const http = require('http');
const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

const PORT = process.env.PORT || 8080;
const frontendDir = path.join(__dirname, '..', 'frontend');

const db = {
  learners: new Map([
    ['learner-1', { learnerId: 'learner-1', segment: 'PAID' }],
    ['learner-2', { learnerId: 'learner-2', segment: 'FREE' }],
    ['guest-1', { learnerId: 'guest-1', segment: 'GUEST' }]
  ]),
  progress: new Map([
    ['learner-1', { courseId: 'course-101', lessonId: 'lesson-5', progressPercent: 65 }],
    ['learner-2', { courseId: 'course-100', lessonId: 'lesson-2', progressPercent: 20 }]
  ]),
  carts: new Map([
    ['cart-1', { cartId: 'cart-1', learnerId: 'learner-1', amount: 1999, currency: 'INR', status: 'OPEN' }]
  ]),
  orders: new Map(),
  entitlements: new Map(),
  idempotency: new Map(),
  eventLog: []
};

const cache = {
  cmsBySegment: new Map(),
  dashboardByLearner: new Map()
};

const CMS_TTL_MS = 5 * 60 * 1000;
const DASHBOARD_TTL_MS = 60 * 1000;

function nowIso() { return new Date().toISOString(); }

function errorResponse(res, statusCode, code, message, details = []) {
  const correlationId = crypto.randomUUID();
  res.writeHead(statusCode, { 'Content-Type': 'application/json', 'x-correlation-id': correlationId });
  res.end(JSON.stringify({ code, message, details, correlationId }));
}

function ok(res, payload, correlationId = crypto.randomUUID()) {
  res.writeHead(200, { 'Content-Type': 'application/json', 'x-correlation-id': correlationId });
  res.end(JSON.stringify(payload));
}

function readBody(req) {
  return new Promise((resolve, reject) => {
    let body = '';
    req.on('data', (chunk) => body += chunk);
    req.on('end', () => {
      if (!body) return resolve({});
      try { resolve(JSON.parse(body)); } catch { reject(new Error('Invalid JSON body')); }
    });
    req.on('error', reject);
  });
}

function getLearner(learnerId) {
  return db.learners.get(learnerId) || { learnerId, segment: 'GUEST' };
}

function getCmsBlocks(segment) {
  const key = `dashboard:${segment}`;
  const hit = cache.cmsBySegment.get(key);
  if (hit && hit.expiresAt > Date.now()) return { blocks: hit.value, cacheHit: true };

  const base = [
    { type: 'HeroBanner.v1', title: 'Keep Learning', subtitle: 'Upgrade your skills daily' },
    { type: 'ContinueCard.v1', title: 'Continue where you left off' }
  ];

  const variants = {
    GUEST: [...base, { type: 'UpsellCard.v1', cta: 'Sign up for free' }],
    FREE: [...base, { type: 'UpsellCard.v1', cta: 'Upgrade to paid for premium paths' }],
    PAID: [...base, { type: 'AchievementCard.v1', message: 'You unlocked advanced labs' }]
  };

  const value = variants[segment] || variants.GUEST;
  cache.cmsBySegment.set(key, { value, expiresAt: Date.now() + CMS_TTL_MS });
  return { blocks: value, cacheHit: false };
}

function getRecommendations(learnerId) {
  if (process.env.RECOMMENDATIONS_DOWN === '1') {
    throw new Error('Recommendations unavailable');
  }

  return [
    { courseId: 'course-200', title: 'System Design Fundamentals', reason: 'Based on your progress' },
    { courseId: 'course-210', title: 'React Performance', reason: 'Popular in your segment' }
  ];
}

function emitEvent(type, payload) {
  db.eventLog.push({ type, payload, occurredAt: nowIso() });
  if (type === 'CoursePurchased' || type === 'LessonCompleted') {
    cache.dashboardByLearner.delete(payload.learnerId);
  }
}

function getDashboard(learnerId) {
  const cached = cache.dashboardByLearner.get(learnerId);
  if (cached && cached.expiresAt > Date.now()) {
    return { ...cached.value, metadata: { ...cached.value.metadata, cacheHit: true } };
  }

  const learner = getLearner(learnerId);
  const cms = getCmsBlocks(learner.segment);
  const continueLearning = db.progress.get(learnerId) || null;

  let recommendations = [];
  let degraded = false;
  try {
    recommendations = getRecommendations(learnerId);
  } catch {
    degraded = true;
  }

  const payload = {
    learnerId,
    segment: learner.segment,
    cmsBlocks: cms.blocks,
    recommendations,
    continueLearning,
    metadata: {
      generatedAt: nowIso(),
      cacheHit: false,
      cmsCacheHit: cms.cacheHit,
      degraded,
      note: degraded ? 'Recommendations temporarily unavailable' : undefined
    }
  };

  cache.dashboardByLearner.set(learnerId, { value: payload, expiresAt: Date.now() + DASHBOARD_TTL_MS });
  return payload;
}

function checkout(body) {
  const { cartId, paymentMethodToken, idempotencyKey } = body;
  if (!cartId || !paymentMethodToken || !idempotencyKey) {
    return { error: [400, 'VALIDATION_ERROR', 'cartId, paymentMethodToken and idempotencyKey are required'] };
  }

  if (db.idempotency.has(idempotencyKey)) {
    return { response: db.idempotency.get(idempotencyKey) };
  }

  const cart = db.carts.get(cartId);
  if (!cart || cart.status !== 'OPEN') {
    return { error: [409, 'CART_CONFLICT', 'Cart is missing or no longer open'] };
  }

  if (paymentMethodToken.startsWith('bad-')) {
    return { error: [502, 'PAYMENT_PROVIDER_ERROR', 'Payment provider failed. Please retry.'] };
  }

  const orderId = `ord-${crypto.randomUUID().slice(0, 8)}`;
  const order = { orderId, learnerId: cart.learnerId, cartId, amount: cart.amount, status: 'PAID', createdAt: nowIso() };
  db.orders.set(orderId, order);
  cart.status = 'CHECKED_OUT';

  const entitlementId = `ent-${crypto.randomUUID().slice(0, 8)}`;
  db.entitlements.set(order.learnerId, { entitlementId, active: true, sourceOrderId: orderId, grantedAt: nowIso() });

  const response = { orderId, paymentStatus: 'CONFIRMED', entitlementStatus: 'UNLOCKED' };
  db.idempotency.set(idempotencyKey, response);

  emitEvent('CoursePurchased', { learnerId: cart.learnerId, orderId, cartId });
  return { response };
}

function handleApi(req, res, parsedUrl) {
  if (req.method === 'GET' && parsedUrl.pathname === '/api/v1/dashboard') {
    const learnerId = parsedUrl.searchParams.get('learnerId');
    if (!learnerId) return errorResponse(res, 400, 'VALIDATION_ERROR', 'learnerId is required');
    return ok(res, getDashboard(learnerId));
  }

  if (req.method === 'POST' && parsedUrl.pathname === '/api/v1/cart/checkout') {
    return readBody(req)
      .then((body) => {
        const result = checkout(body);
        if (result.error) return errorResponse(res, ...result.error);
        return ok(res, result.response);
      })
      .catch((e) => errorResponse(res, 400, 'VALIDATION_ERROR', e.message));
  }

  if (req.method === 'POST' && parsedUrl.pathname === '/api/v1/events/lesson-completed') {
    return readBody(req)
      .then((body) => {
        if (!body.learnerId) return errorResponse(res, 400, 'VALIDATION_ERROR', 'learnerId is required');
        db.progress.set(body.learnerId, {
          courseId: body.courseId || 'course-unknown',
          lessonId: body.lessonId || 'lesson-unknown',
          progressPercent: body.progressPercent ?? 0
        });
        emitEvent('LessonCompleted', body);
        return ok(res, { status: 'accepted' });
      })
      .catch((e) => errorResponse(res, 400, 'VALIDATION_ERROR', e.message));
  }

  if (req.method === 'POST' && parsedUrl.pathname === '/api/v1/events/search-performed') {
    return readBody(req)
      .then((body) => {
        emitEvent('SearchPerformed', body);
        return ok(res, { status: 'recorded' });
      })
      .catch((e) => errorResponse(res, 400, 'VALIDATION_ERROR', e.message));
  }

  if (req.method === 'GET' && parsedUrl.pathname === '/api/v1/observability/health') {
    return ok(res, {
      service: 'edtech-working-model',
      uptimeSeconds: process.uptime(),
      sli: {
        availabilityTarget: '99.9%',
        dashboardP95CachedTargetMs: 300,
        dashboardP95UncachedTargetMs: 800
      },
      eventLogSize: db.eventLog.length
    });
  }

  return false;
}

function serveStatic(req, res, pathname) {
  let filePath = pathname === '/' ? '/index.html' : pathname;
  filePath = path.normalize(filePath).replace(/^\.+/, '');
  const fullPath = path.join(frontendDir, filePath);

  if (!fullPath.startsWith(frontendDir) || !fs.existsSync(fullPath)) {
    res.writeHead(404, { 'Content-Type': 'text/plain' });
    res.end('Not found');
    return;
  }

  const ext = path.extname(fullPath);
  const type = ext === '.html' ? 'text/html' : ext === '.css' ? 'text/css' : 'application/javascript';
  res.writeHead(200, { 'Content-Type': type });
  res.end(fs.readFileSync(fullPath));
}

http.createServer((req, res) => {
  const parsedUrl = new URL(req.url, `http://${req.headers.host}`);
  const correlationId = req.headers['x-correlation-id'] || crypto.randomUUID();
  res.setHeader('x-correlation-id', correlationId);
  res.setHeader('x-content-type-options', 'nosniff');

  if (parsedUrl.pathname.startsWith('/api/')) {
    const handled = handleApi(req, res, parsedUrl);
    if (handled === false) errorResponse(res, 404, 'NOT_FOUND', 'Endpoint not found');
    return;
  }

  serveStatic(req, res, parsedUrl.pathname);
}).listen(PORT, () => {
  console.log(`EdTech working model running at http://localhost:${PORT}`);
});

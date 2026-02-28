const apiBase = '/api/v1';

const el = {
  learnerId: document.getElementById('learnerId'),
  loadDashboardBtn: document.getElementById('loadDashboardBtn'),
  dashboardMeta: document.getElementById('dashboardMeta'),
  cmsBlocks: document.getElementById('cmsBlocks'),
  continueLearning: document.getElementById('continueLearning'),
  recommendations: document.getElementById('recommendations'),
  cartId: document.getElementById('cartId'),
  paymentMethodToken: document.getElementById('paymentMethodToken'),
  idempotencyKey: document.getElementById('idempotencyKey'),
  checkoutBtn: document.getElementById('checkoutBtn'),
  checkoutResult: document.getElementById('checkoutResult')
};

const blockRegistry = {
  'HeroBanner.v1': (block) => `<div class="block"><h4>${block.title}</h4><p>${block.subtitle}</p></div>`,
  'ContinueCard.v1': (block) => `<div class="block"><strong>${block.title}</strong></div>`,
  'UpsellCard.v1': (block) => `<div class="block"><em>${block.cta}</em></div>`,
  'AchievementCard.v1': (block) => `<div class="block">🏆 ${block.message}</div>`
};

function safeRenderBlock(block) {
  const renderer = blockRegistry[block.type];
  if (!renderer) return `<div class="block">Unsupported block: ${block.type}</div>`;
  return renderer(block);
}

async function fetchJson(url, options) {
  const response = await fetch(url, {
    headers: { 'Content-Type': 'application/json' },
    ...options
  });
  const data = await response.json();
  if (!response.ok) throw data;
  return data;
}

async function loadDashboard() {
  const learnerId = el.learnerId.value.trim();
  const data = await fetchJson(`${apiBase}/dashboard?learnerId=${encodeURIComponent(learnerId)}`);
  el.dashboardMeta.textContent = `segment=${data.segment}, cacheHit=${data.metadata.cacheHit}, degraded=${data.metadata.degraded}`;
  el.cmsBlocks.innerHTML = data.cmsBlocks.map(safeRenderBlock).join('');
  el.continueLearning.textContent = JSON.stringify(data.continueLearning, null, 2);
  el.recommendations.textContent = JSON.stringify(data.recommendations, null, 2);
}

async function checkout() {
  const body = {
    cartId: el.cartId.value.trim(),
    paymentMethodToken: el.paymentMethodToken.value.trim(),
    idempotencyKey: el.idempotencyKey.value.trim() || `idem-${Date.now()}`
  };
  const data = await fetchJson(`${apiBase}/cart/checkout`, {
    method: 'POST',
    body: JSON.stringify(body)
  });
  el.checkoutResult.textContent = JSON.stringify(data, null, 2);
}

el.loadDashboardBtn.addEventListener('click', () => loadDashboard().catch((e) => {
  el.dashboardMeta.textContent = `${e.code || 'ERROR'}: ${e.message || 'Failed to load dashboard'}`;
}));

el.checkoutBtn.addEventListener('click', () => checkout().catch((e) => {
  el.checkoutResult.textContent = JSON.stringify(e, null, 2);
}));

loadDashboard().catch(() => {});

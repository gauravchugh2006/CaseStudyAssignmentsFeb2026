# Question 2 - Working Model Code

This is a runnable prototype implementing key parts of the EdTech system design:
- BFF-style dashboard composition API
- Checkout API with idempotency and entitlement unlock
- CMS block personalization by segment (Guest/Free/Paid)
- Caching by segment and learner
- Event emission/invalidation hooks (`CoursePurchased`, `LessonCompleted`, `SearchPerformed`)
- Consistent error model with correlation ID

## Folder structure
- `backend/server.js` - Node HTTP server + APIs + in-memory stores + cache/eventing
- `frontend/` - UI demonstrating component registry and checkout flow

## Run locally (Windows VS Code)
1. Install Node.js 20+.
2. Open terminal in `Question2-EdTech-WorkingModel`.
3. Run:
   ```bash
   node backend/server.js
   ```
4. Open `http://localhost:8080`.

## API contracts
- `GET /api/v1/dashboard?learnerId=learner-1`
- `POST /api/v1/cart/checkout`
  ```json
  {
    "cartId": "cart-1",
    "paymentMethodToken": "tok-demo-123",
    "idempotencyKey": "idem-001"
  }
  ```

## Notes
- Set `RECOMMENDATIONS_DOWN=1` to test graceful dashboard degradation.
- Send `paymentMethodToken` as `bad-*` to simulate payment provider failure (502).

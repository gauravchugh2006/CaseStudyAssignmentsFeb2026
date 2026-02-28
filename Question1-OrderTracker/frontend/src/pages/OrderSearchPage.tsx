import { FormEvent, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../api/client';
import type { OrderSummary } from '../types/order';

export function OrderSearchPage() {
  const [query, setQuery] = useState('');
  const [orders, setOrders] = useState<OrderSummary[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  async function onSearch(event: FormEvent) {
    event.preventDefault();
    setLoading(true);
    setError('');
    try {
      setOrders(await api.searchOrders(query));
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setLoading(false);
    }
  }

  return (
    <section>
      <form onSubmit={onSearch} className="row">
        <input placeholder="Search by orderId, email or mobile" value={query} onChange={(e) => setQuery(e.target.value)} />
        <button disabled={loading}>Search</button>
      </form>
      {error && <p className="error">{error}</p>}
      <ul>
        {orders.map((order) => (
          <li key={order.orderId}>
            <Link to={`/orders/${order.orderId}`}>{order.orderId}</Link> - {order.email} - {order.status} - ₹{order.total}
          </li>
        ))}
      </ul>
    </section>
  );
}

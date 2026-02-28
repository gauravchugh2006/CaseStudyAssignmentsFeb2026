import { FormEvent, useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { api } from '../api/client';
import type { OrderDetails } from '../types/order';

export function OrderDetailsPage() {
  const { orderId = '' } = useParams();
  const [order, setOrder] = useState<OrderDetails | null>(null);
  const [note, setNote] = useState('');
  const [status, setStatus] = useState('PAID');
  const [error, setError] = useState('');

  async function load() {
    try {
      setOrder(await api.getOrder(orderId));
    } catch (err) {
      setError((err as Error).message);
    }
  }

  useEffect(() => {
    load();
  }, [orderId]);

  async function addNote(event: FormEvent) {
    event.preventDefault();
    if (note.length > 500) {
      setError('Note must be at most 500 chars');
      return;
    }
    await api.addNote(orderId, 'support-agent', note);
    setNote('');
    await load();
  }

  async function updateStatus(event: FormEvent) {
    event.preventDefault();
    await api.updateStatus(orderId, status);
    await load();
  }

  if (!order) return <p>{error || 'Loading...'}</p>;

  return (
    <section>
      <h2>{order.orderId}</h2>
      <p>{order.customerName} | {order.customerEmail} | {order.customerMobile}</p>
      <p>Status: <b>{order.currentStatus}</b></p>
      <h3>Timeline</h3>
      <ul>{order.timeline.map((t, idx) => <li key={idx}>{t.fromStatus} → {t.toStatus} by {t.changedBy}</li>)}</ul>
      <h3>Notes</h3>
      <ul>{order.notes.map((n, idx) => <li key={idx}>{n.author}: {n.text}</li>)}</ul>
      <form onSubmit={addNote} className="column">
        <textarea value={note} onChange={(e) => setNote(e.target.value)} maxLength={500} />
        <small>{note.length}/500</small>
        <button>Add note</button>
      </form>
      <form onSubmit={updateStatus} className="row">
        <select value={status} onChange={(e) => setStatus(e.target.value)}>
          <option>PAID</option>
          <option>SHIPPED</option>
          <option>DELIVERED</option>
          <option>CANCELLED</option>
        </select>
        <button>Update status</button>
      </form>
      {error && <p className="error">{error}</p>}
    </section>
  );
}

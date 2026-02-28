import type { ApiError, OrderDetails, OrderSummary } from '../types/order';

const baseUrl = import.meta.env.VITE_API_URL ?? 'http://localhost:5000';

async function request<T>(url: string, options?: RequestInit): Promise<T> {
  const headers = new Headers(options?.headers);
  if (options?.body && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json');
  }

  const response = await fetch(`${baseUrl}${url}`, {
    headers,
    ...options
  });

  if (!response.ok) {
    const err = (await response.json()) as ApiError;
    throw new Error(err.message ?? 'Request failed');
  }

  return (await response.json()) as T;
}

export const api = {
  searchOrders: (query: string) => request<OrderSummary[]>(`/api/orders?query=${encodeURIComponent(query)}`),
  getOrder: (orderId: string) => request<OrderDetails>(`/api/orders/${orderId}`),
  addNote: (orderId: string, author: string, text: string) =>
    request(`/api/orders/${orderId}/notes`, { method: 'POST', body: JSON.stringify({ author, text }) }),
  updateStatus: (orderId: string, newStatus: string) =>
    request(`/api/orders/${orderId}/status`, { method: 'POST', body: JSON.stringify({ newStatus, changedBy: 'support-ui' }) })
};

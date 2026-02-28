export type OrderSummary = {
  orderId: string;
  email: string;
  mobile: string;
  status: string;
  total: number;
};

export type OrderDetails = {
  orderId: string;
  customerName: string;
  customerEmail: string;
  customerMobile: string;
  currentStatus: string;
  subTotal: number;
  tax: number;
  shipping: number;
  grandTotal: number;
  items: { name: string; quantity: number; price: number }[];
  timeline: { fromStatus: string; toStatus: string; changedBy: string; changedAt: string }[];
  notes: { author: string; text: string; createdAt: string }[];
};

export type ApiError = { code: string; message: string; correlationId: string };

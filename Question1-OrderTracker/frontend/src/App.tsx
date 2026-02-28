import { Link, Route, Routes } from 'react-router-dom';
import { OrderDetailsPage } from './pages/OrderDetailsPage';
import { OrderSearchPage } from './pages/OrderSearchPage';

export function App() {
  return (
    <div className="container">
      <header>
        <h1>Order Status Tracker</h1>
        <Link to="/">Search Orders</Link>
      </header>
      <Routes>
        <Route path="/" element={<OrderSearchPage />} />
        <Route path="/orders/:orderId" element={<OrderDetailsPage />} />
      </Routes>
    </div>
  );
}

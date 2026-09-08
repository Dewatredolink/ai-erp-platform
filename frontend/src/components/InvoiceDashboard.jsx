import { useEffect, useState } from 'react';
import { getInvoiceStats } from '../services/invoiceService';
import './InvoiceDashboard.css';

function formatCurrency(value) {
  return Number(value ?? 0).toFixed(2);
}

function InvoiceDashboard() {
  const [stats, setStats] = useState(null);
  const [error, setError] = useState('');

  useEffect(() => {
    getInvoiceStats()
      .then(setStats)
      .catch(() => setError('Failed to load invoice statistics.'));
  }, []);

  if (error) {
    return <p className="form-error">{error}</p>;
  }

  if (!stats) {
    return <p>Loading statistics...</p>;
  }

  return (
    <div className="invoice-dashboard">
      <div className="invoice-dashboard__card">
        <span className="invoice-dashboard__label">Total Revenue</span>
        <span className="invoice-dashboard__value">{formatCurrency(stats.totalRevenue)}</span>
      </div>
      <div className="invoice-dashboard__card">
        <span className="invoice-dashboard__label">Pending Invoices</span>
        <span className="invoice-dashboard__value">{stats.pendingCount}</span>
      </div>
      <div className="invoice-dashboard__card invoice-dashboard__card--danger">
        <span className="invoice-dashboard__label">Overdue Invoices</span>
        <span className="invoice-dashboard__value">{stats.overdueCount}</span>
      </div>
      <div className="invoice-dashboard__card">
        <span className="invoice-dashboard__label">Average Invoice Value</span>
        <span className="invoice-dashboard__value">{formatCurrency(stats.averageInvoiceValue)}</span>
      </div>
    </div>
  );
}

export default InvoiceDashboard;

import { useCallback, useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import InvoiceDashboard from '../components/InvoiceDashboard';
import { deleteInvoice, downloadPDF, getInvoices } from '../services/invoiceService';
import './InvoiceList.css';

const STATUS_OPTIONS = ['Draft', 'Sent', 'Paid', 'Overdue'];

function StatusBadge({ status }) {
  return <span className={`status-badge status-badge--${status.toLowerCase()}`}>{status}</span>;
}

function InvoiceListPage() {
  const navigate = useNavigate();
  const [invoices, setInvoices] = useState([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState('');
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');

  const loadInvoices = useCallback(async () => {
    setIsLoading(true);
    setError('');
    try {
      const params = { page, pageSize };
      if (search.trim()) params.search = search.trim();
      if (status) params.status = status;
      if (fromDate) params.fromDate = fromDate;
      if (toDate) params.toDate = toDate;

      const data = await getInvoices(params);
      setInvoices(data.items);
      setTotalCount(data.totalCount);
    } catch (err) {
      setError('Failed to load invoices. Please make sure the backend API is running.');
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  }, [page, pageSize, search, status, fromDate, toDate]);

  useEffect(() => {
    loadInvoices();
  }, [loadInvoices]);

  async function handleDeleteClick(invoice) {
    const confirmed = window.confirm(`Delete invoice "${invoice.invoiceNumber}"?`);
    if (!confirmed) return;
    try {
      await deleteInvoice(invoice.invoiceId);
      await loadInvoices();
    } catch (err) {
      setError('Failed to delete invoice.');
      console.error(err);
    }
  }

  async function handleDownloadClick(invoice) {
    try {
      await downloadPDF(invoice.invoiceId, invoice.invoiceNumber);
    } catch (err) {
      setError('Failed to download invoice PDF.');
      console.error(err);
    }
  }

  function handleSearchSubmit(event) {
    event.preventDefault();
    setPage(1);
    loadInvoices();
  }

  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

  return (
    <div className="invoice-list-page">
      <div className="invoice-list-page__toolbar">
        <h1>Invoices</h1>
        <button type="button" className="btn btn--primary" onClick={() => navigate('/invoices/new')}>
          Create Invoice
        </button>
      </div>

      <InvoiceDashboard />

      <form className="invoice-list-page__filters" onSubmit={handleSearchSubmit}>
        <input
          type="text"
          placeholder="Search by invoice number"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
        <select value={status} onChange={(e) => setStatus(e.target.value)}>
          <option value="">All Statuses</option>
          {STATUS_OPTIONS.map((s) => (
            <option key={s} value={s}>
              {s}
            </option>
          ))}
        </select>
        <label>
          From
          <input type="date" value={fromDate} onChange={(e) => setFromDate(e.target.value)} />
        </label>
        <label>
          To
          <input type="date" value={toDate} onChange={(e) => setToDate(e.target.value)} />
        </label>
        <button type="submit" className="btn btn--secondary">
          Apply Filters
        </button>
      </form>

      {error && <p className="form-error">{error}</p>}

      {isLoading ? (
        <p>Loading invoices...</p>
      ) : (
        <table className="invoice-list-table">
          <thead>
            <tr>
              <th>Invoice Number</th>
              <th>Company</th>
              <th>Date</th>
              <th>Amount</th>
              <th>Status</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {invoices.length === 0 ? (
              <tr>
                <td colSpan={6}>No invoices found.</td>
              </tr>
            ) : (
              invoices.map((invoice) => (
                <tr key={invoice.invoiceId}>
                  <td>
                    <Link to={`/invoices/${invoice.invoiceId}`}>{invoice.invoiceNumber}</Link>
                  </td>
                  <td>{invoice.company?.companyName ?? '—'}</td>
                  <td>{invoice.invoiceDate?.slice(0, 10)}</td>
                  <td>{Number(invoice.grandTotal).toFixed(2)}</td>
                  <td>
                    <StatusBadge status={invoice.status} />
                  </td>
                  <td>
                    <button
                      type="button"
                      className="btn btn--secondary"
                      onClick={() => navigate(`/invoices/${invoice.invoiceId}/edit`)}
                    >
                      Edit
                    </button>
                    <button
                      type="button"
                      className="btn btn--secondary"
                      onClick={() => handleDownloadClick(invoice)}
                    >
                      PDF
                    </button>
                    <button
                      type="button"
                      className="btn btn--danger"
                      onClick={() => handleDeleteClick(invoice)}
                    >
                      Delete
                    </button>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      )}

      <div className="invoice-list-page__pagination">
        <button
          type="button"
          className="btn btn--secondary"
          onClick={() => setPage((p) => Math.max(1, p - 1))}
          disabled={page <= 1}
        >
          Previous
        </button>
        <span>
          Page {page} of {totalPages}
        </span>
        <button
          type="button"
          className="btn btn--secondary"
          onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
          disabled={page >= totalPages}
        >
          Next
        </button>
      </div>
    </div>
  );
}

export default InvoiceListPage;

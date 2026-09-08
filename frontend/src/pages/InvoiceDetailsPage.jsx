import { useCallback, useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { deleteInvoice, downloadPDF, getInvoiceById } from '../services/invoiceService';
import { openInvoicePrintWindow } from '../services/printService';
import './InvoiceDetails.css';

function InvoiceDetailsPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [invoice, setInvoice] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');

  const loadInvoice = useCallback(async () => {
    setIsLoading(true);
    setError('');
    try {
      const data = await getInvoiceById(id);
      setInvoice(data);
    } catch (err) {
      setError('Failed to load invoice.');
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  }, [id]);

  useEffect(() => {
    loadInvoice();
  }, [loadInvoice]);

  async function handleDelete() {
    if (!invoice) return;
    const confirmed = window.confirm(`Delete invoice "${invoice.invoiceNumber}"?`);
    if (!confirmed) return;
    try {
      await deleteInvoice(invoice.invoiceId);
      navigate('/invoices');
    } catch (err) {
      setError('Failed to delete invoice.');
      console.error(err);
    }
  }

  async function handleDownload() {
    if (!invoice) return;
    try {
      await downloadPDF(invoice.invoiceId, invoice.invoiceNumber);
    } catch (err) {
      setError('Failed to download invoice PDF.');
      console.error(err);
    }
  }

  function handlePrint() {
    if (!invoice) return;
    try {
      openInvoicePrintWindow(invoice, invoice.company);
    } catch (err) {
      setError('Failed to open print dialog.');
      console.error(err);
    }
  }

  if (isLoading) {
    return <p>Loading invoice...</p>;
  }

  if (error || !invoice) {
    return <p className="form-error">{error || 'Invoice not found.'}</p>;
  }

  return (
    <div className="invoice-details-page">
      <div className="invoice-details-page__toolbar">
        <h1>Invoice {invoice.invoiceNumber}</h1>
        <div className="invoice-details-page__actions">
          <button type="button" className="btn btn--secondary" onClick={handlePrint}>
            Print
          </button>
          <button type="button" className="btn btn--secondary" onClick={handleDownload}>
            Download PDF
          </button>
          <button
            type="button"
            className="btn btn--secondary"
            onClick={() => navigate(`/invoices/${invoice.invoiceId}/edit`)}
          >
            Edit
          </button>
          <button type="button" className="btn btn--danger" onClick={handleDelete}>
            Delete
          </button>
        </div>
      </div>

      <div className="invoice-details-card">
        <div className="invoice-details-grid">
          <div>
            <span className="invoice-details-label">Company</span>
            <span>{invoice.company?.companyName ?? '—'}</span>
          </div>
          <div>
            <span className="invoice-details-label">Status</span>
            <span>{invoice.status}</span>
          </div>
          <div>
            <span className="invoice-details-label">Invoice Date</span>
            <span>{invoice.invoiceDate?.slice(0, 10)}</span>
          </div>
          <div>
            <span className="invoice-details-label">Due Date</span>
            <span>{invoice.dueDate?.slice(0, 10)}</span>
          </div>
        </div>

        <table className="invoice-details-items">
          <thead>
            <tr>
              <th>Description</th>
              <th>Quantity</th>
              <th>Unit Price</th>
              <th>Tax Amount</th>
              <th>Amount</th>
            </tr>
          </thead>
          <tbody>
            {invoice.items?.map((item) => (
              <tr key={item.invoiceItemId}>
                <td>{item.description}</td>
                <td>{item.quantity}</td>
                <td>{Number(item.unitPrice).toFixed(2)}</td>
                <td>{Number(item.taxAmount).toFixed(2)}</td>
                <td>{Number(item.amount).toFixed(2)}</td>
              </tr>
            ))}
          </tbody>
        </table>

        <div className="invoice-details-totals">
          <div>
            <span>Subtotal</span>
            <span>{Number(invoice.totalAmount).toFixed(2)}</span>
          </div>
          <div>
            <span>Tax</span>
            <span>{Number(invoice.taxAmount).toFixed(2)}</span>
          </div>
          <div className="invoice-details-totals-grand">
            <span>Grand Total</span>
            <span>{Number(invoice.grandTotal).toFixed(2)}</span>
          </div>
        </div>

        {invoice.notes && (
          <div className="invoice-details-notes">
            <span className="invoice-details-label">Notes</span>
            <p>{invoice.notes}</p>
          </div>
        )}
      </div>
    </div>
  );
}

export default InvoiceDetailsPage;

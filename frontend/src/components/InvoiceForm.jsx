import { useEffect, useState } from 'react';
import { getCompanies } from '../services/companyService';
import './InvoiceForm.css';

const STATUS_OPTIONS = ['Draft', 'Sent', 'Paid', 'Overdue'];

function emptyItem() {
  return { description: '', quantity: 1, unitPrice: 0, taxRate: 0 };
}

function toDateInputValue(value) {
  if (!value) return '';
  return value.slice(0, 10);
}

function calculateItemAmounts(item) {
  const quantity = Number(item.quantity) || 0;
  const unitPrice = Number(item.unitPrice) || 0;
  const taxRate = Number(item.taxRate) || 0;
  const amount = quantity * unitPrice;
  const taxAmount = (amount * taxRate) / 100;
  return { amount, taxAmount };
}

function InvoiceForm({ invoice, onSubmit, onCancel }) {
  const [companies, setCompanies] = useState([]);
  const [formData, setFormData] = useState({
    invoiceNumber: invoice?.invoiceNumber ?? '',
    companyId: invoice?.companyId ?? '',
    invoiceDate: toDateInputValue(invoice?.invoiceDate) || toDateInputValue(new Date().toISOString()),
    dueDate: toDateInputValue(invoice?.dueDate),
    status: invoice?.status ?? 'Draft',
    notes: invoice?.notes ?? '',
  });
  const [items, setItems] = useState(
    invoice?.items?.length
      ? invoice.items.map((item) => ({
          description: item.description,
          quantity: item.quantity,
          unitPrice: item.unitPrice,
          taxRate: item.taxRate ?? 0,
        }))
      : [emptyItem()]
  );
  const [error, setError] = useState('');

  useEffect(() => {
    getCompanies()
      .then(setCompanies)
      .catch(() => setCompanies([]));
  }, []);

  function handleFieldChange(event) {
    const { name, value } = event.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
  }

  function handleItemChange(index, field, value) {
    setItems((prev) =>
      prev.map((item, i) => (i === index ? { ...item, [field]: value } : item))
    );
  }

  function handleAddItem() {
    setItems((prev) => [...prev, emptyItem()]);
  }

  function handleRemoveItem(index) {
    setItems((prev) => (prev.length > 1 ? prev.filter((_, i) => i !== index) : prev));
  }

  const itemTotals = items.map(calculateItemAmounts);
  const subtotal = itemTotals.reduce((sum, t) => sum + t.amount, 0);
  const totalTax = itemTotals.reduce((sum, t) => sum + t.taxAmount, 0);
  const grandTotal = subtotal + totalTax;

  function handleSubmit(event) {
    event.preventDefault();

    if (!formData.invoiceNumber.trim()) {
      setError('Invoice number is required.');
      return;
    }
    if (!formData.companyId) {
      setError('Company is required.');
      return;
    }
    if (!formData.invoiceDate || !formData.dueDate) {
      setError('Invoice date and due date are required.');
      return;
    }
    if (new Date(formData.dueDate) < new Date(formData.invoiceDate)) {
      setError('Due date must be on or after the invoice date.');
      return;
    }
    if (items.some((item) => !item.description.trim() || Number(item.quantity) <= 0)) {
      setError('Each item needs a description and a quantity greater than zero.');
      return;
    }

    setError('');
    onSubmit({
      invoiceNumber: formData.invoiceNumber.trim(),
      companyId: Number(formData.companyId),
      invoiceDate: formData.invoiceDate,
      dueDate: formData.dueDate,
      status: formData.status,
      notes: formData.notes,
      items: items.map((item) => ({
        description: item.description.trim(),
        quantity: Number(item.quantity),
        unitPrice: Number(item.unitPrice),
        taxRate: Number(item.taxRate) || 0,
      })),
    });
  }

  return (
    <div className="invoice-form">
      <h1>{invoice ? 'Edit Invoice' : 'New Invoice'}</h1>
      <form onSubmit={handleSubmit} noValidate>
        <div className="invoice-form__grid">
          <div>
            <label htmlFor="invoiceNumber">Invoice Number</label>
            <input
              id="invoiceNumber"
              type="text"
              name="invoiceNumber"
              value={formData.invoiceNumber}
              onChange={handleFieldChange}
              placeholder="Enter invoice number"
              required
            />
          </div>

          <div>
            <label htmlFor="companyId">Company</label>
            <select
              id="companyId"
              name="companyId"
              value={formData.companyId}
              onChange={handleFieldChange}
              required
            >
              <option value="">Select a company</option>
              {companies.map((company) => (
                <option key={company.companyId} value={company.companyId}>
                  {company.companyName}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label htmlFor="invoiceDate">Invoice Date</label>
            <input
              id="invoiceDate"
              type="date"
              name="invoiceDate"
              value={formData.invoiceDate}
              onChange={handleFieldChange}
              required
            />
          </div>

          <div>
            <label htmlFor="dueDate">Due Date</label>
            <input
              id="dueDate"
              type="date"
              name="dueDate"
              value={formData.dueDate}
              onChange={handleFieldChange}
              required
            />
          </div>

          <div>
            <label htmlFor="status">Status</label>
            <select id="status" name="status" value={formData.status} onChange={handleFieldChange}>
              {STATUS_OPTIONS.map((status) => (
                <option key={status} value={status}>
                  {status}
                </option>
              ))}
            </select>
          </div>
        </div>

        <h2>Items</h2>
        <table className="invoice-items-table">
          <thead>
            <tr>
              <th>Description</th>
              <th>Quantity</th>
              <th>Unit Price</th>
              <th>Tax %</th>
              <th>Tax Amount</th>
              <th>Amount</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {items.map((item, index) => {
              const { amount, taxAmount } = itemTotals[index];
              return (
                <tr key={index}>
                  <td>
                    <input
                      type="text"
                      value={item.description}
                      onChange={(e) => handleItemChange(index, 'description', e.target.value)}
                      placeholder="Item description"
                      required
                    />
                  </td>
                  <td>
                    <input
                      type="number"
                      min="0"
                      step="0.01"
                      value={item.quantity}
                      onChange={(e) => handleItemChange(index, 'quantity', e.target.value)}
                    />
                  </td>
                  <td>
                    <input
                      type="number"
                      min="0"
                      step="0.01"
                      value={item.unitPrice}
                      onChange={(e) => handleItemChange(index, 'unitPrice', e.target.value)}
                    />
                  </td>
                  <td>
                    <input
                      type="number"
                      min="0"
                      step="0.01"
                      value={item.taxRate}
                      onChange={(e) => handleItemChange(index, 'taxRate', e.target.value)}
                    />
                  </td>
                  <td>{taxAmount.toFixed(2)}</td>
                  <td>{amount.toFixed(2)}</td>
                  <td>
                    <button
                      type="button"
                      className="btn btn--danger"
                      onClick={() => handleRemoveItem(index)}
                      disabled={items.length === 1}
                    >
                      Remove
                    </button>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
        <button type="button" className="btn btn--secondary" onClick={handleAddItem}>
          Add Item
        </button>

        <div className="invoice-form__totals">
          <div>
            <span>Subtotal</span>
            <span>{subtotal.toFixed(2)}</span>
          </div>
          <div>
            <span>Tax</span>
            <span>{totalTax.toFixed(2)}</span>
          </div>
          <div className="invoice-form__totals-grand">
            <span>Grand Total</span>
            <span>{grandTotal.toFixed(2)}</span>
          </div>
        </div>

        <label htmlFor="notes">Notes</label>
        <textarea
          id="notes"
          name="notes"
          value={formData.notes}
          onChange={handleFieldChange}
          placeholder="Additional notes"
          rows={3}
        />

        {error && (
          <p className="form-error" role="alert">
            {error}
          </p>
        )}

        <div className="invoice-form__actions">
          <button type="button" className="btn btn--secondary" onClick={onCancel}>
            Cancel
          </button>
          <button type="submit" className="btn btn--primary">
            Save
          </button>
        </div>
      </form>
    </div>
  );
}

export default InvoiceForm;

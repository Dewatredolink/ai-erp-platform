// Print utility service
export function openInvoicePrintWindow(invoice, company) {
  const printWindow = window.open('', '_blank');
  const html = generatePrintHTML(invoice, company);
  printWindow.document.write(html);
  printWindow.document.close();
  
  // Wait for content to load, then print
  printWindow.onload = () => {
    printWindow.print();
  };
}

function generatePrintHTML(invoice, company) {
  const formatDate = (dateStr) => {
    if (!dateStr) return '';
    return new Date(dateStr).toLocaleDateString('en-IN', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
    });
  };

  const formatCurrency = (value) => {
    return Number(value || 0).toLocaleString('en-IN', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    });
  };

  return `
    <!DOCTYPE html>
    <html>
    <head>
      <meta charset="UTF-8">
      <title>Invoice ${invoice.invoiceNumber}</title>
      <style>
        * {
          margin: 0;
          padding: 0;
          box-sizing: border-box;
        }
        
        body {
          font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
          line-height: 1.6;
          color: #333;
          padding: 20px;
        }
        
        .invoice-container {
          max-width: 900px;
          margin: 0 auto;
          background: white;
          padding: 40px;
          border: 1px solid #ddd;
          border-radius: 8px;
        }
        
        .invoice-header {
          display: flex;
          justify-content: space-between;
          align-items: flex-start;
          margin-bottom: 40px;
          border-bottom: 3px solid #007bff;
          padding-bottom: 20px;
        }
        
        .invoice-title h1 {
          font-size: 32px;
          color: #007bff;
          margin-bottom: 5px;
        }
        
        .invoice-number {
          font-size: 14px;
          color: #666;
        }
        
        .company-info {
          text-align: right;
        }
        
        .company-info h3 {
          font-size: 16px;
          margin-bottom: 5px;
        }
        
        .company-info p {
          font-size: 12px;
          color: #666;
          margin: 3px 0;
        }
        
        .invoice-details {
          display: grid;
          grid-template-columns: 1fr 1fr;
          gap: 30px;
          margin-bottom: 30px;
        }
        
        .detail-section h4 {
          font-size: 12px;
          font-weight: 600;
          text-transform: uppercase;
          color: #007bff;
          margin-bottom: 10px;
          border-bottom: 1px solid #eee;
          padding-bottom: 8px;
        }
        
        .detail-section p {
          font-size: 13px;
          margin: 5px 0;
          color: #333;
        }
        
        .detail-label {
          font-weight: 600;
          color: #666;
        }
        
        .detail-value {
          color: #333;
        }
        
        .status-badge {
          display: inline-block;
          padding: 4px 12px;
          border-radius: 20px;
          font-size: 12px;
          font-weight: 600;
          margin-top: 8px;
        }
        
        .status-draft {
          background-color: #e9ecef;
          color: #495057;
        }
        
        .status-sent {
          background-color: #cfe2ff;
          color: #084298;
        }
        
        .status-paid {
          background-color: #d1e7dd;
          color: #0f5132;
        }
        
        .status-overdue {
          background-color: #f8d7da;
          color: #842029;
        }
        
        .items-table {
          width: 100%;
          border-collapse: collapse;
          margin-bottom: 30px;
        }
        
        .items-table thead {
          background-color: #f8f9fa;
          border-bottom: 2px solid #007bff;
        }
        
        .items-table th {
          padding: 12px;
          text-align: left;
          font-size: 12px;
          font-weight: 600;
          text-transform: uppercase;
          color: #007bff;
        }
        
        .items-table td {
          padding: 12px;
          border-bottom: 1px solid #eee;
          font-size: 13px;
        }
        
        .items-table tbody tr:hover {
          background-color: #f9f9f9;
        }
        
        .text-right {
          text-align: right;
        }
        
        .text-center {
          text-align: center;
        }
        
        .totals-section {
          width: 100%;
          margin-bottom: 30px;
        }
        
        .totals-table {
          width: 60%;
          margin-left: auto;
          border-collapse: collapse;
        }
        
        .totals-table tr {
          border-bottom: 1px solid #eee;
        }
        
        .totals-table td {
          padding: 12px;
          font-size: 13px;
        }
        
        .totals-table td:first-child {
          text-align: left;
          font-weight: 500;
          color: #666;
        }
        
        .totals-table td:last-child {
          text-align: right;
          font-weight: 600;
          color: #333;
        }
        
        .totals-table tr.total-row {
          background-color: #f8f9fa;
          border-bottom: 2px solid #007bff;
        }
        
        .totals-table tr.total-row td {
          padding: 14px 12px;
          font-size: 14px;
          color: #007bff;
        }
        
        .notes-section {
          margin-top: 30px;
          padding: 15px;
          background-color: #f8f9fa;
          border-left: 4px solid #007bff;
          border-radius: 4px;
        }
        
        .notes-section h4 {
          font-size: 12px;
          font-weight: 600;
          color: #007bff;
          margin-bottom: 8px;
          text-transform: uppercase;
        }
        
        .notes-section p {
          font-size: 13px;
          color: #333;
          line-height: 1.6;
        }
        
        .footer {
          margin-top: 40px;
          padding-top: 20px;
          border-top: 1px solid #eee;
          text-align: center;
          font-size: 12px;
          color: #999;
        }
        
        @media print {
          body {
            padding: 0;
          }
          .invoice-container {
            border: none;
            border-radius: 0;
            box-shadow: none;
          }
          .footer {
            display: none;
          }
        }
      </style>
    </head>
    <body>
      <div class="invoice-container">
        <!-- Header -->
        <div class="invoice-header">
          <div class="invoice-title">
            <h1>INVOICE</h1>
            <div class="invoice-number">Invoice #${invoice.invoiceNumber}</div>
          </div>
          <div class="company-info">
            <h3>${company?.companyName || 'N/A'}</h3>
            ${company?.address ? `<p>${company.address}</p>` : ''}
            ${company?.city ? `<p>${company.city}${company.state ? ', ' + company.state : ''}</p>` : ''}
            ${company?.pinCode ? `<p>${company.pinCode}</p>` : ''}
            ${company?.phoneNumber ? `<p>Ph: ${company.phoneNumber}</p>` : ''}
            ${company?.email ? `<p>Email: ${company.email}</p>` : ''}
          </div>
        </div>
        
        <!-- Details -->
        <div class="invoice-details">
          <div class="detail-section">
            <h4>Invoice Details</h4>
            <p>
              <span class="detail-label">Invoice Date:</span>
              <span class="detail-value">${formatDate(invoice.invoiceDate)}</span>
            </p>
            <p>
              <span class="detail-label">Due Date:</span>
              <span class="detail-value">${formatDate(invoice.dueDate)}</span>
            </p>
            <p>
              <span class="detail-label">Status:</span>
              <span class="status-badge status-${invoice.status.toLowerCase()}">${invoice.status}</span>
            </p>
          </div>
          <div class="detail-section"></div>
        </div>
        
        <!-- Items Table -->
        <table class="items-table">
          <thead>
            <tr>
              <th>Description</th>
              <th class="text-right">Qty</th>
              <th class="text-right">Unit Price</th>
              <th class="text-right">Tax Rate</th>
              <th class="text-right">Tax Amount</th>
              <th class="text-right">Amount</th>
            </tr>
          </thead>
          <tbody>
            ${invoice.items?.map(item => `
              <tr>
                <td>${item.description}</td>
                <td class="text-right">${formatCurrency(item.quantity)}</td>
                <td class="text-right">₹${formatCurrency(item.unitPrice)}</td>
                <td class="text-right">${item.taxRate ? formatCurrency(item.taxRate) + '%' : '—'}</td>
                <td class="text-right">₹${formatCurrency(item.taxAmount)}</td>
                <td class="text-right"><strong>₹${formatCurrency(item.amount)}</strong></td>
              </tr>
            `).join('') || ''}
          </tbody>
        </table>
        
        <!-- Totals -->
        <div class="totals-section">
          <table class="totals-table">
            <tr>
              <td>Subtotal</td>
              <td>₹${formatCurrency(invoice.totalAmount)}</td>
            </tr>
            <tr>
              <td>Tax</td>
              <td>₹${formatCurrency(invoice.taxAmount)}</td>
            </tr>
            <tr class="total-row">
              <td>Grand Total</td>
              <td>₹${formatCurrency(invoice.grandTotal)}</td>
            </tr>
          </table>
        </div>
        
        <!-- Notes -->
        ${invoice.notes ? `
          <div class="notes-section">
            <h4>Notes</h4>
            <p>${invoice.notes}</p>
          </div>
        ` : ''}
        
        <!-- Footer -->
        <div class="footer">
          <p>This is an electronically generated document. No signature is required.</p>
          <p>Generated on ${new Date().toLocaleDateString('en-IN')} at ${new Date().toLocaleTimeString('en-IN')}</p>
        </div>
      </div>
    </body>
    </html>
  `;
}

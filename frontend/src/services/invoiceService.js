import apiClient from './companyService';

/**
 * Fetch invoices from the backend API with optional pagination and filters.
 * @param {Object} [params] - Query params (page, pageSize, status, search, fromDate, toDate)
 * @returns {Promise<Object>} paginated invoice list: { items, totalCount, page, pageSize }
 */
export async function getInvoices(params = {}) {
  const response = await apiClient.get('/invoices', { params });
  return response.data;
}

/**
 * Fetch a single invoice (with items) by id.
 * @param {string} id
 */
export async function getInvoiceById(id) {
  const response = await apiClient.get(`/invoices/${id}`);
  return response.data;
}

/**
 * Create a new invoice.
 * @param {Object} data - Invoice data including items array
 */
export async function createInvoice(data) {
  const response = await apiClient.post('/invoices', data);
  return response.data;
}

/**
 * Update an existing invoice.
 * @param {string} id
 * @param {Object} data - Updated invoice data including items array
 */
export async function updateInvoice(id, data) {
  const response = await apiClient.put(`/invoices/${id}`, data);
  return response.data;
}

/**
 * Delete an invoice by id.
 * @param {string} id
 */
export async function deleteInvoice(id) {
  const response = await apiClient.delete(`/invoices/${id}`);
  return response.data;
}

/**
 * Download the invoice PDF and trigger a browser download.
 * @param {string} id
 * @param {string} [invoiceNumber] - used to name the downloaded file
 */
export async function downloadPDF(id, invoiceNumber) {
  const response = await apiClient.post(`/invoices/${id}/pdf`, null, {
    responseType: 'blob',
  });

  const url = window.URL.createObjectURL(new Blob([response.data], { type: 'application/pdf' }));
  const link = document.createElement('a');
  link.href = url;
  link.setAttribute('download', `invoice-${invoiceNumber ?? id}.pdf`);
  document.body.appendChild(link);
  link.click();
  link.remove();
  window.URL.revokeObjectURL(url);
}

/**
 * Fetch invoice dashboard statistics.
 * @returns {Promise<Object>} { totalRevenue, pendingCount, overdueCount, averageInvoiceValue, totalInvoices }
 */
export async function getInvoiceStats() {
  const response = await apiClient.get('/invoices/stats');
  return response.data;
}

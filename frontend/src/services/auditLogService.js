import apiClient from './companyService';

export async function getAuditLogs(params = {}) {
  const response = await apiClient.get('/audit-logs', { params });
  return response.data;
}

export async function getAuditLogById(id) {
  const response = await apiClient.get(`/audit-logs/${id}`);
  return response.data;
}

export async function exportAuditLogs(params = {}, format = 'json') {
  const response = await apiClient.get('/audit-logs/export', {
    params: { ...params, format },
    responseType: format === 'csv' ? 'blob' : 'json',
  });

  if (format === 'csv') {
    const url = window.URL.createObjectURL(new Blob([response.data], { type: 'text/csv' }));
    const link = document.createElement('a');
    link.href = url;
    link.setAttribute('download', `audit-logs-${new Date().toISOString().slice(0, 10)}.csv`);
    document.body.appendChild(link);
    link.click();
    link.remove();
    window.URL.revokeObjectURL(url);
    return null;
  }

  return response.data;
}

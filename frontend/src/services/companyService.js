import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5079';

const apiClient = axios.create({
  baseURL: `${API_BASE_URL}/api`,
  headers: {
    'Content-Type': 'application/json',
  },
});

/**
 * Fetch all companies from the backend API.
 * @returns {Promise<Array>} list of companies
 */
export async function getCompanies() {
  const response = await apiClient.get('/companies');
  return response.data;
}

/**
 * Fetch a single company by id.
 * @param {number|string} id
 */
export async function getCompany(id) {
  const response = await apiClient.get(`/companies/${id}`);
  return response.data;
}

/**
 * Create a new company.
 * @param {Object} company - Company data with fields like companyName, address, city, state, etc.
 */
export async function createCompany(company) {
  const response = await apiClient.post('/companies', company);
  return response.data;
}

/**
 * Update an existing company.
 * @param {number|string} id
 * @param {Object} company - Updated company data
 */
export async function updateCompany(id, company) {
  const response = await apiClient.put(`/companies/${id}`, company);
  return response.data;
}

/**
 * Delete a company by id.
 * @param {number|string} id
 */
export async function deleteCompany(id) {
  const response = await apiClient.delete(`/companies/${id}`);
  return response.data;
}

export default apiClient;

import { useCallback, useEffect, useState } from 'react';
import CompanyForm from '../components/CompanyForm';
import {
  createCompany,
  deleteCompany,
  getCompanies,
  updateCompany,
} from '../services/companyService';
import './CompaniesPage.css';

function CompaniesPage() {
  const [companies, setCompanies] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingCompany, setEditingCompany] = useState(null);

  const loadCompanies = useCallback(async () => {
    setIsLoading(true);
    setError('');
    try {
      const data = await getCompanies();
      setCompanies(data);
    } catch (err) {
      setError('Failed to load companies. Please make sure the backend API is running.');
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    loadCompanies();
  }, [loadCompanies]);

  function handleAddClick() {
    setEditingCompany(null);
    setIsFormOpen(true);
  }

  function handleEditClick(company) {
    setEditingCompany(company);
    setIsFormOpen(true);
  }

  async function handleDeleteClick(company) {
    const confirmed = window.confirm(`Delete company "${company.name}"?`);
    if (!confirmed) {
      return;
    }
    try {
      await deleteCompany(company.id);
      await loadCompanies();
    } catch (err) {
      setError('Failed to delete company.');
      console.error(err);
    }
  }

  async function handleFormSubmit(companyData) {
    try {
      if (editingCompany) {
        await updateCompany(editingCompany.id, companyData);
      } else {
        await createCompany(companyData);
      }
      setIsFormOpen(false);
      setEditingCompany(null);
      await loadCompanies();
    } catch (err) {
      setError('Failed to save company.');
      console.error(err);
    }
  }

  function handleFormCancel() {
    setIsFormOpen(false);
    setEditingCompany(null);
  }

  return (
    <div className="companies-page">
      <div className="companies-page__toolbar">
        <h1>Companies</h1>
        <button type="button" className="btn btn--primary" onClick={handleAddClick}>
          Add Company
        </button>
      </div>

      {error && <p className="form-error">{error}</p>}

      {isLoading ? (
        <p>Loading companies...</p>
      ) : (
        <table className="companies-table">
          <thead>
            <tr>
              <th>Company ID</th>
              <th>Name</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {companies.length === 0 ? (
              <tr>
                <td colSpan={3}>No companies found.</td>
              </tr>
            ) : (
              companies.map((company) => (
                <tr key={company.id}>
                  <td>{company.id}</td>
                  <td>{company.name}</td>
                  <td>
                    <button
                      type="button"
                      className="btn btn--secondary"
                      onClick={() => handleEditClick(company)}
                    >
                      Edit
                    </button>
                    <button
                      type="button"
                      className="btn btn--danger"
                      onClick={() => handleDeleteClick(company)}
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

      {isFormOpen && (
        <CompanyForm
          key={editingCompany?.id ?? 'new'}
          company={editingCompany}
          onSubmit={handleFormSubmit}
          onCancel={handleFormCancel}
        />
      )}
    </div>
  );
}

export default CompaniesPage;

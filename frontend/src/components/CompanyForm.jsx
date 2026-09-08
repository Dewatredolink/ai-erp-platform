import { useState } from 'react';
import './CompanyForm.css';

function CompanyForm({ company, onSubmit, onCancel }) {
  const [name, setName] = useState(company?.name ?? '');
  const [error, setError] = useState('');

  function handleSubmit(event) {
    event.preventDefault();

    const trimmedName = name.trim();
    if (!trimmedName) {
      setError('Company name is required.');
      return;
    }
    if (trimmedName.length > 200) {
      setError('Company name must be 200 characters or fewer.');
      return;
    }

    setError('');
    onSubmit({ name: trimmedName });
  }

  return (
    <div className="modal-overlay" role="presentation" onClick={onCancel}>
      <div
        className="modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby="company-form-title"
        onClick={(event) => event.stopPropagation()}
      >
        <h2 id="company-form-title">{company ? 'Edit Company' : 'Add Company'}</h2>
        <form onSubmit={handleSubmit} noValidate>
          <label htmlFor="company-name">Company Name</label>
          <input
            id="company-name"
            type="text"
            value={name}
            onChange={(event) => setName(event.target.value)}
            placeholder="Enter company name"
            autoFocus
          />
          {error && (
            <p className="form-error" role="alert">
              {error}
            </p>
          )}
          <div className="modal-actions">
            <button type="button" className="btn btn--secondary" onClick={onCancel}>
              Cancel
            </button>
            <button type="submit" className="btn btn--primary">
              Save
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

export default CompanyForm;

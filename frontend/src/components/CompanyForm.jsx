import { useState } from 'react';
import './CompanyForm.css';

function CompanyForm({ company, onSubmit, onCancel }) {
  const [formData, setFormData] = useState({
    companyName: company?.companyName ?? '',
    address: company?.address ?? '',
    city: company?.city ?? '',
    state: company?.state ?? '',
    pinCode: company?.pinCode ?? '',
    email: company?.email ?? '',
    phoneNumber: company?.phoneNumber ?? '',
    website: company?.website ?? '',
    gstin: company?.gstin ?? '',
    pan: company?.pan ?? '',
    drugLicenceNumber: company?.drugLicenceNumber ?? '',
    udogAadhaar: company?.udogAadhaar ?? '',
    aadhaarNumber: company?.aadhaarNumber ?? '',
    msmeNumber: company?.msmeNumber ?? '',
    fssaiNumber: company?.fssaiNumber ?? '',
    createdBy: company?.createdBy ?? 'Admin',
    updatedBy: company?.updatedBy ?? 'Admin',
  });
  const [error, setError] = useState('');

  function handleChange(event) {
    const { name, value } = event.target;
    setFormData((prev) => ({
      ...prev,
      [name]: value,
    }));
  }

  function handleSubmit(event) {
    event.preventDefault();

    const trimmedName = formData.companyName.trim();
    if (!trimmedName) {
      setError('Company name is required.');
      return;
    }
    if (trimmedName.length > 200) {
      setError('Company name must be 200 characters or fewer.');
      return;
    }

    setError('');
    onSubmit({
      ...formData,
      companyName: trimmedName,
    });
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
          <label htmlFor="company-name">
            Company Name <span style={{ color: 'red' }}>*</span>
          </label>
          <input
            id="company-name"
            type="text"
            name="companyName"
            value={formData.companyName}
            onChange={handleChange}
            placeholder="Enter company name"
            autoFocus
            required
          />

          <label htmlFor="address">Address</label>
          <input
            id="address"
            type="text"
            name="address"
            value={formData.address}
            onChange={handleChange}
            placeholder="Enter address"
          />

          <label htmlFor="city">City</label>
          <input
            id="city"
            type="text"
            name="city"
            value={formData.city}
            onChange={handleChange}
            placeholder="Enter city"
          />

          <label htmlFor="state">State</label>
          <input
            id="state"
            type="text"
            name="state"
            value={formData.state}
            onChange={handleChange}
            placeholder="Enter state"
          />

          <label htmlFor="pinCode">Pin Code</label>
          <input
            id="pinCode"
            type="text"
            name="pinCode"
            value={formData.pinCode}
            onChange={handleChange}
            placeholder="Enter pin code"
          />

          <label htmlFor="email">Email</label>
          <input
            id="email"
            type="email"
            name="email"
            value={formData.email}
            onChange={handleChange}
            placeholder="Enter email"
          />

          <label htmlFor="phoneNumber">Phone Number</label>
          <input
            id="phoneNumber"
            type="tel"
            name="phoneNumber"
            value={formData.phoneNumber}
            onChange={handleChange}
            placeholder="Enter phone number"
          />

          <label htmlFor="website">Website</label>
          <input
            id="website"
            type="url"
            name="website"
            value={formData.website}
            onChange={handleChange}
            placeholder="Enter website URL"
          />

          <label htmlFor="gstin">GSTIN</label>
          <input
            id="gstin"
            type="text"
            name="gstin"
            value={formData.gstin}
            onChange={handleChange}
            placeholder="Enter GSTIN"
          />

          <label htmlFor="pan">PAN</label>
          <input
            id="pan"
            type="text"
            name="pan"
            value={formData.pan}
            onChange={handleChange}
            placeholder="Enter PAN"
          />

          <label htmlFor="drugLicenceNumber">Drug Licence Number</label>
          <input
            id="drugLicenceNumber"
            type="text"
            name="drugLicenceNumber"
            value={formData.drugLicenceNumber}
            onChange={handleChange}
            placeholder="Enter Drug Licence Number"
          />

          <label htmlFor="udogAadhaar">UDOG Aadhaar</label>
          <input
            id="udogAadhaar"
            type="text"
            name="udogAadhaar"
            value={formData.udogAadhaar}
            onChange={handleChange}
            placeholder="Enter UDOG Aadhaar"
          />

          <label htmlFor="aadhaarNumber">Aadhaar Number</label>
          <input
            id="aadhaarNumber"
            type="text"
            name="aadhaarNumber"
            value={formData.aadhaarNumber}
            onChange={handleChange}
            placeholder="Enter Aadhaar Number"
          />

          <label htmlFor="msmeNumber">MSME Number</label>
          <input
            id="msmeNumber"
            type="text"
            name="msmeNumber"
            value={formData.msmeNumber}
            onChange={handleChange}
            placeholder="Enter MSME Number"
          />

          <label htmlFor="fssaiNumber">FSSAI Number</label>
          <input
            id="fssaiNumber"
            type="text"
            name="fssaiNumber"
            value={formData.fssaiNumber}
            onChange={handleChange}
            placeholder="Enter FSSAI Number"
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
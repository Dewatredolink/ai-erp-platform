import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import InvoiceForm from '../components/InvoiceForm';
import { createInvoice, getInvoiceById, updateInvoice } from '../services/invoiceService';

function InvoiceFormPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [invoice, setInvoice] = useState(null);
  const [isLoading, setIsLoading] = useState(Boolean(id));
  const [error, setError] = useState('');

  useEffect(() => {
    if (!id) return;
    setIsLoading(true);
    getInvoiceById(id)
      .then(setInvoice)
      .catch((err) => {
        setError('Failed to load invoice.');
        console.error(err);
      })
      .finally(() => setIsLoading(false));
  }, [id]);

  async function handleSubmit(data) {
    try {
      if (id) {
        await updateInvoice(id, data);
      } else {
        await createInvoice(data);
      }
      navigate('/invoices');
    } catch (err) {
      setError(err.response?.data?.message ?? 'Failed to save invoice.');
      console.error(err);
    }
  }

  function handleCancel() {
    navigate('/invoices');
  }

  if (isLoading) {
    return <p>Loading invoice...</p>;
  }

  return (
    <div>
      {error && <p className="form-error">{error}</p>}
      <InvoiceForm invoice={invoice} onSubmit={handleSubmit} onCancel={handleCancel} />
    </div>
  );
}

export default InvoiceFormPage;

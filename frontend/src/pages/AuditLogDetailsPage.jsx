import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { getAuditLogById } from '../services/auditLogService';
import './AuditLogDetails.css';

function parseJson(value) {
  if (!value) return null;
  try {
    return JSON.parse(value);
  } catch {
    return null;
  }
}

function getChangedFields(oldValues, newValues) {
  const oldObject = oldValues && typeof oldValues === 'object' ? oldValues : {};
  const newObject = newValues && typeof newValues === 'object' ? newValues : {};
  const keys = new Set([...Object.keys(oldObject), ...Object.keys(newObject)]);
  return Array.from(keys).filter((key) => JSON.stringify(oldObject[key]) !== JSON.stringify(newObject[key]));
}

function AuditLogDetailsPage() {
  const { id } = useParams();
  const [auditLog, setAuditLog] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');

  const loadAuditLog = useCallback(async () => {
    setIsLoading(true);
    setError('');
    try {
      const data = await getAuditLogById(id);
      setAuditLog(data);
    } catch (err) {
      setError('Failed to load audit log.');
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  }, [id]);

  useEffect(() => {
    loadAuditLog();
  }, [loadAuditLog]);

  const oldValues = useMemo(() => parseJson(auditLog?.oldValues), [auditLog?.oldValues]);
  const newValues = useMemo(() => parseJson(auditLog?.newValues), [auditLog?.newValues]);
  const metadata = useMemo(() => parseJson(auditLog?.metadata), [auditLog?.metadata]);
  const changedFields = useMemo(() => getChangedFields(oldValues, newValues), [oldValues, newValues]);

  if (isLoading) {
    return <p>Loading audit log...</p>;
  }

  if (error || !auditLog) {
    return <p className="form-error">{error || 'Audit log not found.'}</p>;
  }

  return (
    <div className="audit-log-details-page">
      <div className="audit-log-details-page__toolbar">
        <h1>Audit Log #{auditLog.auditLogId}</h1>
        <Link to="/audit-logs" className="btn btn--secondary">
          Back to logs
        </Link>
      </div>

      <div className="audit-log-details-card">
        <div className="audit-log-details-grid">
          <div>
            <span className="audit-log-details-label">Timestamp</span>
            <span>{new Date(auditLog.createdDate).toLocaleString()}</span>
          </div>
          <div>
            <span className="audit-log-details-label">Username</span>
            <span>{auditLog.username || '—'}</span>
          </div>
          <div>
            <span className="audit-log-details-label">User ID</span>
            <span>{auditLog.userId ?? '—'}</span>
          </div>
          <div>
            <span className="audit-log-details-label">Entity</span>
            <span>
              {auditLog.entityName}
              {auditLog.entityId ? ` #${auditLog.entityId}` : ''}
            </span>
          </div>
          <div>
            <span className="audit-log-details-label">Action</span>
            <span>{auditLog.actionType}</span>
          </div>
          <div>
            <span className="audit-log-details-label">Correlation ID</span>
            <span>{auditLog.correlationId}</span>
          </div>
          <div>
            <span className="audit-log-details-label">IP Address</span>
            <span>{auditLog.ipAddress || '—'}</span>
          </div>
          <div>
            <span className="audit-log-details-label">User Agent</span>
            <span>{auditLog.userAgent || '—'}</span>
          </div>
          <div>
            <span className="audit-log-details-label">Request Path</span>
            <span>{auditLog.requestPath || '—'}</span>
          </div>
          <div>
            <span className="audit-log-details-label">HTTP Method</span>
            <span>{auditLog.httpMethod || '—'}</span>
          </div>
        </div>

        <section className="audit-log-details-section">
          <h2>Changed Fields</h2>
          {changedFields.length === 0 ? <p>No field changes recorded.</p> : <p>{changedFields.join(', ')}</p>}
        </section>

        <section className="audit-log-details-section">
          <h2>Old Values</h2>
          <pre>{oldValues ? JSON.stringify(oldValues, null, 2) : '—'}</pre>
        </section>

        <section className="audit-log-details-section">
          <h2>New Values</h2>
          <pre>{newValues ? JSON.stringify(newValues, null, 2) : '—'}</pre>
        </section>

        <section className="audit-log-details-section">
          <h2>Metadata</h2>
          <pre>{metadata ? JSON.stringify(metadata, null, 2) : '—'}</pre>
        </section>
      </div>
    </div>
  );
}

export default AuditLogDetailsPage;

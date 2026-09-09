import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { exportAuditLogs, getAuditLogs } from '../services/auditLogService';
import './AuditLogList.css';

function AuditLogListPage() {
  const [logs, setLogs] = useState([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);
  const [username, setUsername] = useState('');
  const [entityName, setEntityName] = useState('');
  const [actionType, setActionType] = useState('');
  const [correlationId, setCorrelationId] = useState('');
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');
  const [isExportingJson, setIsExportingJson] = useState(false);
  const [isExportingCsv, setIsExportingCsv] = useState(false);

  const loadAuditLogs = useCallback(async () => {
    setIsLoading(true);
    setError('');
    try {
      const params = { page, pageSize };
      if (username.trim()) params.username = username.trim();
      if (entityName.trim()) params.entityName = entityName.trim();
      if (actionType.trim()) params.actionType = actionType.trim();
      if (correlationId.trim()) params.correlationId = correlationId.trim();
      if (fromDate) params.fromDate = fromDate;
      if (toDate) params.toDate = toDate;

      const data = await getAuditLogs(params);
      setLogs(data.items);
      setTotalCount(data.totalCount);
    } catch (err) {
      setError('Failed to load audit logs.');
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  }, [page, pageSize, username, entityName, actionType, correlationId, fromDate, toDate]);

  useEffect(() => {
    loadAuditLogs();
  }, [loadAuditLogs]);

  async function handleExport(format) {
    const params = {};
    if (username.trim()) params.username = username.trim();
    if (entityName.trim()) params.entityName = entityName.trim();
    if (actionType.trim()) params.actionType = actionType.trim();
    if (correlationId.trim()) params.correlationId = correlationId.trim();
    if (fromDate) params.fromDate = fromDate;
    if (toDate) params.toDate = toDate;

    if (format === 'csv') {
      setIsExportingCsv(true);
    } else {
      setIsExportingJson(true);
    }

    try {
      const data = await exportAuditLogs(params, format);
      if (format === 'json' && data) {
        const json = JSON.stringify(data, null, 2);
        const blob = new Blob([json], { type: 'application/json' });
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.setAttribute('download', `audit-logs-${new Date().toISOString().slice(0, 10)}.json`);
        document.body.appendChild(link);
        link.click();
        link.remove();
        window.URL.revokeObjectURL(url);
      }
    } catch (err) {
      setError('Failed to export audit logs.');
      console.error(err);
    } finally {
      setIsExportingCsv(false);
      setIsExportingJson(false);
    }
  }

  function handleSearchSubmit(event) {
    event.preventDefault();
    if (page !== 1) {
      setPage(1);
      return;
    }
    loadAuditLogs();
  }

  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

  return (
    <div className="audit-log-list-page">
      <div className="audit-log-list-page__toolbar">
        <h1>Audit Logs</h1>
        <div className="audit-log-list-page__export-actions">
          <button
            type="button"
            className="btn btn--secondary"
            onClick={() => handleExport('json')}
            disabled={isExportingJson || isExportingCsv}
          >
            {isExportingJson ? 'Exporting JSON...' : 'Export JSON'}
          </button>
          <button
            type="button"
            className="btn btn--secondary"
            onClick={() => handleExport('csv')}
            disabled={isExportingJson || isExportingCsv}
          >
            {isExportingCsv ? 'Exporting CSV...' : 'Export CSV'}
          </button>
        </div>
      </div>

      <form className="audit-log-list-page__filters" onSubmit={handleSearchSubmit}>
        <input
          type="text"
          placeholder="User"
          value={username}
          onChange={(e) => setUsername(e.target.value)}
        />
        <input
          type="text"
          placeholder="Entity Name"
          value={entityName}
          onChange={(e) => setEntityName(e.target.value)}
        />
        <input
          type="text"
          placeholder="Action Type"
          value={actionType}
          onChange={(e) => setActionType(e.target.value)}
        />
        <input
          type="text"
          placeholder="Correlation ID"
          value={correlationId}
          onChange={(e) => setCorrelationId(e.target.value)}
        />
        <label>
          From
          <input type="date" value={fromDate} onChange={(e) => setFromDate(e.target.value)} />
        </label>
        <label>
          To
          <input type="date" value={toDate} onChange={(e) => setToDate(e.target.value)} />
        </label>
        <button type="submit" className="btn btn--secondary">
          Apply Filters
        </button>
      </form>

      {error && <p className="form-error">{error}</p>}

      {isLoading ? (
        <p>Loading audit logs...</p>
      ) : (
        <table className="audit-log-list-table">
          <thead>
            <tr>
              <th>Timestamp</th>
              <th>User</th>
              <th>Entity</th>
              <th>Action</th>
              <th>Correlation ID</th>
              <th>Details</th>
            </tr>
          </thead>
          <tbody>
            {logs.length === 0 ? (
              <tr>
                <td colSpan={6}>No audit logs found.</td>
              </tr>
            ) : (
              logs.map((log) => (
                <tr key={log.auditLogId}>
                  <td>{new Date(log.createdDate).toLocaleString()}</td>
                  <td>{log.username || '—'}</td>
                  <td>
                    {log.entityName}
                    {log.entityId ? ` #${log.entityId}` : ''}
                  </td>
                  <td>{log.actionType}</td>
                  <td>{log.correlationId}</td>
                  <td>
                    <Link to={`/audit-logs/${log.auditLogId}`}>View</Link>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      )}

      <div className="audit-log-list-page__pagination">
        <button
          type="button"
          className="btn btn--secondary"
          onClick={() => setPage((current) => Math.max(1, current - 1))}
          disabled={page <= 1}
        >
          Previous
        </button>
        <span>
          Page {page} of {totalPages}
        </span>
        <button
          type="button"
          className="btn btn--secondary"
          onClick={() => setPage((current) => Math.min(totalPages, current + 1))}
          disabled={page >= totalPages}
        >
          Next
        </button>
      </div>
    </div>
  );
}

export default AuditLogListPage;

import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { searchAuditLogs, exportAuditLogsCsv, type SearchAuditLogsParams, type AuditLogDto } from '../../shared/api/audit-logs';

export default function AuditLogPage() {
  const [actionFilter, setActionFilter] = useState('');
  const [targetFilter, setTargetFilter] = useState('');
  const [fromFilter, setFromFilter] = useState('');
  const [toFilter, setToFilter] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize] = useState(50);
  const [expandedRow, setExpandedRow] = useState<string | null>(null);
  const [exporting, setExporting] = useState(false);

  const params: SearchAuditLogsParams = {
    action: actionFilter || undefined,
    targetId: targetFilter || undefined,
    from: fromFilter || undefined,
    to: toFilter || undefined,
    page,
    pageSize,
  };

  const { data, isLoading } = useQuery({
    queryKey: ['audit-logs', params],
    queryFn: () => searchAuditLogs(params),
  });

  const handleExport = async () => {
    setExporting(true);
    try {
      const blob = await exportAuditLogsCsv({
        action: actionFilter || undefined,
        targetId: targetFilter || undefined,
        from: fromFilter || new Date(Date.now() - 30 * 86400000).toISOString(),
        to: toFilter || undefined,
      });
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `audit-log-${new Date().toISOString().split('T')[0]}.csv`;
      a.click();
      URL.revokeObjectURL(url);
    } finally {
      setExporting(false);
    }
  };

  const totalPages = data ? Math.ceil(data.totalCount / pageSize) : 0;

  return (
    <div className="audit-log-page">
      <div className="audit-log-header">
        <h2>Audit Trail</h2>
        <button className="btn btn-secondary" onClick={handleExport} disabled={exporting}>
          {exporting ? 'Exporting…' : 'Export CSV'}
        </button>
      </div>

      <div className="audit-log-filters">
        <input
          type="text"
          placeholder="Filter by action…"
          value={actionFilter}
          onChange={(e) => { setActionFilter(e.target.value); setPage(1); }}
          className="input"
        />
        <input
          type="text"
          placeholder="Filter by target ID…"
          value={targetFilter}
          onChange={(e) => { setTargetFilter(e.target.value); setPage(1); }}
          className="input"
        />
        <input
          type="date"
          value={fromFilter ? fromFilter.split('T')[0] : ''}
          onChange={(e) => { setFromFilter(e.target.value ? new Date(e.target.value).toISOString() : ''); setPage(1); }}
          className="input"
        />
        <input
          type="date"
          value={toFilter ? toFilter.split('T')[0] : ''}
          onChange={(e) => { setToFilter(e.target.value ? new Date(e.target.value).toISOString() : ''); setPage(1); }}
          className="input"
        />
      </div>

      {isLoading && (
        <div className="audit-log-skeleton">
          {Array.from({ length: 5 }).map((_, i) => (
            <div key={i} className="skeleton-row" />
          ))}
        </div>
      )}

      {!isLoading && data && data.logs.length === 0 && (
        <div className="audit-log-empty">
          <p>No audit logs found.</p>
          <button className="link-btn" onClick={() => { setActionFilter(''); setTargetFilter(''); setFromFilter(''); setToFilter(''); }}>
            Reset filters
          </button>
        </div>
      )}

      {!isLoading && data && data.logs.length > 0 && (
        <>
          <table className="audit-log-table">
            <thead>
              <tr>
                <th>Timestamp</th>
                <th>Actor</th>
                <th>Action</th>
                <th>Target</th>
                <th>IP Address</th>
              </tr>
            </thead>
            <tbody>
              {data.logs.map((log) => (
                <AuditLogRow
                  key={log.id}
                  log={log}
                  expanded={expandedRow === log.id}
                  onToggle={() => setExpandedRow(expandedRow === log.id ? null : log.id)}
                />
              ))}
            </tbody>
          </table>

          <div className="audit-log-pagination">
            <button disabled={page <= 1} onClick={() => setPage(page - 1)}>Previous</button>
            <span>Page {page} of {totalPages}</span>
            <button disabled={page >= totalPages} onClick={() => setPage(page + 1)}>Next</button>
          </div>
        </>
      )}
    </div>
  );
}

function AuditLogRow({ log, expanded, onToggle }: { log: AuditLogDto; expanded: boolean; onToggle: () => void }) {
  const ts = new Date(log.timestamp);
  const formatted = `${ts.toLocaleDateString()} ${ts.toLocaleTimeString()}`;

  return (
    <>
      <tr onClick={onToggle} className="audit-log-row" style={{ cursor: 'pointer' }}>
        <td>{formatted}</td>
        <td>{log.actorName}</td>
        <td>{log.action}</td>
        <td className="target-id">{log.targetId}</td>
        <td>{log.ipAddress}</td>
      </tr>
      {expanded && log.metadata && (
        <tr className="audit-log-detail">
          <td colSpan={5}>
            <pre className="metadata-json">{formatJson(log.metadata)}</pre>
          </td>
        </tr>
      )}
    </>
  );
}

function formatJson(raw: string): string {
  try {
    return JSON.stringify(JSON.parse(raw), null, 2);
  } catch {
    return raw;
  }
}

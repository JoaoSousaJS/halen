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

  const resetFilters = () => {
    setActionFilter('');
    setTargetFilter('');
    setFromFilter('');
    setToFilter('');
    setPage(1);
  };

  const totalPages = data ? Math.ceil(data.totalCount / pageSize) : 0;

  return (
    <div>
      <div className="admin-page-head" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <h2>Audit Trail</h2>
        <button className="btn btn-sm" onClick={handleExport} disabled={exporting}>
          {exporting ? 'Exporting…' : 'Export CSV'}
        </button>
      </div>

      <div className="admin-toolbar">
        <input
          type="text"
          placeholder="Filter by action…"
          value={actionFilter}
          onChange={(e) => { setActionFilter(e.target.value); setPage(1); }}
          className="admin-search"
        />
        <input
          type="text"
          placeholder="Filter by target ID…"
          value={targetFilter}
          onChange={(e) => { setTargetFilter(e.target.value); setPage(1); }}
          className="admin-search"
        />
        <input
          type="date"
          value={fromFilter ? fromFilter.split('T')[0] : ''}
          onChange={(e) => { setFromFilter(e.target.value ? new Date(e.target.value).toISOString() : ''); setPage(1); }}
          className="admin-search"
          aria-label="From date"
        />
        <input
          type="date"
          value={toFilter ? toFilter.split('T')[0] : ''}
          onChange={(e) => { setToFilter(e.target.value ? new Date(e.target.value).toISOString() : ''); setPage(1); }}
          className="admin-search"
          aria-label="To date"
        />
      </div>

      {isLoading && (
        <div className="admin-table-wrap">
          {Array.from({ length: 5 }).map((_, i) => (
            <div key={i} className="skeleton-row" style={{ height: 42, borderBottom: '1px solid var(--border)' }} />
          ))}
        </div>
      )}

      {!isLoading && data && data.logs.length === 0 && (
        <div className="admin-table-wrap" style={{ padding: 40, textAlign: 'center' }}>
          <p style={{ color: 'var(--text-muted)', marginBottom: 8 }}>No audit logs found.</p>
          <button className="btn btn-sm" onClick={resetFilters}>Reset filters</button>
        </div>
      )}

      {!isLoading && data && data.logs.length > 0 && (
        <>
          <div className="admin-table-wrap">
            <table className="admin-table">
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
          </div>

          <div className="admin-pagination">
            <button className="btn btn-sm" disabled={page <= 1} onClick={() => setPage(page - 1)}>Previous</button>
            <span style={{ fontSize: 13, color: 'var(--text-muted)' }}>
              Page {page} of {totalPages} · {data.totalCount} total
            </span>
            <button className="btn btn-sm" disabled={page >= totalPages} onClick={() => setPage(page + 1)}>Next</button>
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
      <tr onClick={onToggle} style={{ cursor: 'pointer' }}>
        <td className="admin-mono">{formatted}</td>
        <td>{log.actorName}</td>
        <td><span className="chip">{log.action}</span></td>
        <td className="admin-mono" style={{ maxWidth: 160, overflow: 'hidden', textOverflow: 'ellipsis' }}>{log.targetId}</td>
        <td className="admin-mono">{log.ipAddress}</td>
      </tr>
      {expanded && log.metadata && (
        <tr>
          <td colSpan={5} style={{ background: 'var(--surface-2)', padding: '12px 14px' }}>
            <pre style={{ margin: 0, fontSize: 12, fontFamily: 'var(--font-mono)', whiteSpace: 'pre-wrap', color: 'var(--text-dim)' }}>
              {formatJson(log.metadata)}
            </pre>
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

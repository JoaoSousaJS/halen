import { useState } from 'react';
import type { FormEvent } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { listClinics, createClinic } from '../../shared/api/clinics';
import { getApiError } from '../../shared/api/errors';

interface ClinicsPageProps {
  onSelectClinic: (id: string) => void;
}

function formatDate(dateStr: string): string {
  const d = new Date(dateStr);
  return d.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
}

export default function ClinicsPage({ onSelectClinic }: ClinicsPageProps) {
  const queryClient = useQueryClient();
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [showCreate, setShowCreate] = useState(false);

  const clinics = useQuery({
    queryKey: ['clinics', search, page],
    queryFn: () => listClinics({ search: search || undefined, page, pageSize: 20 }),
  });

  const activeCount = clinics.data?.clinics.filter((c) => c.isActive).length ?? 0;
  const totalCount = clinics.data?.clinics.length ?? 0;

  return (
    <section className="clinics-page">
      <div className="admin-page-head">
        <div>
          <div className="admin-tag">Clinic management</div>
          <h1 className="auth-heading">Clinics.</h1>
          <p className="text-dim" style={{ marginTop: 4 }}>
            {clinics.data
              ? `${totalCount} clinic${totalCount !== 1 ? 's' : ''} · ${activeCount} active`
              : 'Manage all platform clinics'}
          </p>
        </div>
      </div>

      <div className="admin-toolbar">
        <input
          type="search"
          className="admin-search"
          placeholder="Search clinics..."
          value={search}
          onChange={(e) => { setSearch(e.target.value); setPage(1); }}
        />
        <button className="btn btn-primary clinics-create-btn" onClick={() => setShowCreate(true)}>
          <span className="clinics-create-icon">+</span>
          Create clinic
        </button>
      </div>

      {clinics.isLoading && <p className="text-dim">Loading...</p>}
      {clinics.error && <p className="clinics-error">{getApiError(clinics.error)}</p>}

      {clinics.data && clinics.data.clinics.length === 0 && (
        <div className="clinics-empty">
          <div className="clinics-empty-icon">&#9678;</div>
          <p>No clinics found</p>
          <span className="text-dim">Try adjusting your search or create a new clinic</span>
        </div>
      )}

      {clinics.data && clinics.data.clinics.length > 0 && (
        <>
          <div className="admin-table-wrap">
            <table className="admin-table">
              <thead>
                <tr>
                  <th>Name</th>
                  <th>Slug</th>
                  <th>Status</th>
                  <th>Created</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {clinics.data.clinics.map((c) => (
                  <tr
                    key={c.id}
                    onClick={() => onSelectClinic(c.id)}
                    className="clinics-row"
                  >
                    <td>
                      <div className="clinics-name-cell">
                        <div className={`clinics-indicator ${c.isActive ? 'clinics-indicator--active' : 'clinics-indicator--inactive'}`} />
                        <span className="admin-user-name">{c.name}</span>
                      </div>
                    </td>
                    <td><span className="admin-mono">{c.slug}</span></td>
                    <td>
                      <span className={`chip ${c.isActive ? 'chip-good' : ''}`}>
                        {c.isActive ? 'Active' : 'Inactive'}
                      </span>
                    </td>
                    <td className="admin-mono text-dim">{formatDate(c.createdAt)}</td>
                    <td>
                      <span className="clinics-row-arrow">&rsaquo;</span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="admin-pagination">
            <button className="btn btn-sm" disabled={page <= 1} onClick={() => setPage(page - 1)}>
              &larr; Prev
            </button>
            <span className="text-dim">Page {page}</span>
            <button
              className="btn btn-sm"
              disabled={clinics.data.clinics.length < 20}
              onClick={() => setPage(page + 1)}
            >
              Next &rarr;
            </button>
          </div>
        </>
      )}

      {showCreate && (
        <CreateClinicDialog
          onClose={() => setShowCreate(false)}
          onCreated={() => {
            setShowCreate(false);
            queryClient.invalidateQueries({ queryKey: ['clinics'] });
          }}
        />
      )}
    </section>
  );
}

function CreateClinicDialog({ onClose, onCreated }: { onClose: () => void; onCreated: () => void }) {
  const [name, setName] = useState('');
  const [slug, setSlug] = useState('');
  const [error, setError] = useState('');

  const create = useMutation({
    mutationFn: () => createClinic({ name, slug }),
    onSuccess: onCreated,
    onError: (err) => setError(getApiError(err)),
  });

  function handleNameChange(value: string) {
    setName(value);
    setSlug(value.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, ''));
  }

  function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError('');
    create.mutate();
  }

  return (
    <div className="dialog-overlay" onClick={onClose}>
      <div className="dialog" onClick={(e) => e.stopPropagation()}>
        <div className="dialog-header">
          <h3 className="dialog-title">Create Clinic</h3>
          <button type="button" className="dialog-close" onClick={onClose} aria-label="Close dialog">
            &times;
          </button>
        </div>
        <form onSubmit={handleSubmit} className="dialog-body">
          <label className="field">
            <span>Name</span>
            <input className="input" value={name} onChange={(e) => handleNameChange(e.target.value)} required placeholder="e.g. Sunrise Health" />
          </label>
          <label className="field">
            <span>Slug</span>
            <input className="input" value={slug} onChange={(e) => setSlug(e.target.value)} required pattern="[a-z0-9-]+" placeholder="auto-generated-from-name" />
            <span className="field-hint">URL-safe identifier. Lowercase letters, numbers, and hyphens only.</span>
          </label>
          {error && <p className="dialog-error">{error}</p>}
          <div className="dialog-actions">
            <button type="button" className="btn btn-ghost" onClick={onClose}>Cancel</button>
            <button type="submit" className="btn btn-primary" disabled={create.isPending}>
              {create.isPending ? 'Creating...' : 'Create'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

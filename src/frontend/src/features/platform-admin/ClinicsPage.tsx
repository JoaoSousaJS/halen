import { useState } from 'react';
import type { FormEvent } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { listClinics, createClinic } from '../../shared/api/clinics';
import { getApiError } from '../../shared/api/errors';

interface ClinicsPageProps {
  onSelectClinic: (id: string) => void;
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

  return (
    <section>
      <div className="section-header">
        <h2>Clinics</h2>
        <button className="btn btn-primary" onClick={() => setShowCreate(true)}>
          Create clinic
        </button>
      </div>

      <input
        type="search"
        className="input"
        placeholder="Search clinics..."
        value={search}
        onChange={(e) => { setSearch(e.target.value); setPage(1); }}
      />

      {clinics.isLoading && <p className="text-dim">Loading...</p>}
      {clinics.error && <p className="text-error">{getApiError(clinics.error)}</p>}

      {clinics.data && (
        <>
          <table className="table">
            <thead>
              <tr>
                <th>Name</th>
                <th>Slug</th>
                <th>Status</th>
                <th>Created</th>
              </tr>
            </thead>
            <tbody>
              {clinics.data.clinics.map((c) => (
                <tr key={c.id} onClick={() => onSelectClinic(c.id)} className="clickable-row">
                  <td>{c.name}</td>
                  <td><code>{c.slug}</code></td>
                  <td>{c.isActive ? 'Active' : 'Inactive'}</td>
                  <td>{new Date(c.createdAt).toLocaleDateString()}</td>
                </tr>
              ))}
            </tbody>
          </table>

          <div className="pagination">
            <button className="btn btn-sm" disabled={page <= 1} onClick={() => setPage(page - 1)}>
              Prev
            </button>
            <span>Page {page}</span>
            <button
              className="btn btn-sm"
              disabled={clinics.data.clinics.length < 20}
              onClick={() => setPage(page + 1)}
            >
              Next
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
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal" onClick={(e) => e.stopPropagation()}>
        <h3>Create Clinic</h3>
        <form onSubmit={handleSubmit}>
          <label className="field">
            <span>Name</span>
            <input className="input" value={name} onChange={(e) => handleNameChange(e.target.value)} required />
          </label>
          <label className="field">
            <span>Slug</span>
            <input className="input" value={slug} onChange={(e) => setSlug(e.target.value)} required pattern="[a-z0-9-]+" />
          </label>
          {error && <p className="text-error">{error}</p>}
          <div className="modal-actions">
            <button type="button" className="btn" onClick={onClose}>Cancel</button>
            <button type="submit" className="btn btn-primary" disabled={create.isPending}>
              {create.isPending ? 'Creating...' : 'Create'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

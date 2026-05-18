import { useState, useRef } from 'react';
import type { FormEvent } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { getApiError } from '../../../shared/api/errors';
import {
  getPatientDocuments,
  uploadDocument,
  downloadDocument,
  deleteDocument,
} from '../../../shared/api/medical-records';
import type { DocumentDto, DocumentType } from '../../../shared/api/medical-records';
import { Button, Field, Input, Select, Dialog, DialogActions, Chip } from '../../../shared/components';

interface DocumentsPanelProps {
  patientProfileId: string;
}

const DOCUMENT_TYPE_OPTIONS: { value: DocumentType; label: string }[] = [
  { value: 'LabResult', label: 'Lab Result' },
  { value: 'Imaging', label: 'Imaging' },
  { value: 'Referral', label: 'Referral' },
  { value: 'Discharge', label: 'Discharge Summary' },
  { value: 'Insurance', label: 'Insurance' },
  { value: 'Consent', label: 'Consent Form' },
  { value: 'Other', label: 'Other' },
];

const TYPE_CHIP_VARIANT: Partial<Record<DocumentType, 'good' | 'warn' | 'danger'>> = {
  LabResult: 'good',
  Imaging: 'warn',
};

function formatFileSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

export default function DocumentsPanel({ patientProfileId }: DocumentsPanelProps) {
  const queryClient = useQueryClient();
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [showUploadForm, setShowUploadForm] = useState(false);
  const [typeFilter, setTypeFilter] = useState('');

  const [file, setFile] = useState<File | null>(null);
  const [docType, setDocType] = useState<DocumentType | ''>('');
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');

  const documents = useQuery({
    queryKey: ['patient-documents', patientProfileId],
    queryFn: () => getPatientDocuments(patientProfileId),
  });

  const upload = useMutation({
    mutationFn: (formData: FormData) => uploadDocument(patientProfileId, formData),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['patient-documents', patientProfileId] });
      resetForm();
    },
  });

  const remove = useMutation({
    mutationFn: (documentId: string) => deleteDocument(patientProfileId, documentId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['patient-documents', patientProfileId] });
    },
  });

  function resetForm() {
    setFile(null);
    setDocType('');
    setTitle('');
    setDescription('');
    setShowUploadForm(false);
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  }

  function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (!file || !docType) return;

    const formData = new FormData();
    formData.append('file', file);
    formData.append('type', docType);
    formData.append('title', title);
    if (description) {
      formData.append('description', description);
    }

    upload.mutate(formData);
  }

  function handleDownload(documentId: string) {
    downloadDocument(patientProfileId, documentId);
  }

  if (documents.isLoading) {
    return <p className="text-dim" role="status">Loading documents...</p>;
  }

  if (documents.isError) {
    return <p className="auth-error">Failed to load documents.</p>;
  }

  const allItems = documents.data ?? [];
  const filteredItems = typeFilter
    ? allItems.filter((doc: DocumentDto) => doc.type === typeFilter)
    : allItems;

  return (
    <section className="panel" aria-label="Documents">
      <div className="panel-header">
        <h3>Documents</h3>
        <Button size="sm" onClick={() => setShowUploadForm(true)}>Upload Document</Button>
      </div>

      <div className="panel-filters">
        <Field label="Filter by type">
          <Select
            value={typeFilter}
            onChange={(e) => setTypeFilter(e.target.value)}
            options={DOCUMENT_TYPE_OPTIONS}
            placeholder="All types"
            aria-label="Filter by document type"
          />
        </Field>
      </div>

      {filteredItems.length === 0 ? (
        <p className="panel-empty text-dim">
          {allItems.length === 0 ? 'No documents uploaded.' : 'No documents match the selected filter.'}
        </p>
      ) : (
        <ul className="record-list" role="list">
          {filteredItems.map((doc: DocumentDto) => (
            <li key={doc.id} className="record-card">
              <div className="record-card-header">
                <strong>{doc.title}</strong>
                <Chip
                  status={DOCUMENT_TYPE_OPTIONS.find((o) => o.value === doc.type)?.label ?? doc.type}
                  variant={TYPE_CHIP_VARIANT[doc.type]}
                />
              </div>
              <div className="record-card-body">
                <span>{doc.fileName} ({formatFileSize(doc.fileSize)})</span>
                <span className="text-dim">Uploaded by: {doc.uploadedBy}</span>
                <span className="text-dim">
                  Date: {new Date(doc.uploadedAt).toLocaleDateString()}
                </span>
              </div>
              <div className="record-card-actions">
                <Button
                  size="sm"
                  ariaLabel={`Download ${doc.title}`}
                  onClick={() => handleDownload(doc.id)}
                >
                  Download
                </Button>
                <Button
                  variant="danger"
                  size="sm"
                  ariaLabel={`Delete ${doc.title}`}
                  disabled={remove.isPending}
                  onClick={() => remove.mutate(doc.id)}
                >
                  Delete
                </Button>
              </div>
            </li>
          ))}
        </ul>
      )}

      {remove.isError ? (
        <p className="auth-error">{getApiError(remove.error)}</p>
      ) : null}

      {showUploadForm ? (
        <Dialog title="Upload Document" onClose={() => setShowUploadForm(false)}>
          <form onSubmit={handleSubmit} aria-label="Upload document form">
            <Field label="File">
              <input
                ref={fileInputRef}
                type="file"
                required
                onChange={(e) => setFile(e.target.files?.[0] ?? null)}
                aria-label="Choose file"
              />
            </Field>

            <Field label="Document Type">
              <Select
                required
                value={docType}
                onChange={(e) => setDocType(e.target.value as DocumentType)}
                options={DOCUMENT_TYPE_OPTIONS}
                placeholder="Select type"
              />
            </Field>

            <Field label="Title">
              <Input
                required
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                placeholder="e.g. Blood work results"
              />
            </Field>

            <Field label="Description">
              <textarea
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                rows={3}
                placeholder="Optional description"
              />
            </Field>

            {upload.isError ? (
              <p className="auth-error">{getApiError(upload.error)}</p>
            ) : null}

            <DialogActions>
              <Button variant="ghost" type="button" onClick={() => setShowUploadForm(false)}>
                Cancel
              </Button>
              <Button variant="primary" type="submit" disabled={upload.isPending || !file}>
                {upload.isPending ? 'Uploading...' : 'Upload'}
              </Button>
            </DialogActions>
          </form>
        </Dialog>
      ) : null}
    </section>
  );
}

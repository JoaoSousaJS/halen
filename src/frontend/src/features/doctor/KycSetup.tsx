import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { getKycStatus, submitKycDocuments } from '../../shared/api/doctor';
import { getApiError } from '../../shared/api/errors';

export default function KycSetup() {
  const queryClient = useQueryClient();
  const kyc = useQuery({ queryKey: ['kyc-status'], queryFn: getKycStatus });

  const [licensePhoto, setLicensePhoto] = useState<File | null>(null);
  const [medicalCertificate, setMedicalCertificate] = useState<File | null>(null);
  const [identityProof, setIdentityProof] = useState<File | null>(null);

  const submit = useMutation({
    mutationFn: submitKycDocuments,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['kyc-status'] });
    },
  });

  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (!licensePhoto || !medicalCertificate || !identityProof) return;
    submit.mutate({ licensePhoto, medicalCertificate, identityProof });
  }

  if (kyc.isLoading) return <p className="text-dim">Loading KYC status…</p>;
  if (kyc.isError) return <p className="auth-error">Failed to load KYC status.</p>;

  const status = kyc.data?.status;

  if (status === 'Approved') {
    return (
      <div className="kyc-section">
        <div className="auth-card" style={{ maxWidth: 560 }}>
          <p style={{ color: 'var(--accent)', fontWeight: 600 }}>Your KYC documents have been approved.</p>
        </div>
      </div>
    );
  }

  if (status === 'Submitted') {
    return (
      <div className="kyc-section">
        <h2 className="section-heading">KYC documents submitted</h2>
        <div className="auth-card" style={{ maxWidth: 560 }}>
          <p className="text-dim">
            Your documents are under review. You will be notified once an admin reviews them.
          </p>
          {kyc.data?.submittedAt ? (
            <p className="text-dim">
              Submitted on {new Date(kyc.data.submittedAt).toLocaleDateString()}
            </p>
          ) : null}
          {kyc.data?.documents.length ? (
            <ul style={{ paddingLeft: 20 }}>
              {kyc.data.documents.map((doc) => (
                <li key={doc.id} className="text-dim">{doc.documentType}: {doc.fileName}</li>
              ))}
            </ul>
          ) : null}
        </div>
      </div>
    );
  }

  return (
    <div className="kyc-section">
      <h2 className="section-heading">Complete your KYC verification</h2>
      <p className="text-dim">
        Upload your documents to get verified and start seeing patients.
      </p>

      {status === 'Rejected' && kyc.data?.lastRejectionReason ? (
        <div className="auth-error" style={{ padding: 12 }}>
          <strong>Your previous submission was rejected:</strong><br />
          {kyc.data.lastRejectionReason}
        </div>
      ) : null}

      <div className="auth-card" style={{ maxWidth: 560 }}>
        <form onSubmit={handleSubmit} className="auth-form">
          <label className="field">
            <span>License photo</span>
            <input
              type="file"
              required
              accept="image/jpeg,image/png,application/pdf"
              onChange={(e) => setLicensePhoto(e.target.files?.[0] ?? null)}
            />
          </label>

          <label className="field">
            <span>Medical certificate</span>
            <input
              type="file"
              required
              accept="image/jpeg,image/png,application/pdf"
              onChange={(e) => setMedicalCertificate(e.target.files?.[0] ?? null)}
            />
          </label>

          <label className="field">
            <span>Identity proof</span>
            <input
              type="file"
              required
              accept="image/jpeg,image/png,application/pdf"
              onChange={(e) => setIdentityProof(e.target.files?.[0] ?? null)}
            />
          </label>

          {submit.isError ? (
            <p className="auth-error">{getApiError(submit.error)}</p>
          ) : null}

          <button
            type="submit"
            className="btn btn-primary btn-block"
            disabled={submit.isPending || !licensePhoto || !medicalCertificate || !identityProof}
          >
            {submit.isPending ? 'Submitting…' : 'Submit for review'}
          </button>
        </form>
      </div>
    </div>
  );
}

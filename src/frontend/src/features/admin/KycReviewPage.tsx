import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { getDoctorKycDetails, reviewKyc, downloadKycDocument } from '../../shared/api/admin';
import { getApiError } from '../../shared/api/errors';

interface KycReviewPageProps {
  doctorProfileId: string;
  onBack: () => void;
}

export default function KycReviewPage({ doctorProfileId, onBack }: KycReviewPageProps) {
  const queryClient = useQueryClient();
  const [rejectionReason, setRejectionReason] = useState('');
  const [showRejectForm, setShowRejectForm] = useState(false);

  const details = useQuery({
    queryKey: ['admin-kyc-details', doctorProfileId],
    queryFn: () => getDoctorKycDetails(doctorProfileId),
  });

  const review = useMutation({
    mutationFn: (payload: { decision: 'Approved' | 'Rejected'; rejectionReason?: string }) =>
      reviewKyc(doctorProfileId, payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin-users'] });
      queryClient.invalidateQueries({ queryKey: ['admin-kyc-details', doctorProfileId] });
      onBack();
    },
  });

  if (details.isLoading) return <p className="text-dim">Loading KYC details…</p>;
  if (details.isError) return <p className="auth-error">Failed to load KYC details.</p>;

  const data = details.data;
  if (!data) return null;

  return (
    <>
      <button className="btn btn-sm" onClick={onBack} style={{ marginBottom: 16 }}>
        ← Back to users
      </button>

      <div className="admin-page-head">
        <div>
          <div className="admin-tag">KYC Review</div>
          <h1 className="auth-heading">{data.doctorName}</h1>
          <p className="text-dim" style={{ marginTop: 4 }}>
            {data.specialty} · License: {data.licenseNumber}
          </p>
        </div>
        <span className={`chip ${data.status === 'Submitted' ? 'chip-warn' : data.status === 'Approved' ? 'chip-good' : 'chip-danger'}`}>
          {data.status}
        </span>
      </div>

      <section style={{ marginTop: 24 }}>
        <h2 className="section-heading">Uploaded documents</h2>
        {data.documents.length === 0 ? (
          <p className="text-dim">No documents uploaded.</p>
        ) : (
          <div style={{ display: 'flex', gap: 16, flexWrap: 'wrap', marginTop: 12 }}>
            {data.documents.map((doc) => (
              <div key={doc.id} className="auth-card" style={{ padding: 12, minWidth: 180 }}>
                <p style={{ fontWeight: 600, fontSize: 13 }}>{doc.documentType}</p>
                <p className="text-dim" style={{ fontSize: 12 }}>{doc.fileName}</p>
                <p className="text-dim" style={{ fontSize: 12 }}>
                  {new Date(doc.uploadedAt).toLocaleDateString()}
                </p>
                <button
                  className="btn btn-sm"
                  style={{ marginTop: 8 }}
                  onClick={async () => {
                    const blob = await downloadKycDocument(doc.id);
                    const url = URL.createObjectURL(blob);
                    const a = document.createElement('a');
                    a.href = url;
                    a.download = doc.fileName;
                    a.click();
                    URL.revokeObjectURL(url);
                  }}
                >
                  Download
                </button>
              </div>
            ))}
          </div>
        )}
      </section>

      {data.reviewHistory.length > 0 ? (
        <section style={{ marginTop: 24 }}>
          <h2 className="section-heading">Review history</h2>
          <div style={{ marginTop: 12 }}>
            {data.reviewHistory.map((r) => (
              <div key={r.id} className="auth-card" style={{ padding: 12, marginBottom: 8 }}>
                <span className={`chip ${r.decision === 'Approved' ? 'chip-good' : 'chip-danger'}`}>
                  {r.decision}
                </span>
                <span className="text-dim" style={{ marginLeft: 8 }}>
                  by {r.reviewerName} on {new Date(r.reviewedAt).toLocaleDateString()}
                </span>
                {r.rejectionReason ? (
                  <p style={{ marginTop: 4, fontSize: 13 }}>{r.rejectionReason}</p>
                ) : null}
              </div>
            ))}
          </div>
        </section>
      ) : null}

      {data.status === 'Submitted' ? (
        <section style={{ marginTop: 24 }}>
          <h2 className="section-heading">Review actions</h2>
          <div style={{ display: 'flex', gap: 12, marginTop: 12 }}>
            <button
              className="btn btn-primary"
              disabled={review.isPending}
              onClick={() => review.mutate({ decision: 'Approved' })}
            >
              {review.isPending ? 'Processing…' : 'Approve'}
            </button>
            <button
              className="btn btn-danger"
              disabled={review.isPending}
              onClick={() => setShowRejectForm(true)}
            >
              Reject
            </button>
          </div>

          {showRejectForm ? (
            <div className="auth-card" style={{ marginTop: 12, maxWidth: 560 }}>
              <label className="field">
                <span>Rejection reason</span>
                <textarea
                  rows={3}
                  required
                  value={rejectionReason}
                  onChange={(e) => setRejectionReason(e.target.value)}
                  placeholder="Explain why the documents are being rejected…"
                />
              </label>
              <div style={{ display: 'flex', gap: 8, marginTop: 8 }}>
                <button
                  className="btn btn-danger btn-sm"
                  disabled={review.isPending || !rejectionReason.trim()}
                  onClick={() => review.mutate({ decision: 'Rejected', rejectionReason })}
                >
                  Confirm rejection
                </button>
                <button
                  className="btn btn-sm"
                  onClick={() => { setShowRejectForm(false); setRejectionReason(''); }}
                >
                  Cancel
                </button>
              </div>
            </div>
          ) : null}

          {review.isError ? (
            <p className="auth-error" style={{ marginTop: 8 }}>{getApiError(review.error)}</p>
          ) : null}
        </section>
      ) : null}
    </>
  );
}

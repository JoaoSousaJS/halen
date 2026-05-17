import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { getDoctorKycDetails, reviewKyc, downloadKycDocument } from '../../shared/api/admin';
import { getApiError } from '../../shared/api/errors';
import { Button, Chip } from '../../shared/components';

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
      <Button size="sm" onClick={onBack}>
        ← Back to users
      </Button>

      <div className="admin-page-head">
        <div>
          <div className="admin-tag">KYC Review</div>
          <h1 className="auth-heading">{data.doctorName}</h1>
          <p className="text-dim">
            {data.specialty} · License: {data.licenseNumber}
          </p>
        </div>
        <Chip
          status={data.status}
          variant={data.status === 'Submitted' ? 'warn' : data.status === 'Approved' ? 'good' : 'danger'}
        />
      </div>

      <section>
        <h2 className="section-heading">Uploaded documents</h2>
        {data.documents.length === 0 ? (
          <p className="text-dim">No documents uploaded.</p>
        ) : (
          <div className="kyc-docs-grid">
            {data.documents.map((doc) => (
              <div key={doc.id} className="kyc-doc-card">
                <p>{doc.documentType}</p>
                <p className="text-dim">{doc.fileName}</p>
                <p className="text-dim">
                  {new Date(doc.uploadedAt).toLocaleDateString()}
                </p>
                <Button
                  size="sm"
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
                </Button>
              </div>
            ))}
          </div>
        )}
      </section>

      {data.reviewHistory.length > 0 ? (
        <section>
          <h2 className="section-heading">Review history</h2>
          <div>
            {data.reviewHistory.map((r) => (
              <div key={r.id} className="kyc-history-item">
                <Chip status={r.decision} variant={r.decision === 'Approved' ? 'good' : 'danger'} />
                <span className="text-dim" style={{ marginLeft: 8 }}>
                  by {r.reviewerName} on {new Date(r.reviewedAt).toLocaleDateString()}
                </span>
                {r.rejectionReason ? (
                  <p style={{ fontSize: 13 }}>{r.rejectionReason}</p>
                ) : null}
              </div>
            ))}
          </div>
        </section>
      ) : null}

      {data.status === 'Submitted' ? (
        <section>
          <h2 className="section-heading">Review actions</h2>
          <div className="kyc-actions">
            <Button
              variant="primary"
              disabled={review.isPending}
              onClick={() => review.mutate({ decision: 'Approved' })}
            >
              {review.isPending ? 'Processing…' : 'Approve'}
            </Button>
            <Button
              variant="danger"
              disabled={review.isPending}
              onClick={() => setShowRejectForm(true)}
            >
              Reject
            </Button>
          </div>

          {showRejectForm ? (
            <div className="auth-card" style={{ maxWidth: 560 }}>
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
              <div className="kyc-actions">
                <Button
                  variant="danger"
                  size="sm"
                  disabled={review.isPending || !rejectionReason.trim()}
                  onClick={() => review.mutate({ decision: 'Rejected', rejectionReason })}
                >
                  Confirm rejection
                </Button>
                <Button
                  size="sm"
                  onClick={() => { setShowRejectForm(false); setRejectionReason(''); }}
                >
                  Cancel
                </Button>
              </div>
            </div>
          ) : null}

          {review.isError ? (
            <p className="auth-error">{getApiError(review.error)}</p>
          ) : null}
        </section>
      ) : null}
    </>
  );
}

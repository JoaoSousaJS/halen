import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { getDoctorKycDetails, reviewKyc, downloadKycDocument } from '../../shared/api/admin';
import { getApiError } from '../../shared/api/errors';
import { Button, Field, Chip } from '../../shared/components';

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
    <section className="kyc-review">
      <Button size="sm" className="kyc-review-back" onClick={onBack}>&larr; Back</Button>

      <div className="kyc-review-header">
        <div>
          <h2 className="section-heading">{data.doctorName}</h2>
          <p className="text-dim">
            {data.specialty} · License: {data.licenseNumber}
          </p>
        </div>
        <Chip
          status={data.status}
          variant={data.status === 'Submitted' ? 'warn' : data.status === 'Approved' ? 'good' : 'danger'}
        />
      </div>

      <div className="kyc-review-columns">
        <div className="kyc-review-section">
          <h3>Documents</h3>
          {data.documents.length === 0 ? (
            <p className="text-dim">No documents uploaded.</p>
          ) : (
            <div className="kyc-docs-grid">
              {data.documents.map((doc) => (
                <div key={doc.id} className="kyc-doc-card">
                  <span className="kyc-doc-type">{doc.documentType}</span>
                  <span className="text-dim">{doc.fileName}</span>
                  <span className="text-dim">
                    {new Date(doc.uploadedAt).toLocaleDateString()}
                  </span>
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

          {data.reviewHistory.length > 0 && (
            <>
              <h3>Review History</h3>
              <div className="kyc-history">
                {data.reviewHistory.map((r) => (
                  <div key={r.id} className="kyc-history-item">
                    <div className="kyc-history-meta">
                      <Chip status={r.decision} variant={r.decision === 'Approved' ? 'good' : 'danger'} />
                      <span className="text-dim">
                        by {r.reviewerName} on {new Date(r.reviewedAt).toLocaleDateString()}
                      </span>
                    </div>
                    {r.rejectionReason && (
                      <p className="kyc-history-reason">{r.rejectionReason}</p>
                    )}
                  </div>
                ))}
              </div>
            </>
          )}
        </div>

        {data.status === 'Submitted' && (
          <div className="kyc-review-section">
            <h3>Review Actions</h3>
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

            {showRejectForm && (
              <div className="kyc-reject-form">
                <Field label="Rejection reason">
                  <textarea
                    rows={3}
                    required
                    value={rejectionReason}
                    onChange={(e) => setRejectionReason(e.target.value)}
                    placeholder="Explain why the documents are being rejected…"
                  />
                </Field>
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
                    variant="ghost"
                    size="sm"
                    onClick={() => { setShowRejectForm(false); setRejectionReason(''); }}
                  >
                    Cancel
                  </Button>
                </div>
              </div>
            )}

            {review.isError && (
              <p className="dialog-error" role="alert">{getApiError(review.error)}</p>
            )}
          </div>
        )}
      </div>
    </section>
  );
}

import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { getApiError } from '../../shared/api/errors';
import { getModerationQueue, moderateReview } from '../../shared/api/reviews';
import type { ModerationReviewDto } from '../../shared/api/reviews';
import { Button, Chip } from '../../shared/components';
import { renderStars } from '../../shared/utils/renderStars';

type ModerationFilter = 'pending' | 'all';

const FILTER_LABELS: { value: ModerationFilter; label: string }[] = [
  { value: 'pending', label: 'Pending' },
  { value: 'all', label: 'All' },
];

export default function ReviewModeration() {
  const queryClient = useQueryClient();

  const [page, setPage] = useState(1);
  const [filter, setFilter] = useState<ModerationFilter>('pending');
  const [moderatingId, setModeratingId] = useState<string | null>(null);

  const queue = useQuery({
    queryKey: ['moderation-queue', page, filter],
    queryFn: () => getModerationQueue({ page, pageSize: 20, filter }),
  });

  const moderate = useMutation({
    mutationFn: ({ reviewId, decision }: { reviewId: string; decision: string }) =>
      moderateReview(reviewId, decision),
    onSuccess: () => {
      setModeratingId(null);
      queryClient.invalidateQueries({ queryKey: ['moderation-queue'] });
    },
    onError: () => setModeratingId(null),
  });

  const totalPages = queue.data ? Math.ceil(queue.data.totalCount / 20) : 0;

  function handleFilterChange(next: ModerationFilter) {
    setFilter(next);
    setPage(1);
  }

  function handleModerate(reviewId: string, decision: string) {
    setModeratingId(reviewId);
    moderate.mutate({ reviewId, decision });
  }

  return (
    <section className="review-moderation">
      <h1 className="auth-heading">
        Review<br /><em>moderation.</em>
      </h1>

      {/* Filter tabs */}
      <nav className="review-moderation-filters" aria-label="Moderation filters">
        {FILTER_LABELS.map((f) => (
          <Button
            key={f.value}
            variant={filter === f.value ? 'primary' : 'ghost'}
            size="sm"
            onClick={() => handleFilterChange(f.value)}
            aria-pressed={filter === f.value}
          >
            {f.label}
          </Button>
        ))}
      </nav>

      {/* Loading / error states */}
      {queue.isLoading ? <p className="text-dim">Loading…</p> : null}
      {queue.isError ? (
        <p className="auth-error">{getApiError(queue.error)}</p>
      ) : null}

      {/* Empty state */}
      {queue.data?.reviews.length === 0 ? (
        <p className="text-dim">No reviews to moderate.</p>
      ) : null}

      {/* Mutation error (shown once at the top) */}
      {moderate.isError ? (
        <p className="auth-error">{getApiError(moderate.error)}</p>
      ) : null}

      {/* Queue list */}
      <div className="review-moderation-list">
        {queue.data?.reviews.map((review) => {
          const isBusy = moderate.isPending && moderatingId === review.id;

          return (
            <article key={review.id} className="review-moderation-card">
              <div className="review-moderation-card-header">
                <span className="review-moderation-card-stars" aria-label={`${review.rating} out of 5 stars`}>
                  {renderStars(review.rating)}
                </span>
                <Chip
                  status={review.moderationStatus}
                  variant={
                    review.moderationStatus === 'Approved'
                      ? 'good'
                      : review.moderationStatus === 'Pending'
                        ? 'warn'
                        : 'danger'
                  }
                />
              </div>

              <h3 className="review-moderation-card-title">{review.title}</h3>
              <p className="review-moderation-card-body">{review.body}</p>

              <div className="review-moderation-card-meta">
                <span>
                  <strong>Patient:</strong> {review.patientName}
                </span>
                <span>
                  <strong>Doctor:</strong> {review.doctorName}
                </span>
                <span className="review-moderation-card-date">
                  {new Date(review.createdAt).toLocaleDateString()}
                </span>
              </div>

              {review.tags.length > 0 ? (
                <div className="review-moderation-card-tags">
                  {review.tags.map((tag) => (
                    <Chip key={tag} status={tag} />
                  ))}
                </div>
              ) : null}

              {/* Action buttons */}
              <div className="review-moderation-card-actions">
                <Button
                  variant="primary"
                  size="sm"
                  disabled={isBusy}
                  onClick={() => handleModerate(review.id, 'Approved')}
                >
                  {isBusy ? 'Processing…' : 'Approve'}
                </Button>
                <Button
                  size="sm"
                  disabled={isBusy}
                  onClick={() => handleModerate(review.id, 'Hidden')}
                >
                  Hide
                </Button>
                <Button
                  variant="danger"
                  size="sm"
                  disabled={isBusy}
                  onClick={() => handleModerate(review.id, 'Removed')}
                >
                  Remove
                </Button>
              </div>
            </article>
          );
        })}
      </div>

      {/* Pagination */}
      {totalPages > 1 ? (
        <nav className="review-moderation-pagination" aria-label="Moderation pagination">
          <Button
            size="sm"
            disabled={page <= 1}
            onClick={() => setPage((p) => p - 1)}
          >
            Previous
          </Button>
          <span className="text-dim">
            Page {page} of {totalPages}
          </span>
          <Button
            size="sm"
            disabled={page >= totalPages}
            onClick={() => setPage((p) => p + 1)}
          >
            Next
          </Button>
        </nav>
      ) : null}
    </section>
  );
}

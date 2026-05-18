import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { getApiError } from '../../shared/api/errors';
import { getMyReviews, respondToReview } from '../../shared/api/reviews';
import { Button, Chip } from '../../shared/components';

type ReviewFilter = 'all' | 'awaiting-reply' | 'low-star';

const FILTER_LABELS: { value: ReviewFilter; label: string }[] = [
  { value: 'all', label: 'All' },
  { value: 'awaiting-reply', label: 'Awaiting reply' },
  { value: 'low-star', label: 'Low star' },
];

function renderStars(rating: number): string {
  return '★'.repeat(rating) + '☆'.repeat(5 - rating);
}

export default function DoctorMyReviews() {
  const queryClient = useQueryClient();

  const [page, setPage] = useState(1);
  const [filter, setFilter] = useState<ReviewFilter>('all');
  const [replyingTo, setReplyingTo] = useState<string | null>(null);
  const [replyText, setReplyText] = useState('');

  const reviews = useQuery({
    queryKey: ['my-reviews', page, filter],
    queryFn: () => getMyReviews({ page, pageSize: 10, filter }),
  });

  const reply = useMutation({
    mutationFn: () => respondToReview(replyingTo!, replyText),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['my-reviews'] });
      setReplyingTo(null);
      setReplyText('');
    },
  });

  const totalPages = reviews.data ? Math.ceil(reviews.data.totalCount / 10) : 0;

  function handleFilterChange(next: ReviewFilter) {
    setFilter(next);
    setPage(1);
  }

  function handleStartReply(reviewId: string) {
    setReplyingTo(reviewId);
    setReplyText('');
    reply.reset();
  }

  function handleCancelReply() {
    setReplyingTo(null);
    setReplyText('');
    reply.reset();
  }

  return (
    <section className="doctor-my-reviews">
      <h1 className="auth-heading">
        My<br /><em>reviews.</em>
      </h1>

      {/* Summary row */}
      {reviews.data ? (
        <div className="doctor-my-reviews-summary">
          <span className="doctor-my-reviews-summary-score">
            {reviews.data.averageRating != null
              ? reviews.data.averageRating.toFixed(1)
              : '—'}
          </span>
          <span className="doctor-my-reviews-summary-count">
            {reviews.data.reviewCount}{' '}
            {reviews.data.reviewCount === 1 ? 'review' : 'reviews'}
          </span>
        </div>
      ) : null}

      {/* Filter tabs */}
      <nav className="doctor-my-reviews-filters" aria-label="Review filters">
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
      {reviews.isLoading ? <p className="text-dim">Loading…</p> : null}
      {reviews.isError ? (
        <p className="auth-error">{getApiError(reviews.error)}</p>
      ) : null}

      {/* Empty state */}
      {reviews.data?.reviews.length === 0 ? (
        <p className="text-dim">No reviews match this filter.</p>
      ) : null}

      {/* Review list */}
      <div className="doctor-my-reviews-list">
        {reviews.data?.reviews.map((review) => (
          <article key={review.id} className="doctor-my-reviews-card">
            <div className="doctor-my-reviews-card-header">
              <span className="doctor-my-reviews-card-stars" aria-label={`${review.rating} out of 5 stars`}>
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

            <h3 className="doctor-my-reviews-card-title">{review.title}</h3>
            <p className="doctor-my-reviews-card-body">{review.body}</p>

            <div className="doctor-my-reviews-card-meta">
              <span className="doctor-my-reviews-card-author">{review.postedAs}</span>
              <span className="doctor-my-reviews-card-date">
                {new Date(review.createdAt).toLocaleDateString()}
              </span>
            </div>

            {review.tags.length > 0 ? (
              <div className="doctor-my-reviews-card-tags">
                {review.tags.map((tag) => (
                  <Chip key={tag} status={tag} />
                ))}
              </div>
            ) : null}

            {/* Existing doctor response */}
            {review.doctorResponse ? (
              <div className="doctor-my-reviews-card-response">
                <strong>Your reply</strong>
                <p>{review.doctorResponse}</p>
                {review.doctorRespondedAt ? (
                  <span className="text-dim">
                    {new Date(review.doctorRespondedAt).toLocaleDateString()}
                  </span>
                ) : null}
              </div>
            ) : null}

            {/* Reply button (only if no existing response and not already composing) */}
            {!review.doctorResponse && replyingTo !== review.id ? (
              <Button
                size="sm"
                onClick={() => handleStartReply(review.id)}
              >
                Reply
              </Button>
            ) : null}

            {/* Reply composer */}
            {replyingTo === review.id ? (
              <div className="doctor-my-reviews-reply-composer">
                <textarea
                  className="doctor-my-reviews-reply-textarea"
                  name="doctor-reply"
                  aria-label="Reply to review"
                  rows={4}
                  maxLength={600}
                  value={replyText}
                  onChange={(e) => setReplyText(e.target.value)}
                  placeholder="Write your reply…"
                />
                <span className="doctor-my-reviews-reply-counter">
                  {replyText.length}/600
                </span>

                {reply.isError ? (
                  <p className="auth-error">{getApiError(reply.error)}</p>
                ) : null}

                <div className="doctor-my-reviews-reply-actions">
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={handleCancelReply}
                    disabled={reply.isPending}
                  >
                    Cancel
                  </Button>
                  <Button
                    variant="primary"
                    size="sm"
                    disabled={replyText.length < 3 || reply.isPending}
                    onClick={() => reply.mutate()}
                  >
                    {reply.isPending ? 'Posting…' : 'Post reply'}
                  </Button>
                </div>
              </div>
            ) : null}
          </article>
        ))}
      </div>

      {/* Pagination */}
      {totalPages > 1 ? (
        <nav className="doctor-my-reviews-pagination" aria-label="Review pagination">
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

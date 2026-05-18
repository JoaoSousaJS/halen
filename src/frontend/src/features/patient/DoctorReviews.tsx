import { useState } from 'react';
import { useQuery, useMutation, useQueryClient, keepPreviousData } from '@tanstack/react-query';
import { getDoctorReviews, voteHelpful } from '../../shared/api/reviews';
import type { ReviewDto } from '../../shared/api/reviews';
import { Button, Chip, Select } from '../../shared/components';
import { renderStars } from '../../shared/utils/renderStars';

interface DoctorReviewsProps {
  doctorProfileId: string;
}

const SORT_OPTIONS = [
  { value: 'newest', label: 'Newest' },
  { value: 'highest', label: 'Highest rated' },
  { value: 'lowest', label: 'Lowest rated' },
  { value: 'helpful', label: 'Most helpful' },
];

const PAGE_SIZE = 10;

export default function DoctorReviews({ doctorProfileId }: DoctorReviewsProps) {
  const [page, setPage] = useState(1);
  const [sortBy, setSortBy] = useState('newest');

  const queryClient = useQueryClient();

  const { data, isLoading, isError } = useQuery({
    queryKey: ['doctor-reviews', doctorProfileId, page, sortBy],
    queryFn: () => getDoctorReviews(doctorProfileId, { page, pageSize: PAGE_SIZE, sortBy }),
    placeholderData: keepPreviousData,
  });

  const helpfulMutation = useMutation({
    mutationFn: (reviewId: string) => voteHelpful(reviewId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['doctor-reviews', doctorProfileId] });
    },
  });

  if (isLoading && !data) {
    return <p role="status">Loading reviews…</p>;
  }

  if (isError) {
    return <p className="doctor-reviews-error" role="alert">Failed to load reviews. Please try again.</p>;
  }

  if (!data || data.reviewCount === 0) {
    return <p className="doctor-reviews-empty" role="status">No reviews yet</p>;
  }

  const totalPages = Math.ceil(data.totalCount / PAGE_SIZE);

  return (
    <section className="doctor-reviews" aria-label="Doctor reviews">
      {/* Aggregate score card */}
      <div className="doctor-reviews-aggregate">
        <span className="doctor-reviews-score">
          {data.averageRating != null ? data.averageRating.toFixed(1) : '—'}
        </span>
        <span className="doctor-reviews-score-stars" aria-hidden="true">
          {data.averageRating != null ? renderStars(Math.round(data.averageRating)) : '☆☆☆☆☆'}
        </span>
        <span className="doctor-reviews-count">
          {data.reviewCount} {data.reviewCount === 1 ? 'review' : 'reviews'}
        </span>
      </div>

      {/* Rating breakdown */}
      <div className="doctor-reviews-breakdown" aria-label="Rating breakdown">
        {[5, 4, 3, 2, 1].map((stars) => {
          const entry = data.ratingBreakdown.find((b) => b.stars === stars);
          const count = entry?.count ?? 0;
          const percentage = data.reviewCount > 0 ? (count / data.reviewCount) * 100 : 0;
          return (
            <div key={stars} className="doctor-reviews-breakdown-row">
              <span className="doctor-reviews-breakdown-label">{stars} star{stars > 1 ? 's' : ''}</span>
              <div className="doctor-reviews-breakdown-bar">
                <div
                  className="doctor-reviews-breakdown-fill"
                  style={{ width: `${percentage}%` }}
                  aria-label={`${count} reviews with ${stars} stars`}
                />
              </div>
              <span className="doctor-reviews-breakdown-count">{count}</span>
            </div>
          );
        })}
      </div>

      {/* Tag cloud */}
      {data.topTags.length > 0 && (
        <div className="doctor-reviews-tags">
          <h3 className="doctor-reviews-tags-heading">What patients praise</h3>
          <div className="doctor-reviews-tag-list">
            {data.topTags.map((tagEntry) => (
              <Chip key={tagEntry.tag} status={`${tagEntry.tag} (${tagEntry.count})`} />
            ))}
          </div>
        </div>
      )}

      {/* Sort selector */}
      <div className="doctor-reviews-sort">
        <Select
          options={SORT_OPTIONS}
          value={sortBy}
          aria-label="Sort reviews"
          onChange={(e) => {
            setSortBy(e.target.value);
            setPage(1);
          }}
        />
      </div>

      {/* Review list */}
      <ul className="doctor-reviews-list">
        {data.reviews.map((review: ReviewDto) => (
          <li key={review.id} className="doctor-reviews-item">
            <header className="doctor-reviews-item-header">
              <span className="doctor-reviews-item-author">{review.postedAs}</span>
              <span className="doctor-reviews-item-stars" aria-label={`${review.rating} out of 5 stars`}>
                {renderStars(review.rating)}
              </span>
              <time className="doctor-reviews-item-date" dateTime={review.createdAt}>
                {new Date(review.createdAt).toLocaleDateString()}
              </time>
            </header>

            <h4 className="doctor-reviews-item-title">{review.title}</h4>
            {review.body && <p className="doctor-reviews-item-body">{review.body}</p>}

            {review.tags.length > 0 && (
              <div className="doctor-reviews-item-tags">
                {review.tags.map((tag) => (
                  <Chip key={tag} status={tag} />
                ))}
              </div>
            )}

            <Button
              variant="ghost"
              size="sm"
              onClick={() => helpfulMutation.mutate(review.id)}
              disabled={helpfulMutation.isPending}
              ariaLabel={`Mark review as helpful, currently ${review.helpfulCount} votes`}
            >
              Helpful ({review.helpfulCount})
            </Button>

            {review.doctorResponse && (
              <div className="doctor-reviews-response">
                <span className="doctor-reviews-response-label">Doctor's response</span>
                <p className="doctor-reviews-response-text">{review.doctorResponse}</p>
                {review.doctorRespondedAt && (
                  <time className="doctor-reviews-response-date" dateTime={review.doctorRespondedAt}>
                    {new Date(review.doctorRespondedAt).toLocaleDateString()}
                  </time>
                )}
              </div>
            )}
          </li>
        ))}
      </ul>

      {/* Pagination */}
      {totalPages > 1 && (
        <nav className="doctor-reviews-pagination" aria-label="Review pages">
          <Button
            variant="ghost"
            disabled={page <= 1}
            onClick={() => setPage((p) => p - 1)}
            ariaLabel="Previous page"
          >
            Previous
          </Button>
          <span aria-live="polite" aria-atomic="true">
            Page {page} of {totalPages}
          </span>
          <Button
            variant="ghost"
            disabled={page >= totalPages}
            onClick={() => setPage((p) => p + 1)}
            ariaLabel="Next page"
          >
            Next
          </Button>
        </nav>
      )}
    </section>
  );
}

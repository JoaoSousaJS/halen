import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, keepPreviousData } from '@tanstack/react-query';
import { getDoctorProfile } from '../../shared/api/doctors';
import type { ProfileReviewDto } from '../../shared/api/doctors';
import { Button, Chip, Select } from '../../shared/components';
import { renderStars } from '../../shared/utils/renderStars';

const SORT_OPTIONS = [
  { value: 'newest', label: 'Newest' },
  { value: 'highest', label: 'Highest rated' },
  { value: 'lowest', label: 'Lowest rated' },
  { value: 'helpful', label: 'Most helpful' },
];

const PAGE_SIZE = 10;

export default function DoctorProfilePage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [reviewPage, setReviewPage] = useState(1);
  const [reviewSortBy, setReviewSortBy] = useState('newest');

  const { data, isLoading, isError } = useQuery({
    queryKey: ['doctor-profile', id, reviewPage, reviewSortBy],
    queryFn: () => getDoctorProfile(id!, { reviewPage, reviewPageSize: PAGE_SIZE, reviewSortBy }),
    enabled: !!id,
    placeholderData: keepPreviousData,
  });

  if (isLoading && !data) {
    return <p role="status">Loading profile…</p>;
  }

  if (isError || !data) {
    return <p role="alert">Failed to load doctor profile.</p>;
  }

  const { doctor, availability, reviewsSummary, reviews, reviewTotalCount } = data;
  const totalPages = Math.ceil(reviewTotalCount / PAGE_SIZE);

  return (
    <div className="doctor-profile-page">
      <header className="doctor-profile-header">
        <div className="doctor-profile-name-row">
          <h1>{doctor.name}</h1>
          <Chip status={doctor.specialty} />
        </div>

        <div className="doctor-profile-stats">
          {doctor.averageRating != null && (
            <span className="doctor-profile-rating">
              <span className="doctor-profile-rating-value">{doctor.averageRating.toFixed(1)}</span>
              <span aria-hidden="true">{renderStars(Math.round(doctor.averageRating))}</span>
              <span>{doctor.reviewCount} {doctor.reviewCount === 1 ? 'review' : 'reviews'}</span>
            </span>
          )}
          <span>${doctor.consultationFee}</span>
          <span>{doctor.yearsOfExperience} years experience</span>
        </div>

        <p className="doctor-profile-languages">{doctor.languages.join(', ')}</p>

        <Button
          variant="primary"
          ariaLabel="Book appointment"
          onClick={() => navigate('/dashboard')}
        >
          Book appointment
        </Button>
      </header>

      {availability.length > 0 && (
        <section className="doctor-profile-availability" aria-label="Availability">
          <h2>Availability</h2>
          <div className="doctor-profile-schedule">
            {availability.map((day) => (
              <div key={day.dayOfWeek} className="doctor-profile-day">
                <span className="doctor-profile-day-name">{day.dayOfWeek}</span>
                <div className="doctor-profile-windows">
                  {day.windows.map((w) => (
                    <span key={`${w.startTime}-${w.endTime}`} className="doctor-profile-window">
                      {w.startTime} – {w.endTime}
                    </span>
                  ))}
                </div>
              </div>
            ))}
          </div>
        </section>
      )}

      {reviewsSummary && (
        <section className="doctor-profile-reviews" aria-label="Reviews">
          <h2>Reviews</h2>

          <div className="doctor-profile-reviews-aggregate">
            <span className="doctor-profile-reviews-score">
              {reviewsSummary.averageRating != null
                ? reviewsSummary.averageRating.toFixed(1)
                : '—'}
            </span>
            <span aria-hidden="true">
              {reviewsSummary.averageRating != null
                ? renderStars(Math.round(reviewsSummary.averageRating))
                : '☆☆☆☆☆'}
            </span>
            <span>{reviewsSummary.totalCount} {reviewsSummary.totalCount === 1 ? 'review' : 'reviews'}</span>
          </div>

          <div className="doctor-profile-reviews-breakdown" aria-label="Rating breakdown">
            {[5, 4, 3, 2, 1].map((stars) => {
              const entry = reviewsSummary.ratingBreakdown.find((b) => b.stars === stars);
              const count = entry?.count ?? 0;
              const pct = reviewsSummary.totalCount > 0 ? (count / reviewsSummary.totalCount) * 100 : 0;
              return (
                <div key={stars} className="doctor-profile-breakdown-row">
                  <span>{stars} star{stars > 1 ? 's' : ''}</span>
                  <div className="doctor-profile-breakdown-bar">
                    <div
                      className="doctor-profile-breakdown-fill"
                      style={{ width: `${pct}%` }}
                    />
                  </div>
                  <span>{count}</span>
                </div>
              );
            })}
          </div>

          {reviewsSummary.topTags.length > 0 && (
            <div className="doctor-profile-tags">
              {reviewsSummary.topTags.map((t) => (
                <Chip key={t.tag} status={`${t.tag} (${t.count})`} />
              ))}
            </div>
          )}

          <div className="doctor-profile-reviews-sort">
            <Select
              options={SORT_OPTIONS}
              value={reviewSortBy}
              aria-label="Sort reviews"
              onChange={(e) => {
                setReviewSortBy(e.target.value);
                setReviewPage(1);
              }}
            />
          </div>

          <ul className="doctor-profile-review-list">
            {reviews.map((review: ProfileReviewDto) => (
              <li key={review.id} className="doctor-profile-review-item">
                <header className="doctor-profile-review-header">
                  <span>{review.postedAs}</span>
                  <span aria-label={`${review.rating} out of 5 stars`}>
                    {renderStars(review.rating)}
                  </span>
                  <time dateTime={review.createdAt}>
                    {new Date(review.createdAt).toLocaleDateString()}
                  </time>
                </header>
                <h4>{review.title}</h4>
                {review.body && <p>{review.body}</p>}
                {review.tags.length > 0 && (
                  <div className="doctor-profile-review-tags">
                    {review.tags.map((tag) => (
                      <Chip key={tag} status={tag} />
                    ))}
                  </div>
                )}
                {review.doctorResponse && (
                  <div className="doctor-profile-doctor-response">
                    <span className="doctor-profile-response-label">Doctor's response</span>
                    <p>{review.doctorResponse}</p>
                  </div>
                )}
              </li>
            ))}
          </ul>

          {totalPages > 1 && (
            <nav className="doctor-profile-pagination" aria-label="Review pages">
              <Button
                variant="ghost"
                disabled={reviewPage <= 1}
                onClick={() => setReviewPage((p) => p - 1)}
                ariaLabel="Previous page"
              >
                Previous
              </Button>
              <span aria-live="polite" aria-atomic="true">
                Page {reviewPage} of {totalPages}
              </span>
              <Button
                variant="ghost"
                disabled={reviewPage >= totalPages}
                onClick={() => setReviewPage((p) => p + 1)}
                ariaLabel="Next page"
              >
                Next
              </Button>
            </nav>
          )}
        </section>
      )}
    </div>
  );
}

import { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { submitReview } from '../../shared/api/reviews';
import type { SubmitReviewPayload } from '../../shared/api/reviews';
import { getApiError } from '../../shared/api/errors';
import { Button, Chip, Field, Input } from '../../shared/components';

interface ReviewFormProps {
  appointmentId: string;
  patientFirstName: string;
  patientLastInitial: string;
  onClose: () => void;
  onSuccess: () => void;
}

const ALLOWED_TAGS = [
  'clear explanations',
  'listens',
  'calm bedside manner',
  'thorough',
  'sends follow-up notes',
  'on time',
  'wait times',
  'booking flexibility',
] as const;

const MAX_TAGS = 6;
const TITLE_MAX = 120;
const BODY_MAX = 600;

const RATING_LABELS: Record<number, string> = {
  1: '1 star',
  2: '2 stars',
  3: '3 stars',
  4: '4 stars',
  5: '5 stars',
};

export default function ReviewForm({
  appointmentId,
  patientFirstName,
  patientLastInitial,
  onClose,
  onSuccess,
}: ReviewFormProps) {
  const [rating, setRating] = useState(0);
  const [title, setTitle] = useState('');
  const [body, setBody] = useState('');
  const [selectedTags, setSelectedTags] = useState<string[]>([]);

  const queryClient = useQueryClient();

  const mutation = useMutation({
    mutationFn: (payload: SubmitReviewPayload) => submitReview(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['doctor-reviews'] });
      onSuccess();
    },
  });

  const canSubmit = rating > 0 && title.length >= 3 && !mutation.isPending;

  function handleToggleTag(tag: string) {
    setSelectedTags((prev) => {
      if (prev.includes(tag)) {
        return prev.filter((t) => t !== tag);
      }
      if (prev.length >= MAX_TAGS) {
        return prev;
      }
      return [...prev, tag];
    });
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!canSubmit) return;

    mutation.mutate({
      appointmentId,
      rating,
      title: title.trim(),
      body: body.trim(),
      tags: selectedTags,
    });
  }

  return (
    <form className="review-form" onSubmit={handleSubmit} aria-label="Write a review">
      <fieldset className="review-form-stars" role="radiogroup" aria-label="Rating">
        <legend className="review-form-legend">How was your visit?</legend>
        <div className="review-form-star-buttons">
          {[1, 2, 3, 4, 5].map((star) => (
            <button
              key={star}
              type="button"
              className={`review-form-star ${star <= rating ? 'review-form-star-filled' : ''}`}
              aria-label={`${star} star${star > 1 ? 's' : ''}`}
              aria-pressed={star <= rating}
              onClick={() => setRating(star)}
            >
              {star <= rating ? '★' : '☆'}
            </button>
          ))}
        </div>
        {rating > 0 && (
          <span className="review-form-rating-text" aria-live="polite">
            {RATING_LABELS[rating]}
          </span>
        )}
      </fieldset>

      <Field label="Title">
        <Input
          value={title}
          onChange={(e) => setTitle(e.target.value.slice(0, TITLE_MAX))}
          name="review-title"
          autoComplete="off"
          placeholder="Summarize your experience…"
          maxLength={TITLE_MAX}
          required
          aria-describedby="review-form-title-counter"
        />
        <span id="review-form-title-counter" className="review-form-counter">
          {title.length}/{TITLE_MAX}
        </span>
      </Field>

      <fieldset className="review-form-tags">
        <legend className="review-form-legend">What stood out? (up to {MAX_TAGS})</legend>
        <div className="review-form-tag-list">
          {ALLOWED_TAGS.map((tag) => {
            const isSelected = selectedTags.includes(tag);
            return (
              <button
                key={tag}
                type="button"
                role="checkbox"
                aria-checked={isSelected}
                className={`review-form-tag ${isSelected ? 'review-form-tag-selected' : ''}`}
                onClick={() => handleToggleTag(tag)}
                disabled={!isSelected && selectedTags.length >= MAX_TAGS}
              >
                <Chip status={tag} />
              </button>
            );
          })}
        </div>
      </fieldset>

      <Field label="Details (optional)">
        <textarea
          className="review-form-body"
          value={body}
          onChange={(e) => setBody(e.target.value.slice(0, BODY_MAX))}
          name="review-body"
          placeholder="Tell others about your experience…"
          maxLength={BODY_MAX}
          rows={4}
          aria-describedby="review-form-body-counter"
        />
        <span id="review-form-body-counter" className="review-form-counter">
          {body.length}/{BODY_MAX}
        </span>
      </Field>

      <p className="review-form-privacy">
        Posted as {patientFirstName} {patientLastInitial}.
      </p>

      {mutation.isError && (
        <p className="review-form-error" role="alert">
          {getApiError(mutation.error)}
        </p>
      )}

      <div className="review-form-actions">
        <Button type="button" variant="ghost" onClick={onClose} disabled={mutation.isPending}>
          Skip for now
        </Button>
        <Button type="submit" variant="primary" disabled={!canSubmit}>
          {mutation.isPending ? 'Posting…' : 'Post review'}
        </Button>
      </div>
    </form>
  );
}

# Doctor Reviews — Design Spec

**Date:** 2026-05-18
**Status:** Approved
**Design source:** Halen Design System bundle (`/project/reviews/`)

## Overview

Patients rate doctors after completed consultations. Doctors can reply publicly. Admins moderate. Ratings surface on doctor profiles and search results. Gated behind feature flag `doctor_reviews`.

## Domain model

### New entity: `Review`

```
Review : BaseEntity, ITenantScoped
  - ClinicId            : Guid (FK → Clinic, tenant discriminator)
  - AppointmentId       : Guid (FK → Appointment, unique — one review per appointment)
  - PatientProfileId    : Guid (FK → PatientProfile)
  - DoctorProfileId     : Guid (FK → DoctorProfile)
  - Rating              : int (1–5)
  - Title               : string (max 120 chars)
  - Body                : string (max 600 chars)
  - Tags                : string[] (aspect chips: "clear explanations", "listens", etc.)
  - IsVerified          : bool (true when appointment was completed)
  - HelpfulCount        : int (default 0)
  - ModerationStatus    : ReviewModerationStatus enum
  - DoctorResponse      : string? (max 600 chars)
  - DoctorRespondedAt   : DateTime?
  - PostedAs            : string (anonymized: "Maya C." — first name + last initial)
```

### New enum: `ReviewModerationStatus`

```
Pending     — awaiting auto/manual moderation
Approved    — visible to public
Hidden      — hidden by admin (soft)
Removed     — removed by admin (hard)
```

### Modified entity: `DoctorProfile`

Add denormalized aggregate fields (updated on review submit/moderate):

```
AverageRating   : decimal? (null until first review)
ReviewCount     : int (default 0)
```

### EF Core configuration

- `Review` has query filter for tenant isolation (`ClinicId == TenantClinicId`)
- Unique index on `AppointmentId` (one review per appointment)
- Composite index on `(ClinicId, DoctorProfileId, ModerationStatus)` for public queries
- `Tags` stored as JSON column (`HasConversion` with `JsonSerializer`)
- `ModerationStatus` stored as string (`HasConversion<string>()`)
- `Rating` has check constraint (1–5)

## CQRS commands

### SubmitReview

- **Who:** Patient (appointment owner)
- **Preconditions:** Appointment status is `Completed`, no existing review for this appointment
- **Input:** AppointmentId, Rating, Title, Body, Tags[]
- **Behavior:**
  1. Validate appointment exists, belongs to patient, is completed
  2. Check no duplicate review
  3. Create `Review` with `IsVerified = true`, `ModerationStatus = Approved` (auto-approve for now; auto-flag deferred)
  4. Set `PostedAs` from patient's first name + last initial
  5. Recalculate `DoctorProfile.AverageRating` and `ReviewCount`
  6. Publish `review.submitted` event
  7. If rating <= 2, also publish `review.low_star` event
- **Returns:** `SubmitReviewResult(Success, ReviewId?, Error?, Kind?)`

### RespondToReview

- **Who:** Doctor (review's doctor)
- **Input:** ReviewId, Response (max 600 chars)
- **Preconditions:** Review exists, belongs to doctor, no existing response
- **Behavior:** Set `DoctorResponse` and `DoctorRespondedAt`
- **Returns:** `RespondToReviewResult(Success, Error?, Kind?)`

### ModerateReview

- **Who:** Admin (PlatformAdmin or ClinicAdmin)
- **Input:** ReviewId, Decision (Approve/Hide/Remove)
- **Behavior:** Update `ModerationStatus`
- **Returns:** `ModerateReviewResult(Success, Error?, Kind?)`

### VoteHelpful

- **Who:** Any authenticated user
- **Input:** ReviewId
- **Behavior:** Increment `HelpfulCount` (simple counter, no duplicate tracking in v1)
- **Returns:** `VoteHelpfulResult(Success, NewCount?, Error?, Kind?)`

## CQRS queries

### GetDoctorReviews

- **Who:** Any authenticated user
- **Input:** DoctorProfileId, Page, PageSize, SortBy (newest/highest/lowest/helpful)
- **Filter:** Only `ModerationStatus == Approved`
- **Returns:** `DoctorReviewsResult` with:
  - Reviews list (with doctor response if present)
  - Aggregate: AverageRating, ReviewCount, RatingBreakdown (count per star), TopTags (tag + count)

### GetMyReviews (Doctor)

- **Who:** Doctor
- **Input:** Page, PageSize, Filter (all/awaiting-reply/low-star)
- **Returns:** Reviews for this doctor (all moderation statuses except Removed)

### GetModerationQueue (Admin)

- **Who:** PlatformAdmin, ClinicAdmin
- **Input:** Page, PageSize, Filter (pending/all)
- **Returns:** Reviews needing moderation, with patient and doctor context

## Validation rules

- `Rating`: required, 1–5
- `Title`: required, 3–120 chars
- `Body`: optional, max 600 chars
- `Tags`: optional, max 6 tags, each from predefined set
- `DoctorResponse`: required for RespondToReview, 3–600 chars

### Predefined aspect tags

```
"clear explanations", "listens", "calm bedside manner",
"thorough", "sends follow-up notes", "on time",
"wait times", "booking flexibility"
```

## Events

### review.submitted

```csharp
record ReviewSubmittedEvent(
    Guid ReviewId, Guid DoctorUserId, Guid PatientUserId,
    int Rating, string PatientName, string DoctorName);
```

Notification to doctor: "New {Rating}-star review from {PatientName}"

### review.low_star

```csharp
record ReviewLowStarEvent(
    Guid ReviewId, Guid DoctorUserId, Guid DoctorProfileId,
    int Rating, string PatientName, string DoctorName);
```

Notification to admins: "⚠ {Rating}-star review for Dr. {DoctorName}"

## API endpoints

```
POST   /api/v1/reviews                     — SubmitReview (Patient)
GET    /api/v1/reviews/doctor/{id}          — GetDoctorReviews (any auth'd user)
POST   /api/v1/reviews/{id}/respond         — RespondToReview (Doctor)
POST   /api/v1/reviews/{id}/helpful         — VoteHelpful (any auth'd user)
GET    /api/v1/doctor/reviews               — GetMyReviews (Doctor)
GET    /api/v1/admin/reviews/moderation     — GetModerationQueue (Admin)
POST   /api/v1/admin/reviews/{id}/moderate  — ModerateReview (Admin)
```

All gated behind `[RequireFeature("doctor_reviews")]`.

## Frontend components

### Patient: Post-appointment rating form

- Shown after consultation ends (link from post-call wrap-up or dashboard)
- 5-star selector (large, interactive)
- Aspect tag chips (toggleable, from predefined set)
- Optional comment textarea (600 char counter)
- Privacy notice: "Posted as {FirstName} {LastInitial}"
- Buttons: "Skip for now" (ghost) / "Post review" (primary)
- Location: `src/frontend/src/features/patient/ReviewForm.tsx`

### Public: Doctor profile reviews section

- Aggregate score card (large rating number + stars + review count)
- Rating breakdown bars (5→1 star distribution)
- "What patients praise" tag cloud (positive tags with counts)
- Paginated review list with sort options
- Each review: avatar, name, rating, title, body, tags, helpful count, doctor response
- Location: `src/frontend/src/features/patient/DoctorReviews.tsx`

### Doctor: My Reviews page

- Summary row: aggregate score, distribution, top tags
- Filter tabs: All / Awaiting reply / Low star
- Review list with reply action
- Reply composer: suggested openers, textarea, char counter, post button
- Location: `src/frontend/src/features/doctor/DoctorMyReviews.tsx`

### Admin: Moderation queue

- Queue list with patient info, rating, review text, flag reason
- Action buttons: Approve / Hide / Remove
- Location: `src/frontend/src/features/admin/ReviewModeration.tsx`

### DoctorCard enrichment

- Add star rating display + review count to existing `DoctorCard.tsx`
- Add "Top rated" chip if rating >= 4.7 and reviewCount >= 50
- Add "Highest rated" sort option to doctor search

## Testing strategy

- **Unit tests (TDD):** Handler tests for all 4 commands + 3 queries, validator tests
- **Integration tests:** Controller tests for all 7 endpoints
- **Frontend tests:** Vitest + Testing Library for all new components
- **Storybook:** Stories for ReviewForm, DoctorReviews, DoctorMyReviews, ReviewModeration
- **Playwright e2e:** Patient submits review, doctor replies, admin moderates

## Feature flag

Feature key: `doctor_reviews`

- Backend: `[RequireFeature("doctor_reviews")]` on ReviewsController
- Frontend: `FeatureGate` wrapper + `useFeatureFlags().hasFeature("doctor_reviews")`
- Seeded as enabled for the default clinic in dev

## Deferred to future iteration

- Multi-channel review nudges (email/push/SMS reminders)
- Low-star alert admin screen with consult context + suggested remediation
- Review search with highlighted results
- Auto-flagging (profanity, links, billing keywords)
- "Ban author" moderation action
- Mobile-specific layouts
- Attachment support in reviews

using Halen.Application.Interfaces;
using Halen.Application.Reviews.Queries;
using Halen.Domain.Constants;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Halen.Application.Doctors.Queries;

public class GetDoctorProfileQueryHandler(IAppDbContext db)
    : IRequestHandler<GetDoctorProfileQuery, GetDoctorProfileResult>
{
    private static readonly Dictionary<DayOfWeek, int> DayOrder = new()
    {
        [System.DayOfWeek.Monday] = 0,
        [System.DayOfWeek.Tuesday] = 1,
        [System.DayOfWeek.Wednesday] = 2,
        [System.DayOfWeek.Thursday] = 3,
        [System.DayOfWeek.Friday] = 4,
        [System.DayOfWeek.Saturday] = 5,
        [System.DayOfWeek.Sunday] = 6,
    };

    public async Task<GetDoctorProfileResult> Handle(GetDoctorProfileQuery request, CancellationToken ct)
    {
        var raw = await db.DoctorProfiles.AsNoTracking()
            .Where(d => d.Id == request.DoctorProfileId
                && d.KycStatus == KycStatus.Approved
                && d.User.Status == AccountStatus.Active)
            .Select(d => new
            {
                d.Id,
                Name = d.User.FirstName + " " + d.User.LastName,
                d.Specialty,
                d.ConsultationFee,
                d.YearsOfExperience,
                d.Languages,
                d.AverageRating,
                d.ReviewCount,
            })
            .FirstOrDefaultAsync(ct);

        var doctor = raw is null ? null : new DoctorProfileDto(
            raw.Id, raw.Name, raw.Specialty, raw.ConsultationFee,
            raw.YearsOfExperience, raw.Languages, raw.AverageRating, raw.ReviewCount);

        if (doctor is null)
            return GetDoctorProfileResult.NotFound("Doctor profile not found.");

        var availability = await BuildAvailability(request.DoctorProfileId, ct);
        var (reviewsSummary, reviews, reviewTotalCount) = await BuildReviews(request, ct);

        return GetDoctorProfileResult.Ok(doctor, availability, reviewsSummary, reviews, reviewTotalCount);
    }

    private async Task<IReadOnlyList<AvailabilityDayDto>> BuildAvailability(Guid doctorProfileId, CancellationToken ct)
    {
        var windows = await db.DoctorAvailabilities.AsNoTracking()
            .Where(a => a.DoctorProfileId == doctorProfileId && a.IsActive)
            .ToListAsync(ct);

        return windows
            .GroupBy(w => w.DayOfWeek)
            .OrderBy(g => DayOrder.GetValueOrDefault(g.Key, 7))
            .Select(g => new AvailabilityDayDto(
                g.Key.ToString(),
                g.OrderBy(w => w.StartTime)
                    .Select(w => new TimeWindowDto(
                        w.StartTime.ToString("HH:mm"),
                        w.EndTime.ToString("HH:mm"),
                        w.SlotDurationMinutes))
                    .ToList()))
            .ToList();
    }

    private async Task<(ReviewsSummaryDto?, IReadOnlyList<ReviewDto>, int)> BuildReviews(
        GetDoctorProfileQuery request, CancellationToken ct)
    {
        var reviewsEnabled = await db.ClinicFeatureFlags.AsNoTracking()
            .Where(f => f.ClinicId == db.DoctorProfiles.AsNoTracking()
                .Where(d => d.Id == request.DoctorProfileId)
                .Select(d => d.ClinicId)
                .FirstOrDefault())
            .Where(f => f.FeatureKey == FeatureKeys.DoctorReviews && f.IsEnabled)
            .AnyAsync(ct);

        if (!reviewsEnabled)
            return (null, [], 0);

        var baseQuery = db.Reviews.AsNoTracking()
            .Where(r => r.DoctorProfileId == request.DoctorProfileId
                && r.ModerationStatus == ReviewModerationStatus.Approved);

        var totalCount = await baseQuery.CountAsync(ct);

        var ratingGroups = await baseQuery
            .GroupBy(r => r.Rating)
            .Select(g => new RatingBreakdownDto(g.Key, g.Count()))
            .ToListAsync(ct);

        var ratingBreakdown = Enumerable.Range(1, 5)
            .Select(star => ratingGroups.FirstOrDefault(r => r.Stars == star) ?? new RatingBreakdownDto(star, 0))
            .ToList();

        var allTags = await baseQuery
            .Select(r => r.Tags)
            .ToListAsync(ct);

        var topTags = allTags
            .SelectMany(tags => tags)
            .GroupBy(tag => tag)
            .Select(g => new TagCountDto(g.Key, g.Count()))
            .OrderByDescending(t => t.Count)
            .Take(8)
            .ToList();

        var doctorProfile = await db.DoctorProfiles.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == request.DoctorProfileId, ct);

        IQueryable<Domain.Entities.Review> sorted = request.ReviewSortBy switch
        {
            "highest" => baseQuery.OrderByDescending(r => r.Rating).ThenByDescending(r => r.CreatedAt),
            "lowest" => baseQuery.OrderBy(r => r.Rating).ThenByDescending(r => r.CreatedAt),
            "helpful" => baseQuery.OrderByDescending(r => r.HelpfulCount).ThenByDescending(r => r.CreatedAt),
            _ => baseQuery.OrderByDescending(r => r.CreatedAt),
        };

        var reviews = await sorted
            .Skip((request.ReviewPage - 1) * request.ReviewPageSize)
            .Take(request.ReviewPageSize)
            .Select(r => new ReviewDto(
                r.Id,
                r.Rating,
                r.Title,
                r.Body,
                r.Tags,
                r.PostedAs,
                r.HelpfulCount,
                r.DoctorResponse,
                r.DoctorRespondedAt,
                r.CreatedAt))
            .ToListAsync(ct);

        var summary = new ReviewsSummaryDto(
            doctorProfile?.AverageRating,
            totalCount,
            ratingBreakdown,
            topTags);

        return (summary, reviews, totalCount);
    }
}

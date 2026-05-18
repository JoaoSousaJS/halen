using Halen.Application.Interfaces;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Halen.Application.Reviews.Queries;

public class GetDoctorReviewsQueryHandler(
    IAppDbContext db
) : IRequestHandler<GetDoctorReviewsQuery, DoctorReviewsResult>
{
    public async Task<DoctorReviewsResult> Handle(GetDoctorReviewsQuery request, CancellationToken ct)
    {
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

        IQueryable<Domain.Entities.Review> sorted = request.SortBy switch
        {
            "highest" => baseQuery.OrderByDescending(r => r.Rating).ThenByDescending(r => r.CreatedAt),
            "lowest" => baseQuery.OrderBy(r => r.Rating).ThenByDescending(r => r.CreatedAt),
            "helpful" => baseQuery.OrderByDescending(r => r.HelpfulCount).ThenByDescending(r => r.CreatedAt),
            _ => baseQuery.OrderByDescending(r => r.CreatedAt),
        };

        var reviews = await sorted
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
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

        return new DoctorReviewsResult(
            reviews,
            totalCount,
            doctorProfile?.AverageRating,
            doctorProfile?.ReviewCount ?? 0,
            ratingBreakdown,
            topTags);
    }
}

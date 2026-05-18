using Halen.Application.Interfaces;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Halen.Application.Reviews.Queries;

public class GetMyReviewsQueryHandler(
    IAppDbContext db
) : IRequestHandler<GetMyReviewsQuery, GetMyReviewsResult>
{
    public async Task<GetMyReviewsResult> Handle(GetMyReviewsQuery request, CancellationToken ct)
    {
        var doctorProfile = await db.DoctorProfiles.AsNoTracking()
            .FirstOrDefaultAsync(d => d.UserId == request.DoctorUserId, ct);

        if (doctorProfile is null)
            return new GetMyReviewsResult([], 0, null, 0);

        var baseQuery = db.Reviews.AsNoTracking()
            .Where(r => r.DoctorProfileId == doctorProfile.Id
                && r.ModerationStatus != ReviewModerationStatus.Removed);

        baseQuery = request.Filter switch
        {
            "awaiting-reply" => baseQuery.Where(r => r.DoctorResponse == null),
            "low-star" => baseQuery.Where(r => r.Rating <= 2),
            _ => baseQuery,
        };

        var totalCount = await baseQuery.CountAsync(ct);

        var reviews = await baseQuery
            .OrderByDescending(r => r.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new DoctorReviewItemDto(
                r.Id,
                r.Rating,
                r.Title,
                r.Body,
                r.Tags,
                r.PostedAs,
                r.HelpfulCount,
                r.ModerationStatus.ToString(),
                r.DoctorResponse,
                r.DoctorRespondedAt,
                r.CreatedAt))
            .ToListAsync(ct);

        return new GetMyReviewsResult(
            reviews,
            totalCount,
            doctorProfile.AverageRating,
            doctorProfile.ReviewCount);
    }
}

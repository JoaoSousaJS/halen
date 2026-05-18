using Halen.Application.Interfaces;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Halen.Application.Reviews.Queries;

public class GetModerationQueueQueryHandler(
    IAppDbContext db
) : IRequestHandler<GetModerationQueueQuery, ModerationQueueResult>
{
    public async Task<ModerationQueueResult> Handle(GetModerationQueueQuery request, CancellationToken ct)
    {
        var baseQuery = db.Reviews.AsNoTracking()
            .Where(r => request.Filter == "pending"
                ? r.ModerationStatus == ReviewModerationStatus.Pending
                : r.ModerationStatus != ReviewModerationStatus.Removed);

        var totalCount = await baseQuery.CountAsync(ct);

        var entities = await baseQuery
            .Include(r => r.PatientProfile).ThenInclude(p => p.User)
            .Include(r => r.DoctorProfile).ThenInclude(d => d.User)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var reviews = entities.Select(r => new ModerationReviewDto(
            r.Id,
            r.Rating,
            r.Title,
            r.Body,
            r.Tags,
            r.PostedAs,
            r.ModerationStatus.ToString(),
            $"{r.PatientProfile.User.FirstName} {r.PatientProfile.User.LastName}",
            $"Dr. {r.DoctorProfile.User.LastName}",
            r.CreatedAt)).ToList();

        return new ModerationQueueResult(reviews, totalCount);
    }
}

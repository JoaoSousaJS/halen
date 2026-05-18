using Halen.Application.Common;
using Halen.Application.Interfaces;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Halen.Application.Reviews.Commands;

public class ModerateReviewCommandHandler(
    IAppDbContext db,
    ILogger<ModerateReviewCommandHandler> logger
) : IRequestHandler<ModerateReviewCommand, ModerateReviewResult>
{
    public async Task<ModerateReviewResult> Handle(ModerateReviewCommand request, CancellationToken ct)
    {
        var review = await db.Reviews
            .Include(r => r.DoctorProfile)
            .FirstOrDefaultAsync(r => r.Id == request.ReviewId, ct);

        if (review is null)
            return new ModerateReviewResult(false, Error: "Review not found.", Kind: ErrorKind.NotFound);

        if (request.Decision == ReviewModerationStatus.Pending)
            return new ModerateReviewResult(false, Error: "Cannot set moderation status back to Pending.", Kind: ErrorKind.Validation);

        var previousStatus = review.ModerationStatus;
        review.ModerationStatus = request.Decision;

        var crossesBoundary =
            (previousStatus == ReviewModerationStatus.Approved && request.Decision != ReviewModerationStatus.Approved) ||
            (previousStatus != ReviewModerationStatus.Approved && request.Decision == ReviewModerationStatus.Approved);

        if (crossesBoundary)
        {
            var stats = await db.Reviews
                .Where(r => r.DoctorProfileId == review.DoctorProfileId
                    && r.ModerationStatus == ReviewModerationStatus.Approved
                    && r.Id != review.Id)
                .GroupBy(_ => 1)
                .Select(g => new { Count = g.Count(), Sum = g.Sum(r => r.Rating) })
                .FirstOrDefaultAsync(ct);

            if (request.Decision == ReviewModerationStatus.Approved)
            {
                var newCount = (stats?.Count ?? 0) + 1;
                var newAvg = (decimal)((stats?.Sum ?? 0) + review.Rating) / newCount;
                review.DoctorProfile.AverageRating = Math.Round(newAvg, 2);
                review.DoctorProfile.ReviewCount = newCount;
            }
            else
            {
                var newCount = stats?.Count ?? 0;
                review.DoctorProfile.AverageRating = newCount > 0 ? Math.Round((decimal)stats!.Sum / newCount, 2) : null;
                review.DoctorProfile.ReviewCount = newCount;
            }
        }

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Admin {AdminUserId} moderated review {ReviewId}: {PreviousStatus} -> {Decision}",
            request.AdminUserId, request.ReviewId, previousStatus, request.Decision);

        return new ModerateReviewResult(true);
    }
}

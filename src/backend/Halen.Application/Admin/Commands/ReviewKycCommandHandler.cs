using Halen.Application.Common;
using Halen.Application.Events;
using Halen.Application.Interfaces;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Halen.Application.Admin.Commands;

public class ReviewKycCommandHandler(
    IAppDbContext db,
    ITenantContext tenantContext,
    IEventBus eventBus,
    ILogger<ReviewKycCommandHandler> logger
) : IRequestHandler<ReviewKycCommand, ReviewKycResult>
{
    public async Task<ReviewKycResult> Handle(ReviewKycCommand request, CancellationToken ct)
    {
        var doctor = await db.DoctorProfiles
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == request.DoctorProfileId, ct);

        if (doctor is null)
            return new ReviewKycResult(false, "Doctor profile not found.", ErrorKind.NotFound);

        if (doctor.KycStatus != KycStatus.Submitted)
            return new ReviewKycResult(false, "Doctor has not submitted KYC documents.", ErrorKind.Validation);

        var admin = await db.Users.FirstOrDefaultAsync(u => u.Id == request.AdminUserId, ct);
        if (admin is null)
            return new ReviewKycResult(false, "Admin user not found.", ErrorKind.NotFound);

        var review = new KycReview
        {
            DoctorProfileId = doctor.Id,
            ReviewedByUserId = request.AdminUserId,
            ClinicId = tenantContext.ClinicId,
            Decision = request.Decision,
            RejectionReason = request.Decision == KycDecision.Rejected ? request.RejectionReason : null,
            ReviewedAt = DateTime.UtcNow,
        };

        db.KycReviews.Add(review);

        if (request.Decision == KycDecision.Approved)
        {
            doctor.KycStatus = KycStatus.Approved;
            doctor.User.Status = AccountStatus.Active;
        }
        else
        {
            doctor.KycStatus = KycStatus.Rejected;
        }

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "KYC review for doctor {DoctorProfileId}: {Decision} by admin {AdminUserId}",
            doctor.Id, request.Decision, request.AdminUserId);

        try
        {
            var adminName = $"{admin.FirstName} {admin.LastName}";
            await eventBus.PublishAsync(Topics.KycReviewed, new KycReviewedEvent(
                doctor.Id,
                doctor.UserId,
                request.Decision,
                request.RejectionReason,
                adminName), ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to publish KYC reviewed event for doctor {DoctorProfileId}", doctor.Id);
        }

        return new ReviewKycResult(true);
    }
}

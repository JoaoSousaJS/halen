using Halen.Application.Common;
using Halen.Application.Events;
using Halen.Application.Interfaces;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Halen.Application.Reviews.Commands;

public class SubmitReviewCommandHandler(
    IAppDbContext db,
    ITenantContext tenantContext,
    IEventBus eventBus,
    ILogger<SubmitReviewCommandHandler> logger
) : IRequestHandler<SubmitReviewCommand, SubmitReviewResult>
{
    public async Task<SubmitReviewResult> Handle(SubmitReviewCommand request, CancellationToken ct)
    {
        var patientProfile = await db.PatientProfiles
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == request.PatientUserId, ct);

        if (patientProfile is null)
            return new SubmitReviewResult(false, Error: "Patient profile not found.", Kind: ErrorKind.NotFound);

        var appointment = await db.Appointments
            .Include(a => a.Doctor)
                .ThenInclude(d => d.User)
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, ct);

        if (appointment is null)
            return new SubmitReviewResult(false, Error: "Appointment not found.", Kind: ErrorKind.NotFound);

        if (appointment.PatientId != patientProfile.Id)
            return new SubmitReviewResult(false, Error: "You can only review your own appointments.", Kind: ErrorKind.Forbidden);

        if (appointment.Status != AppointmentStatus.Completed)
            return new SubmitReviewResult(false, Error: "Only completed appointments can be reviewed.", Kind: ErrorKind.Validation);

        var duplicateExists = await db.Reviews.AnyAsync(r => r.AppointmentId == request.AppointmentId, ct);
        if (duplicateExists)
            return new SubmitReviewResult(false, Error: "A review already exists for this appointment.", Kind: ErrorKind.Validation);

        var postedAs = $"{patientProfile.User.FirstName} {patientProfile.User.LastName[0]}.";

        var review = new Review
        {
            ClinicId = tenantContext.ClinicId,
            AppointmentId = request.AppointmentId,
            PatientProfileId = patientProfile.Id,
            DoctorProfileId = appointment.DoctorId,
            Rating = request.Rating,
            Title = request.Title,
            Body = request.Body,
            Tags = request.Tags,
            IsVerified = true,
            HelpfulCount = 0,
            ModerationStatus = ReviewModerationStatus.Approved,
            PostedAs = postedAs,
        };

        db.Reviews.Add(review);

        var stats = await db.Reviews
            .Where(r => r.DoctorProfileId == appointment.DoctorId && r.ModerationStatus == ReviewModerationStatus.Approved)
            .GroupBy(_ => 1)
            .Select(g => new { Count = g.Count(), Sum = g.Sum(r => r.Rating) })
            .FirstOrDefaultAsync(ct);

        var newCount = (stats?.Count ?? 0) + 1;
        var newAvg = (decimal)((stats?.Sum ?? 0) + request.Rating) / newCount;

        appointment.Doctor.AverageRating = Math.Round(newAvg, 2);
        appointment.Doctor.ReviewCount = newCount;

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Review {ReviewId} submitted by patient {PatientUserId} for doctor {DoctorProfileId}",
            review.Id, request.PatientUserId, appointment.DoctorId);

        try
        {
            await eventBus.PublishAsync(
                Topics.ReviewSubmitted,
                new ReviewSubmittedEvent(
                    review.Id,
                    appointment.Doctor.UserId,
                    patientProfile.UserId,
                    request.Rating,
                    patientProfile.User.FirstName,
                    $"Dr. {appointment.Doctor.User.LastName}"),
                ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to publish review.submitted event for review {ReviewId}", review.Id);
        }

        if (request.Rating <= 2)
        {
            try
            {
                await eventBus.PublishAsync(
                    Topics.ReviewLowStar,
                    new ReviewLowStarEvent(
                        review.Id,
                        appointment.Doctor.UserId,
                        appointment.DoctorId,
                        request.Rating,
                        patientProfile.User.FirstName,
                        $"Dr. {appointment.Doctor.User.LastName}"),
                    ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to publish review.low_star event for review {ReviewId}", review.Id);
            }
        }

        return new SubmitReviewResult(true, ReviewId: review.Id);
    }
}

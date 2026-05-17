using Halen.Application.Common;
using Halen.Application.Events;
using Halen.Application.Interfaces;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Halen.Application.Appointments.Commands;

public class CompleteAppointmentCommandHandler(
    IAppDbContext db,
    IEventBus eventBus,
    ILogger<CompleteAppointmentCommandHandler> logger,
    IPaymentService paymentService
) : IRequestHandler<CompleteAppointmentCommand, CompleteAppointmentResult>
{
    public async Task<CompleteAppointmentResult> Handle(CompleteAppointmentCommand request, CancellationToken ct)
    {
        var appointment = await db.Appointments
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, ct);

        if (appointment is null)
            return new CompleteAppointmentResult(false, "Appointment not found", ErrorKind.NotFound);

        var doctorProfile = await db.DoctorProfiles
            .Where(d => d.UserId == request.UserId)
            .Select(d => new { d.Id, d.User.FirstName, d.User.LastName })
            .FirstOrDefaultAsync(ct);

        if (doctorProfile is null || appointment.DoctorId != doctorProfile.Id)
            return new CompleteAppointmentResult(false, "You can only complete your own appointments", ErrorKind.Forbidden);

        if (appointment.Status is not (AppointmentStatus.Scheduled or AppointmentStatus.InProgress))
            return new CompleteAppointmentResult(false, "Only scheduled or in-progress appointments can be completed");

        appointment.Status = AppointmentStatus.Completed;
        appointment.Notes = request.Notes;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Appointment {AppointmentId} completed by doctor {DoctorId}",
            request.AppointmentId, doctorProfile.Id);

        var payment = await db.Payments
            .FirstOrDefaultAsync(p => p.AppointmentId == appointment.Id, ct);

        if (payment is { Status: PaymentStatus.Authorized, PaymentIntentId: not null })
        {
            var captureResult = await paymentService.CaptureIntentAsync(payment.PaymentIntentId, ct);
            if (captureResult.Success)
            {
                payment.Capture();
                await db.SaveChangesAsync(ct);
            }
            else
            {
                logger.LogError("Payment capture failed for appointment {AppointmentId}: {Error}",
                    appointment.Id, captureResult.ErrorMessage);
            }
        }

        var patientUserId = await db.PatientProfiles
            .Where(p => p.Id == appointment.PatientId)
            .Select(p => p.UserId)
            .FirstOrDefaultAsync(ct);

        try
        {
            await eventBus.PublishAsync(Topics.AppointmentCompleted, new AppointmentCompletedEvent(
                request.AppointmentId,
                request.UserId,
                patientUserId,
                $"{doctorProfile.FirstName} {doctorProfile.LastName}"), ct);

            if (payment is { Status: PaymentStatus.Captured })
            {
                await eventBus.PublishAsync(Topics.PaymentCaptured, new PaymentCapturedEvent(
                    payment.Id, appointment.Id, patientUserId, payment.Amount, payment.Currency), ct);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to publish completed event for appointment {AppointmentId}", request.AppointmentId);
        }

        return new CompleteAppointmentResult(true);
    }
}

using Halen.Application.Common;
using Halen.Application.Events;
using Halen.Application.Interfaces;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Halen.Application.Appointments.Commands;

public class CancelAppointmentCommandHandler(
    IAppDbContext db,
    IEventBus eventBus,
    ILogger<CancelAppointmentCommandHandler> logger,
    IPaymentService paymentService
) : IRequestHandler<CancelAppointmentCommand, CancelAppointmentResult>
{
    public async Task<CancelAppointmentResult> Handle(CancelAppointmentCommand request, CancellationToken ct)
    {
        var appointment = await db.Appointments
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, ct);

        if (appointment is null)
            return new CancelAppointmentResult(false, "Appointment not found", ErrorKind.NotFound);

        switch (request.UserRole)
        {
            case UserRole.Patient:
            {
                var profile = await db.PatientProfiles
                    .FirstOrDefaultAsync(p => p.UserId == request.UserId, ct);
                if (profile is null || appointment.PatientId != profile.Id)
                    return new CancelAppointmentResult(false, "You do not have permission to cancel this appointment", ErrorKind.Forbidden);
                break;
            }
            case UserRole.Doctor:
            {
                var profile = await db.DoctorProfiles
                    .FirstOrDefaultAsync(d => d.UserId == request.UserId, ct);
                if (profile is null || appointment.DoctorId != profile.Id)
                    return new CancelAppointmentResult(false, "You do not have permission to cancel this appointment", ErrorKind.Forbidden);
                break;
            }
            case UserRole.PlatformAdmin:
                break;
            default:
                return new CancelAppointmentResult(false, "This role is not allowed to cancel appointments", ErrorKind.Forbidden);
        }

        if (appointment.Status != AppointmentStatus.Scheduled)
            return new CancelAppointmentResult(false, "Only scheduled appointments can be cancelled");

        appointment.Status = AppointmentStatus.Cancelled;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Appointment {AppointmentId} cancelled by {UserRole} {UserId}",
            request.AppointmentId, request.UserRole, request.UserId);

        var payment = await db.Payments
            .FirstOrDefaultAsync(p => p.AppointmentId == appointment.Id, ct);

        if (payment is { Status: PaymentStatus.Authorized, PaymentIntentId: not null })
        {
            var refundResult = await paymentService.RefundIntentAsync(payment.PaymentIntentId, ct);
            if (refundResult.Success)
            {
                payment.Refund();
                await db.SaveChangesAsync(ct);
            }
            else
            {
                logger.LogWarning("Payment refund failed for appointment {AppointmentId}: {Error}",
                    appointment.Id, refundResult.ErrorMessage);
            }
        }

        var patientUserId = await db.PatientProfiles
            .Where(p => p.Id == appointment.PatientId)
            .Select(p => p.UserId)
            .FirstOrDefaultAsync(ct);

        var doctorUserId = await db.DoctorProfiles
            .Where(d => d.Id == appointment.DoctorId)
            .Select(d => d.UserId)
            .FirstOrDefaultAsync(ct);

        var cancellerName = await GetUserNameAsync(request.UserId, ct);

        try
        {
            await eventBus.PublishAsync(Topics.AppointmentCancelled, new AppointmentCancelledEvent(
                request.AppointmentId,
                request.UserId,
                patientUserId,
                doctorUserId,
                cancellerName,
                request.UserRole.ToString()), ct);

            if (payment is { Status: PaymentStatus.Refunded })
            {
                await eventBus.PublishAsync(Topics.PaymentRefunded, new PaymentRefundedEvent(
                    payment.Id, appointment.Id, patientUserId, payment.Amount, payment.Currency), ct);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to publish cancelled event for appointment {AppointmentId}", request.AppointmentId);
        }

        return new CancelAppointmentResult(true);
    }

    private async Task<string> GetUserNameAsync(Guid userId, CancellationToken ct)
    {
        var name = await db.PatientProfiles
            .Where(p => p.UserId == userId)
            .Select(p => p.User.FirstName + " " + p.User.LastName)
            .FirstOrDefaultAsync(ct);

        return name ?? await db.DoctorProfiles
            .Where(d => d.UserId == userId)
            .Select(d => d.User.FirstName + " " + d.User.LastName)
            .FirstOrDefaultAsync(ct) ?? "User";
    }
}

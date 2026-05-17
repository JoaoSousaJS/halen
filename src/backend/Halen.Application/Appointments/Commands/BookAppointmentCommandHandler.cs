using System.Data;
using Halen.Application.Common;
using Halen.Application.Events;
using Halen.Application.Interfaces;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Halen.Application.Appointments.Commands;

public class BookAppointmentCommandHandler(
    IAppDbContext db,
    ITenantContext tenantContext,
    IEventBus eventBus,
    ILogger<BookAppointmentCommandHandler> logger,
    IPaymentService paymentService
) : IRequestHandler<BookAppointmentCommand, BookAppointmentResult>
{
    private const int DefaultDurationMinutes = 20;

    public async Task<BookAppointmentResult> Handle(BookAppointmentCommand request, CancellationToken ct)
    {
        var doctor = await db.DoctorProfiles
            .Where(d => d.Id == request.DoctorId)
            .Select(d => new { d.UserId, d.User.FirstName, d.User.LastName, d.KycStatus, d.ConsultationFee })
            .FirstOrDefaultAsync(ct);

        if (doctor is null)
            return new BookAppointmentResult(false, null, "Doctor not found");

        if (doctor.KycStatus != KycStatus.Approved)
            return new BookAppointmentResult(false, null, "Doctor is not yet approved for appointments.");

        var patientData = await db.PatientProfiles
            .Where(p => p.UserId == request.UserId)
            .Select(p => new { Profile = p, p.User.FirstName, p.User.LastName })
            .FirstOrDefaultAsync(ct);

        PatientProfile patientProfile;
        string patientName;

        if (patientData is null)
        {
            patientProfile = new PatientProfile { UserId = request.UserId, ClinicId = tenantContext.ClinicId };
            db.PatientProfiles.Add(patientProfile);
            // Fetch name from User directly since there's no profile yet
            var user = await db.Users
                .Where(u => u.Id == request.UserId)
                .Select(u => new { u.FirstName, u.LastName })
                .FirstAsync(ct);
            patientName = $"{user.FirstName} {user.LastName}";
        }
        else
        {
            patientProfile = patientData.Profile;
            patientName = $"{patientData.FirstName} {patientData.LastName}";
        }

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        try
        {
            var dayOfWeek = request.ScheduledAt.DayOfWeek;
            var timeOfDay = TimeOnly.FromDateTime(request.ScheduledAt);

            var matchingWindow = await db.DoctorAvailabilities
                .Where(a => a.DoctorProfileId == request.DoctorId &&
                    a.IsActive &&
                    a.DayOfWeek == dayOfWeek &&
                    a.StartTime <= timeOfDay &&
                    timeOfDay.AddMinutes(DefaultDurationMinutes) <= a.EndTime)
                .Select(a => new { a.SlotDurationMinutes })
                .FirstOrDefaultAsync(ct);

            if (matchingWindow is null)
                return new BookAppointmentResult(false, null, "Doctor is not available at the requested time.");

            var endTime = request.ScheduledAt.AddMinutes(matchingWindow.SlotDurationMinutes);
            var hasConflict = await db.Appointments.AnyAsync(a =>
                a.DoctorId == request.DoctorId &&
                a.Status == AppointmentStatus.Scheduled &&
                a.ScheduledAt < endTime &&
                request.ScheduledAt < a.ScheduledAt.AddMinutes(a.DurationMinutes), ct);

            if (hasConflict)
                return new BookAppointmentResult(false, null, "This time slot is not available");

            var appointment = new Appointment
            {
                PatientId = patientProfile.Id,
                DoctorId = request.DoctorId,
                ScheduledAt = request.ScheduledAt,
                Reason = request.Reason,
                ClinicId = tenantContext.ClinicId,
            };

            db.Appointments.Add(appointment);
            await db.SaveChangesAsync(ct);

            var idempotencyKey = $"booking_{request.UserId}_{request.DoctorId}_{request.ScheduledAt:O}";
            var payment = new Payment
            {
                ClinicId = tenantContext.ClinicId,
                AppointmentId = appointment.Id,
                PatientProfileId = patientProfile.Id,
                Amount = doctor.ConsultationFee,
                Currency = "USD",
                Status = PaymentStatus.Pending,
                IdempotencyKey = idempotencyKey,
            };
            db.Payments.Add(payment);
            await db.SaveChangesAsync(ct);

            var intentResult = await paymentService.CreateIntentAsync(
                request.UserId, doctor.ConsultationFee, "USD", idempotencyKey, ct);

            if (!intentResult.Success)
            {
                return new BookAppointmentResult(false, null, "Payment authorization failed");
            }

            payment.Authorize(intentResult.PaymentIntentId!);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            logger.LogInformation("Appointment {AppointmentId} booked by patient {PatientId} with doctor {DoctorId}",
                appointment.Id, patientProfile.Id, request.DoctorId);

            try
            {
                await eventBus.PublishAsync(Topics.AppointmentBooked, new AppointmentBookedEvent(
                    appointment.Id,
                    request.UserId,
                    doctor.UserId,
                    request.ScheduledAt,
                    patientName,
                    $"{doctor.FirstName} {doctor.LastName}"), ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to publish booked event for appointment {AppointmentId}", appointment.Id);
            }

            return new BookAppointmentResult(true, appointment.Id, null, payment.Status.ToString());
        }
        catch (DbUpdateException)
        {
            return new BookAppointmentResult(false, null, "This time slot is not available");
        }
    }
}

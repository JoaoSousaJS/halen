using System.Data;
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
    IEventBus eventBus,
    ILogger<BookAppointmentCommandHandler> logger
) : IRequestHandler<BookAppointmentCommand, BookAppointmentResult>
{
    private const int DefaultDurationMinutes = 20;

    public async Task<BookAppointmentResult> Handle(BookAppointmentCommand request, CancellationToken ct)
    {
        var doctor = await db.DoctorProfiles
            .Where(d => d.Id == request.DoctorId)
            .Select(d => new { d.UserId, d.User.FirstName, d.User.LastName })
            .FirstOrDefaultAsync(ct);

        if (doctor is null)
            return new BookAppointmentResult(false, null, "Doctor not found");

        var patientProfile = await db.PatientProfiles
            .FirstOrDefaultAsync(p => p.UserId == request.UserId, ct);

        if (patientProfile is null)
        {
            patientProfile = new PatientProfile { UserId = request.UserId };
            db.PatientProfiles.Add(patientProfile);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        try
        {
            var endTime = request.ScheduledAt.AddMinutes(DefaultDurationMinutes);
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
            };

            db.Appointments.Add(appointment);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            logger.LogInformation("Appointment {AppointmentId} booked by patient {PatientId} with doctor {DoctorId}",
                appointment.Id, patientProfile.Id, request.DoctorId);

            var patientName = await db.PatientProfiles
                .Where(p => p.Id == patientProfile.Id)
                .Select(p => p.User.FirstName + " " + p.User.LastName)
                .FirstOrDefaultAsync(ct) ?? "Patient";

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

            return new BookAppointmentResult(true, appointment.Id);
        }
        catch (DbUpdateException)
        {
            return new BookAppointmentResult(false, null, "This time slot is not available");
        }
    }
}

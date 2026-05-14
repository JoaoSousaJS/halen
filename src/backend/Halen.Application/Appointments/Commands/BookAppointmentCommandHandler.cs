using System.Data;
using Halen.Application.Interfaces;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Halen.Application.Appointments.Commands;

public class BookAppointmentCommandHandler(
    IAppDbContext db,
    ILogger<BookAppointmentCommandHandler> logger
) : IRequestHandler<BookAppointmentCommand, BookAppointmentResult>
{
    private const int DefaultDurationMinutes = 20;

    public async Task<BookAppointmentResult> Handle(BookAppointmentCommand request, CancellationToken ct)
    {
        var doctorExists = await db.DoctorProfiles.AnyAsync(d => d.Id == request.DoctorId, ct);
        if (!doctorExists)
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

            return new BookAppointmentResult(true, appointment.Id);
        }
        catch (DbUpdateException)
        {
            return new BookAppointmentResult(false, null, "This time slot is not available");
        }
    }
}

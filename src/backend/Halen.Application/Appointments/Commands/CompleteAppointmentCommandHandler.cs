using Halen.Application.Common;
using Halen.Application.Events;
using Halen.Application.Interfaces;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Halen.Application.Appointments.Commands;

public class CompleteAppointmentCommandHandler(
    IAppDbContext db,
    IEventBus eventBus,
    ILogger<CompleteAppointmentCommandHandler> logger
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

        if (appointment.Status != AppointmentStatus.Scheduled)
            return new CompleteAppointmentResult(false, "Only scheduled appointments can be completed");

        appointment.Status = AppointmentStatus.Completed;
        appointment.Notes = request.Notes;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Appointment {AppointmentId} completed by doctor {DoctorId}",
            request.AppointmentId, doctorProfile.Id);

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
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to publish completed event for appointment {AppointmentId}", request.AppointmentId);
        }

        return new CompleteAppointmentResult(true);
    }
}

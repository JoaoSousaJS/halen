using Halen.Application.Common;
using Halen.Application.Interfaces;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Halen.Application.Appointments.Commands;

public class CompleteAppointmentCommandHandler(
    IAppDbContext db,
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
            .FirstOrDefaultAsync(d => d.UserId == request.UserId, ct);

        if (doctorProfile is null || appointment.DoctorId != doctorProfile.Id)
            return new CompleteAppointmentResult(false, "You can only complete your own appointments", ErrorKind.Forbidden);

        if (appointment.Status != AppointmentStatus.Scheduled)
            return new CompleteAppointmentResult(false, "Only scheduled appointments can be completed");

        appointment.Status = AppointmentStatus.Completed;
        appointment.Notes = request.Notes;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Appointment {AppointmentId} completed by doctor {DoctorId}",
            request.AppointmentId, doctorProfile.Id);

        return new CompleteAppointmentResult(true);
    }
}

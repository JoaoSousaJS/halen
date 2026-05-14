using Halen.Application.Common;
using Halen.Application.Interfaces;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Halen.Application.Appointments.Commands;

public class CancelAppointmentCommandHandler(
    IAppDbContext db,
    ILogger<CancelAppointmentCommandHandler> logger
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
            case UserRole.Admin:
                break;
        }

        if (appointment.Status != AppointmentStatus.Scheduled)
            return new CancelAppointmentResult(false, "Only scheduled appointments can be cancelled");

        appointment.Status = AppointmentStatus.Cancelled;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Appointment {AppointmentId} cancelled by {UserRole} {UserId}",
            request.AppointmentId, request.UserRole, request.UserId);

        return new CancelAppointmentResult(true);
    }
}

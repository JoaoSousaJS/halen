using Halen.Application.Common;
using Halen.Application.Interfaces;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Halen.Application.Consultations.Commands;

public class JoinConsultationRoomCommandHandler(
    IAppDbContext db,
    ILogger<JoinConsultationRoomCommandHandler> logger
) : IRequestHandler<JoinConsultationRoomCommand, JoinConsultationRoomResult>
{
    public async Task<JoinConsultationRoomResult> Handle(JoinConsultationRoomCommand request, CancellationToken ct)
    {
        var room = await db.ConsultationRooms
            .Include(r => r.Appointment)
            .FirstOrDefaultAsync(r => r.AppointmentId == request.AppointmentId, ct);

        if (room is null)
            return new JoinConsultationRoomResult(false, Error: "Consultation room not found", Kind: ErrorKind.NotFound);

        if (room.Status == ConsultationRoomStatus.Ended)
            return new JoinConsultationRoomResult(false, Error: "Consultation has already ended");

        if (request.Role == "Doctor")
            room.DoctorJoinedAt ??= DateTimeOffset.UtcNow;
        else
            room.PatientJoinedAt ??= DateTimeOffset.UtcNow;

        var started = false;
        if (room.DoctorJoinedAt is not null && room.PatientJoinedAt is not null
            && room.Status == ConsultationRoomStatus.Waiting)
        {
            room.Status = ConsultationRoomStatus.Active;
            room.StartedAt = DateTimeOffset.UtcNow;
            room.Appointment!.Status = AppointmentStatus.InProgress;
            started = true;
        }

        await db.SaveChangesAsync(ct);

        logger.LogInformation("User {UserId} ({Role}) joined consultation for appointment {AppointmentId}",
            request.UserId, request.Role, request.AppointmentId);

        return new JoinConsultationRoomResult(
            true,
            ConsultationStarted: started,
            RoomCode: room.RoomCode,
            StartedAt: room.StartedAt
        );
    }
}

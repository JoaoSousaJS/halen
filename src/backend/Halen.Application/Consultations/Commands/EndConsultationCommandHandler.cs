using Halen.Application.Appointments.Commands;
using Halen.Application.Common;
using Halen.Application.Interfaces;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Halen.Application.Consultations.Commands;

public class EndConsultationCommandHandler(
    IAppDbContext db,
    IMediator mediator,
    ILogger<EndConsultationCommandHandler> logger
) : IRequestHandler<EndConsultationCommand, EndConsultationResult>
{
    public async Task<EndConsultationResult> Handle(EndConsultationCommand request, CancellationToken ct)
    {
        var room = await db.ConsultationRooms
            .Include(r => r.Appointment)
            .FirstOrDefaultAsync(r => r.AppointmentId == request.AppointmentId, ct);

        if (room is null)
            return new EndConsultationResult(false, Error: "Consultation room not found", Kind: ErrorKind.NotFound);

        if (room.Status == ConsultationRoomStatus.Ended)
            return new EndConsultationResult(false, Error: "Consultation has already ended");

        var doctorProfile = await db.DoctorProfiles
            .Where(d => d.UserId == request.UserId)
            .Select(d => new { d.Id })
            .FirstOrDefaultAsync(ct);

        if (doctorProfile is null || room.Appointment!.DoctorId != doctorProfile.Id)
            return new EndConsultationResult(false, Error: "Only the assigned doctor can end the consultation", Kind: ErrorKind.Forbidden);

        room.Status = ConsultationRoomStatus.Ended;
        room.EndedAt = DateTimeOffset.UtcNow;

        if (request.Notes is not null)
            room.Notes = request.Notes;

        await db.SaveChangesAsync(ct);

        var completeResult = await mediator.Send(
            new CompleteAppointmentCommand(request.UserId, request.AppointmentId, room.Notes), ct);

        if (!completeResult.Success)
            logger.LogWarning("CompleteAppointmentCommand failed after ending consultation for appointment {AppointmentId}: {Error}",
                request.AppointmentId, completeResult.Error);

        return new EndConsultationResult(true, EndedAt: room.EndedAt);
    }
}

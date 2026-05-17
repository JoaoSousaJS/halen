using Halen.Application.Common;
using Halen.Application.Interfaces;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Halen.Application.Consultations.Commands;

public class SaveConsultationNotesCommandHandler(
    IAppDbContext db,
    ILogger<SaveConsultationNotesCommandHandler> logger
) : IRequestHandler<SaveConsultationNotesCommand, SaveConsultationNotesResult>
{
    public async Task<SaveConsultationNotesResult> Handle(SaveConsultationNotesCommand request, CancellationToken ct)
    {
        var room = await db.ConsultationRooms
            .Include(r => r.Appointment)
            .FirstOrDefaultAsync(r => r.AppointmentId == request.AppointmentId, ct);

        if (room is null)
            return new SaveConsultationNotesResult(false, Error: "Consultation room not found", Kind: ErrorKind.NotFound);

        if (room.Status == ConsultationRoomStatus.Ended)
            return new SaveConsultationNotesResult(false, Error: "Consultation has already ended");

        var doctorProfile = await db.DoctorProfiles
            .Where(d => d.UserId == request.UserId)
            .Select(d => new { d.Id })
            .FirstOrDefaultAsync(ct);

        if (doctorProfile is null || room.Appointment!.DoctorId != doctorProfile.Id)
            return new SaveConsultationNotesResult(false, Error: "Only the assigned doctor can save notes", Kind: ErrorKind.Forbidden);

        room.Notes = request.Notes;
        await db.SaveChangesAsync(ct);

        return new SaveConsultationNotesResult(true);
    }
}

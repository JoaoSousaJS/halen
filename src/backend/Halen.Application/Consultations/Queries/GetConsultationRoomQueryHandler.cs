using Halen.Application.Common;
using Halen.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Halen.Application.Consultations.Queries;

public class GetConsultationRoomQueryHandler(
    IAppDbContext db
) : IRequestHandler<GetConsultationRoomQuery, GetConsultationRoomResult>
{
    public async Task<GetConsultationRoomResult> Handle(GetConsultationRoomQuery request, CancellationToken ct)
    {
        var appointment = await db.Appointments
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, ct);

        if (appointment is null)
            return new GetConsultationRoomResult(false, Error: "Appointment not found", Kind: ErrorKind.NotFound);

        var doctorProfile = await db.DoctorProfiles
            .Where(d => d.UserId == request.UserId)
            .Select(d => new { d.Id })
            .FirstOrDefaultAsync(ct);

        var patientProfile = await db.PatientProfiles
            .Where(p => p.UserId == request.UserId)
            .Select(p => new { p.Id })
            .FirstOrDefaultAsync(ct);

        bool isDoctor = doctorProfile is not null && appointment.DoctorId == doctorProfile.Id;
        bool isPatient = patientProfile is not null && appointment.PatientId == patientProfile.Id;

        if (!isDoctor && !isPatient)
            return new GetConsultationRoomResult(false, Error: "You are not a participant of this appointment", Kind: ErrorKind.Forbidden);

        var room = await db.ConsultationRooms
            .Include(r => r.Appointment!)
                .ThenInclude(a => a.Doctor)
                    .ThenInclude(d => d.User)
            .Include(r => r.Appointment!)
                .ThenInclude(a => a.Patient)
                    .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(r => r.AppointmentId == request.AppointmentId, ct);

        if (room is null)
            return new GetConsultationRoomResult(false, Error: "Consultation room not found", Kind: ErrorKind.NotFound);

        var dto = new ConsultationRoomDto(
            room.Id,
            room.AppointmentId,
            room.RoomCode,
            room.Status.ToString(),
            room.StartedAt,
            room.EndedAt,
            room.DoctorJoinedAt,
            room.PatientJoinedAt,
            room.Notes,
            $"{room.Appointment!.Doctor.User.FirstName} {room.Appointment.Doctor.User.LastName}",
            $"{room.Appointment.Patient.User.FirstName} {room.Appointment.Patient.User.LastName}",
            room.Appointment.Reason,
            room.Appointment.DurationMinutes);

        return new GetConsultationRoomResult(true, Room: dto);
    }
}

using Halen.Application.Common;
using Halen.Application.Interfaces;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Halen.Application.Consultations.Commands;

public class StartConsultationCommandHandler(
    IAppDbContext db,
    ILogger<StartConsultationCommandHandler> logger
) : IRequestHandler<StartConsultationCommand, StartConsultationResult>
{
    public async Task<StartConsultationResult> Handle(StartConsultationCommand request, CancellationToken ct)
    {
        var appointment = await db.Appointments
            .Include(a => a.ConsultationRoom)
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, ct);

        if (appointment is null)
            return new StartConsultationResult(false, Error: "Appointment not found", Kind: ErrorKind.NotFound);

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
            return new StartConsultationResult(false, Error: "You are not a participant of this appointment", Kind: ErrorKind.Forbidden);

        if (appointment.Status is AppointmentStatus.Completed or AppointmentStatus.Cancelled)
            return new StartConsultationResult(false, Error: "Cannot start consultation for a completed or cancelled appointment");

        if (appointment.ConsultationRoom is { Status: not ConsultationRoomStatus.Ended } existingRoom)
            return new StartConsultationResult(true, RoomCode: existingRoom.RoomCode);

        var room = new ConsultationRoom
        {
            AppointmentId = appointment.Id,
            ClinicId = appointment.ClinicId,
            RoomCode = ConsultationRoom.GenerateRoomCode(),
            Status = ConsultationRoomStatus.Waiting,
        };

        db.ConsultationRooms.Add(room);
        appointment.VideoRoomId = room.RoomCode;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Consultation room {RoomCode} created for appointment {AppointmentId}",
            room.RoomCode, appointment.Id);

        return new StartConsultationResult(true, RoomCode: room.RoomCode);
    }
}

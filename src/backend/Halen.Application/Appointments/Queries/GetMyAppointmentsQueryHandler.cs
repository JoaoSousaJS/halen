using Halen.Application.Interfaces;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Halen.Application.Appointments.Queries;

public class GetMyAppointmentsQueryHandler(
    IAppDbContext db
) : IRequestHandler<GetMyAppointmentsQuery, GetMyAppointmentsResult>
{
    public async Task<GetMyAppointmentsResult> Handle(GetMyAppointmentsQuery request, CancellationToken ct)
    {
        IQueryable<Domain.Entities.Appointment> query;

        if (request.UserRole == UserRole.Patient)
        {
            var profile = await db.PatientProfiles
                .FirstOrDefaultAsync(p => p.UserId == request.UserId, ct);

            if (profile is null)
                return new GetMyAppointmentsResult([]);

            query = db.Appointments.Where(a => a.PatientId == profile.Id);
        }
        else
        {
            var profile = await db.DoctorProfiles
                .FirstOrDefaultAsync(d => d.UserId == request.UserId, ct);

            if (profile is null)
                return new GetMyAppointmentsResult([]);

            query = db.Appointments.Where(a => a.DoctorId == profile.Id);
        }

        var appointments = await query
            .OrderByDescending(a => a.ScheduledAt)
            .Select(a => new AppointmentDto(
                a.Id,
                a.ScheduledAt,
                a.DurationMinutes,
                a.Reason,
                a.Status.ToString(),
                a.Notes,
                $"{a.Doctor.User.FirstName} {a.Doctor.User.LastName}",
                a.Doctor.Specialty,
                a.Doctor.ConsultationFee,
                $"{a.Patient.User.FirstName} {a.Patient.User.LastName}",
                a.PatientId
            ))
            .ToListAsync(ct);

        return new GetMyAppointmentsResult(appointments);
    }
}

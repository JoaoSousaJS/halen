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
        var query = request.UserRole == UserRole.Patient
            ? db.Appointments.Where(a => a.Patient.UserId == request.UserId)
            : db.Appointments.Where(a => a.Doctor.UserId == request.UserId);

        var totalCount = await query.CountAsync(ct);

        var appointments = await query
            .AsNoTracking()
            .OrderByDescending(a => a.ScheduledAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
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
                a.PatientId,
                a.Payment != null ? a.Payment.Status.ToString() : null,
                a.Payment != null ? (decimal?)a.Payment.Amount : null
            ))
            .ToListAsync(ct);

        return new GetMyAppointmentsResult(appointments, totalCount);
    }
}

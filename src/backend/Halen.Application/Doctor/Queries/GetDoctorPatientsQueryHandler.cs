using Halen.Application.Interfaces;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Halen.Application.Doctor.Queries;

public class GetDoctorPatientsQueryHandler(
    IAppDbContext db
) : IRequestHandler<GetDoctorPatientsQuery, IReadOnlyList<DoctorPatientDto>>
{
    public async Task<IReadOnlyList<DoctorPatientDto>> Handle(GetDoctorPatientsQuery request, CancellationToken ct)
    {
        return await db.Appointments
            .AsNoTracking()
            .Where(a => a.Doctor.UserId == request.DoctorUserId
                     && a.Status == AppointmentStatus.Completed)
            .Select(a => new { a.PatientId, Name = a.Patient.User.FirstName + " " + a.Patient.User.LastName })
            .Distinct()
            .OrderBy(p => p.Name)
            .Select(p => new DoctorPatientDto(p.PatientId, p.Name))
            .ToListAsync(ct);
    }
}

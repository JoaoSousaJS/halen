using Halen.Application.Interfaces;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Halen.Application.Appointments.Queries;

public class ListDoctorsQueryHandler(
    IAppDbContext db
) : IRequestHandler<ListDoctorsQuery, ListDoctorsResult>
{
    public async Task<ListDoctorsResult> Handle(ListDoctorsQuery request, CancellationToken ct)
    {
        var query = db.DoctorProfiles
            .AsNoTracking()
            .Where(d => d.KycStatus == KycStatus.Approved && d.User.Status == AccountStatus.Active);

        var totalCount = await query.CountAsync(ct);

        var doctors = await query
            .OrderBy(d => d.User.LastName)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(d => new DoctorDto(
                d.Id,
                $"{d.User.FirstName} {d.User.LastName}",
                d.Specialty,
                d.ConsultationFee,
                d.YearsOfExperience
            ))
            .ToListAsync(ct);

        return new ListDoctorsResult(doctors, totalCount);
    }
}

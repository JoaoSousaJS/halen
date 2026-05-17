using Halen.Application.Interfaces;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Halen.Application.Doctors.Queries;

public class ListSpecialtiesQueryHandler(
    IAppDbContext db
) : IRequestHandler<ListSpecialtiesQuery, ListSpecialtiesResult>
{
    public async Task<ListSpecialtiesResult> Handle(ListSpecialtiesQuery request, CancellationToken ct)
    {
        var specialties = await db.DoctorProfiles
            .AsNoTracking()
            .Where(d => d.KycStatus == KycStatus.Approved && d.User.Status == AccountStatus.Active)
            .Select(d => d.Specialty)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync(ct);

        return new ListSpecialtiesResult(specialties);
    }
}

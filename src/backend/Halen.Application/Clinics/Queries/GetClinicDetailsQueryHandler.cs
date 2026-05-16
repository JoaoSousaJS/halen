using Halen.Application.Common;
using Halen.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Halen.Application.Clinics.Queries;

public class GetClinicDetailsQueryHandler(IAppDbContext db, ITenantContext tenantContext)
    : IRequestHandler<GetClinicDetailsQuery, GetClinicDetailsResult>
{
    public async Task<GetClinicDetailsResult> Handle(GetClinicDetailsQuery request, CancellationToken ct)
    {
        if (!tenantContext.IsPlatformAdmin && request.ClinicId != tenantContext.ClinicId)
            return new GetClinicDetailsResult(false, Error: "Access denied", Kind: ErrorKind.Forbidden);

        var dto = await db.Clinics
            .AsNoTracking()
            .Where(c => c.Id == request.ClinicId)
            .Select(c => new ClinicDetailsDto(
                c.Id, c.Name, c.Slug, c.IsActive,
                db.Users.Count(u => u.ClinicId == c.Id),
                c.FeatureFlags.Select(f => new FeatureFlagDto(f.FeatureKey, f.IsEnabled)).ToList(),
                c.CreatedAt))
            .FirstOrDefaultAsync(ct);

        if (dto is null)
            return new GetClinicDetailsResult(false, Error: "Clinic not found", Kind: ErrorKind.NotFound);

        return new GetClinicDetailsResult(true, dto);
    }
}

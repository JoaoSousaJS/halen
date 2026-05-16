using Halen.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Halen.Application.Clinics.Queries;

public class GetMyFeatureFlagsQueryHandler(IAppDbContext db, ITenantContext tenantContext)
    : IRequestHandler<GetMyFeatureFlagsQuery, List<FeatureFlagDto>>
{
    public async Task<List<FeatureFlagDto>> Handle(GetMyFeatureFlagsQuery request, CancellationToken ct)
    {
        return await db.ClinicFeatureFlags
            .AsNoTracking()
            .Where(f => f.ClinicId == tenantContext.ClinicId)
            .Select(f => new FeatureFlagDto(f.FeatureKey, f.IsEnabled))
            .ToListAsync(ct);
    }
}

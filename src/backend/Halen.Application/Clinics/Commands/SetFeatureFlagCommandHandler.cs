using Halen.Application.Common;
using Halen.Application.Interfaces;
using Halen.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Halen.Application.Clinics.Commands;

public class SetFeatureFlagCommandHandler(IAppDbContext db)
    : IRequestHandler<SetFeatureFlagCommand, SetFeatureFlagResult>
{
    public async Task<SetFeatureFlagResult> Handle(SetFeatureFlagCommand request, CancellationToken ct)
    {
        var clinicExists = await db.Clinics.AnyAsync(c => c.Id == request.ClinicId, ct);
        if (!clinicExists)
            return new SetFeatureFlagResult(false, "Clinic not found", ErrorKind.NotFound);

        var flag = await db.ClinicFeatureFlags
            .FirstOrDefaultAsync(f => f.ClinicId == request.ClinicId && f.FeatureKey == request.FeatureKey, ct);

        if (flag is null)
        {
            db.ClinicFeatureFlags.Add(new ClinicFeatureFlag
            {
                ClinicId = request.ClinicId,
                FeatureKey = request.FeatureKey,
                IsEnabled = request.IsEnabled,
            });
        }
        else
        {
            flag.IsEnabled = request.IsEnabled;
        }

        await db.SaveChangesAsync(ct);
        return new SetFeatureFlagResult(true);
    }
}

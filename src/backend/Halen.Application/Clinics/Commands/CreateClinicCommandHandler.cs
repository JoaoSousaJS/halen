using Halen.Application.Common;
using Halen.Application.Interfaces;
using Halen.Domain.Constants;
using Halen.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Halen.Application.Clinics.Commands;

public class CreateClinicCommandHandler(IAppDbContext db)
    : IRequestHandler<CreateClinicCommand, CreateClinicResult>
{
    public async Task<CreateClinicResult> Handle(CreateClinicCommand request, CancellationToken ct)
    {
        var slugExists = await db.Clinics
            .AnyAsync(c => c.Slug == request.Slug, ct);

        if (slugExists)
            return new CreateClinicResult(false, Error: "A clinic with this slug already exists", Kind: ErrorKind.Validation);

        var clinic = new Clinic
        {
            Name = request.Name,
            Slug = request.Slug,
        };

        db.Clinics.Add(clinic);

        foreach (var key in FeatureKeys.All)
        {
            db.ClinicFeatureFlags.Add(new ClinicFeatureFlag
            {
                ClinicId = clinic.Id,
                FeatureKey = key,
                IsEnabled = false,
            });
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return new CreateClinicResult(false, Error: "A clinic with this slug already exists", Kind: ErrorKind.Validation);
        }

        return new CreateClinicResult(true, clinic.Id);
    }
}

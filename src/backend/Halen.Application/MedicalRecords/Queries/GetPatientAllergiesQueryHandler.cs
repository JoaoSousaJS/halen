using Halen.Application.Common;
using Halen.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Halen.Application.MedicalRecords.Queries;

public class GetPatientAllergiesQueryHandler(
    IAppDbContext db,
    IRecordAccessChecker accessChecker
) : IRequestHandler<GetPatientAllergiesQuery, GetPatientAllergiesResult>
{
    public async Task<GetPatientAllergiesResult> Handle(GetPatientAllergiesQuery request, CancellationToken ct)
    {
        var canAccess = await accessChecker.CanAccessPatientRecord(
            request.CallerUserId, request.CallerRole, request.PatientProfileId, ct);

        if (!canAccess)
            return new GetPatientAllergiesResult(false, Error: "Access denied", Kind: ErrorKind.Forbidden);

        var allergies = await db.PatientAllergies
            .AsNoTracking()
            .Where(a => a.PatientProfileId == request.PatientProfileId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AllergyDto(
                a.Id,
                a.AllergenName,
                a.Reaction,
                a.Severity.ToString(),
                a.DateIdentified != null ? a.DateIdentified.Value.ToString("yyyy-MM-dd") : null,
                a.IsActive,
                a.AddedByUser.FirstName + " " + a.AddedByUser.LastName,
                a.CreatedAt))
            .ToArrayAsync(ct);

        return new GetPatientAllergiesResult(true, allergies);
    }
}

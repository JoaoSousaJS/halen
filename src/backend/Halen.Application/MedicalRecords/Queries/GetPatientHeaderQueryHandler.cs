using Halen.Application.Common;
using Halen.Application.Interfaces;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Halen.Application.MedicalRecords.Queries;

public class GetPatientHeaderQueryHandler(
    IAppDbContext db,
    IRecordAccessChecker accessChecker
) : IRequestHandler<GetPatientHeaderQuery, GetPatientHeaderResult>
{
    public async Task<GetPatientHeaderResult> Handle(GetPatientHeaderQuery request, CancellationToken ct)
    {
        var canAccess = await accessChecker.CanAccessPatientRecord(
            request.CallerUserId, request.CallerRole, request.PatientProfileId, ct);

        if (!canAccess)
            return new GetPatientHeaderResult(false, Error: "Access denied", Kind: ErrorKind.Forbidden);

        var patient = await db.PatientProfiles
            .AsNoTracking()
            .Where(p => p.Id == request.PatientProfileId)
            .Select(p => new
            {
                p.Id,
                PatientName = p.User.FirstName + " " + p.User.LastName,
                p.City
            })
            .FirstOrDefaultAsync(ct);

        if (patient is null)
            return new GetPatientHeaderResult(false, Error: "Patient not found", Kind: ErrorKind.NotFound);

        var allergyChips = await db.PatientAllergies
            .AsNoTracking()
            .Where(a => a.PatientProfileId == request.PatientProfileId && a.IsActive)
            .Select(a => a.AllergenName)
            .ToArrayAsync(ct);

        var conditionChips = await db.PatientConditions
            .AsNoTracking()
            .Where(c => c.PatientProfileId == request.PatientProfileId && c.Status == ConditionStatus.Active)
            .Select(c => c.IcdDescription)
            .ToArrayAsync(ct);

        var header = new PatientHeaderDto(
            patient.Id,
            patient.PatientName,
            patient.City,
            allergyChips,
            conditionChips);

        return new GetPatientHeaderResult(true, header);
    }
}

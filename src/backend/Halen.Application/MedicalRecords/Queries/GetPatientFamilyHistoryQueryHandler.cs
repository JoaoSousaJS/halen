using Halen.Application.Common;
using Halen.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Halen.Application.MedicalRecords.Queries;

public class GetPatientFamilyHistoryQueryHandler(
    IAppDbContext db,
    IRecordAccessChecker accessChecker
) : IRequestHandler<GetPatientFamilyHistoryQuery, GetPatientFamilyHistoryResult>
{
    public async Task<GetPatientFamilyHistoryResult> Handle(GetPatientFamilyHistoryQuery request, CancellationToken ct)
    {
        var canAccess = await accessChecker.CanAccessPatientRecord(
            request.CallerUserId, request.CallerRole, request.PatientProfileId, ct);

        if (!canAccess)
            return new GetPatientFamilyHistoryResult(false, Error: "Access denied", Kind: ErrorKind.Forbidden);

        var entries = await db.PatientFamilyHistories
            .AsNoTracking()
            .Where(f => f.PatientProfileId == request.PatientProfileId)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new FamilyHistoryDto(
                f.Id,
                f.Relationship,
                f.ConditionName,
                f.AgeAtOnset,
                f.Notes,
                f.AddedByUser.FirstName + " " + f.AddedByUser.LastName,
                f.CreatedAt))
            .ToArrayAsync(ct);

        return new GetPatientFamilyHistoryResult(true, entries);
    }
}

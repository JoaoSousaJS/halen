using Halen.Application.Common;
using Halen.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Halen.Application.MedicalRecords.Queries;

public class GetPatientConditionsQueryHandler(
    IAppDbContext db,
    IRecordAccessChecker accessChecker
) : IRequestHandler<GetPatientConditionsQuery, GetPatientConditionsResult>
{
    public async Task<GetPatientConditionsResult> Handle(GetPatientConditionsQuery request, CancellationToken ct)
    {
        var canAccess = await accessChecker.CanAccessPatientRecord(
            request.CallerUserId, request.CallerRole, request.PatientProfileId, ct);

        if (!canAccess)
            return new GetPatientConditionsResult(false, Error: "Access denied", Kind: ErrorKind.Forbidden);

        var conditions = await db.PatientConditions
            .AsNoTracking()
            .Where(c => c.PatientProfileId == request.PatientProfileId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new ConditionDto(
                c.Id,
                c.IcdCode,
                c.IcdDescription,
                c.DateOfOnset != null ? c.DateOfOnset.Value.ToString("yyyy-MM-dd") : null,
                c.Severity.ToString(),
                c.Status.ToString(),
                c.ClinicalNotes,
                c.AddedByUser.FirstName + " " + c.AddedByUser.LastName,
                c.CreatedAt))
            .ToArrayAsync(ct);

        return new GetPatientConditionsResult(true, conditions);
    }
}

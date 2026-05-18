using Halen.Application.Common;
using Halen.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Halen.Application.MedicalRecords.Queries;

public class GetPatientVitalsHistoryQueryHandler(
    IAppDbContext db,
    IRecordAccessChecker accessChecker
) : IRequestHandler<GetPatientVitalsHistoryQuery, GetPatientVitalsHistoryResult>
{
    public async Task<GetPatientVitalsHistoryResult> Handle(GetPatientVitalsHistoryQuery request, CancellationToken ct)
    {
        var canAccess = await accessChecker.CanAccessPatientRecord(
            request.CallerUserId, request.CallerRole, request.PatientProfileId, ct);

        if (!canAccess)
            return new GetPatientVitalsHistoryResult(false, Error: "Access denied", Kind: ErrorKind.Forbidden);

        var cutoff = DateTime.UtcNow.AddDays(-request.DaysBack);

        var readings = await db.PatientVitals
            .AsNoTracking()
            .Where(v => v.PatientProfileId == request.PatientProfileId
                        && v.VitalType == request.VitalType
                        && v.MeasuredAt >= cutoff)
            .OrderByDescending(v => v.MeasuredAt)
            .Select(v => new VitalReadingDetailDto(
                v.Id,
                v.Value,
                v.SecondaryValue,
                v.Unit,
                v.MeasuredAt,
                v.Source.ToString(),
                v.Notes,
                v.AddedByUser.FirstName + " " + v.AddedByUser.LastName))
            .ToArrayAsync(ct);

        return new GetPatientVitalsHistoryResult(true, readings);
    }
}

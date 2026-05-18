using Halen.Application.Common;
using Halen.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Halen.Application.MedicalRecords.Queries;

public class GetPatientMedicationsQueryHandler(
    IAppDbContext db,
    IRecordAccessChecker accessChecker
) : IRequestHandler<GetPatientMedicationsQuery, GetPatientMedicationsResult>
{
    public async Task<GetPatientMedicationsResult> Handle(GetPatientMedicationsQuery request, CancellationToken ct)
    {
        var canAccess = await accessChecker.CanAccessPatientRecord(
            request.CallerUserId, request.CallerRole, request.PatientProfileId, ct);

        if (!canAccess)
            return new GetPatientMedicationsResult(false, Error: "Access denied", Kind: ErrorKind.Forbidden);

        var medications = await db.PatientMedications
            .AsNoTracking()
            .Where(m => m.PatientProfileId == request.PatientProfileId)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new MedicationDto(
                m.Id,
                m.MedicationName,
                m.Dosage,
                m.Frequency,
                m.StartDate != null ? m.StartDate.Value.ToString("yyyy-MM-dd") : null,
                m.EndDate != null ? m.EndDate.Value.ToString("yyyy-MM-dd") : null,
                m.IsActive,
                m.PrescribedByName,
                m.LinkedPrescriptionId,
                m.AddedByUser.FirstName + " " + m.AddedByUser.LastName,
                m.CreatedAt))
            .ToArrayAsync(ct);

        return new GetPatientMedicationsResult(true, medications);
    }
}

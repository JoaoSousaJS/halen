using Halen.Application.Common;
using Halen.Application.Interfaces;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Halen.Application.MedicalRecords.Queries;

public class GetPatientSnapshotQueryHandler(
    IAppDbContext db,
    IRecordAccessChecker accessChecker
) : IRequestHandler<GetPatientSnapshotQuery, GetPatientSnapshotResult>
{
    public async Task<GetPatientSnapshotResult> Handle(GetPatientSnapshotQuery request, CancellationToken ct)
    {
        var canAccess = await accessChecker.CanAccessPatientRecord(
            request.CallerUserId, request.CallerRole, request.PatientProfileId, ct);

        if (!canAccess)
            return new GetPatientSnapshotResult(false, Error: "Access denied", Kind: ErrorKind.Forbidden);

        var allergies = await db.PatientAllergies
            .AsNoTracking()
            .Where(a => a.PatientProfileId == request.PatientProfileId && a.IsActive)
            .Select(a => new AllergySnapshotDto(
                a.Id,
                a.AllergenName,
                a.Reaction,
                a.Severity.ToString()))
            .ToArrayAsync(ct);

        var activeConditions = await db.PatientConditions
            .AsNoTracking()
            .Where(c => c.PatientProfileId == request.PatientProfileId && c.Status == ConditionStatus.Active)
            .Select(c => new ConditionSnapshotDto(
                c.Id,
                c.IcdDescription,
                c.Severity.ToString()))
            .ToArrayAsync(ct);

        var activeMedications = await db.PatientMedications
            .AsNoTracking()
            .Where(m => m.PatientProfileId == request.PatientProfileId && m.IsActive)
            .Select(m => new MedicationSnapshotDto(
                m.Id,
                m.MedicationName,
                m.Dosage,
                m.Frequency,
                m.StartDate != null ? m.StartDate.Value.ToString("yyyy-MM-dd") : null))
            .ToArrayAsync(ct);

        var familyHistory = await db.PatientFamilyHistories
            .AsNoTracking()
            .Where(f => f.PatientProfileId == request.PatientProfileId)
            .Select(f => new FamilyHistorySnapshotDto(
                f.Id,
                f.Relationship,
                f.ConditionName))
            .ToArrayAsync(ct);

        var latestVitals = await GetLatestVitalsAsync(request.PatientProfileId, ct);

        // Onboarding progress: count categories that have at least one entry
        var hasAllergies = await db.PatientAllergies.AnyAsync(a => a.PatientProfileId == request.PatientProfileId, ct);
        var hasConditions = await db.PatientConditions.AnyAsync(c => c.PatientProfileId == request.PatientProfileId, ct);
        var hasMedications = await db.PatientMedications.AnyAsync(m => m.PatientProfileId == request.PatientProfileId, ct);
        var hasVitals = await db.PatientVitals.AnyAsync(v => v.PatientProfileId == request.PatientProfileId, ct);
        var hasDocuments = await db.MedicalDocuments.AnyAsync(d => d.PatientProfileId == request.PatientProfileId, ct);
        var hasFamilyHistory = await db.PatientFamilyHistories.AnyAsync(f => f.PatientProfileId == request.PatientProfileId, ct);

        var onboardingProgress =
            (hasAllergies ? 1 : 0) +
            (hasConditions ? 1 : 0) +
            (hasMedications ? 1 : 0) +
            (hasVitals ? 1 : 0) +
            (hasDocuments ? 1 : 0) +
            (hasFamilyHistory ? 1 : 0);

        var snapshot = new PatientSnapshotDto(
            allergies,
            activeConditions,
            activeMedications,
            familyHistory,
            latestVitals,
            onboardingProgress);

        return new GetPatientSnapshotResult(true, snapshot);
    }

    private async Task<LatestVitalsDto> GetLatestVitalsAsync(Guid patientProfileId, CancellationToken ct)
    {
        var vitals = await db.PatientVitals
            .AsNoTracking()
            .Where(v => v.PatientProfileId == patientProfileId)
            .ToListAsync(ct);

        VitalReadingDto? GetLatest(VitalType type)
        {
            var latest = vitals
                .Where(v => v.VitalType == type)
                .OrderByDescending(v => v.MeasuredAt)
                .FirstOrDefault();

            return latest is null
                ? null
                : new VitalReadingDto(latest.Value, latest.SecondaryValue, latest.Unit, latest.MeasuredAt);
        }

        return new LatestVitalsDto(
            GetLatest(VitalType.BloodPressure),
            GetLatest(VitalType.HeartRate),
            GetLatest(VitalType.Weight),
            GetLatest(VitalType.SpO2));
    }
}

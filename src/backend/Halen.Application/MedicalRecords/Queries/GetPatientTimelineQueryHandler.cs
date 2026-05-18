using Halen.Application.Common;
using Halen.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Halen.Application.MedicalRecords.Queries;

public class GetPatientTimelineQueryHandler(
    IAppDbContext db,
    IRecordAccessChecker accessChecker
) : IRequestHandler<GetPatientTimelineQuery, GetPatientTimelineResult>
{
    public async Task<GetPatientTimelineResult> Handle(GetPatientTimelineQuery request, CancellationToken ct)
    {
        var canAccess = await accessChecker.CanAccessPatientRecord(
            request.CallerUserId, request.CallerRole, request.PatientProfileId, ct);

        if (!canAccess)
            return new GetPatientTimelineResult(false, Error: "Access denied", Kind: ErrorKind.Forbidden);

        var filterTypes = request.FilterTypes?.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var entries = new List<TimelineEntryDto>();

        if (ShouldInclude(filterTypes, "Appointment"))
        {
            var appointments = await db.Appointments
                .AsNoTracking()
                .Where(a => a.PatientId == request.PatientProfileId)
                .Select(a => new TimelineEntryDto(
                    a.Id,
                    "Appointment",
                    a.ScheduledAt,
                    a.Reason,
                    a.Status.ToString(),
                    a.Doctor.User.FirstName + " " + a.Doctor.User.LastName))
                .ToListAsync(ct);
            entries.AddRange(appointments);
        }

        if (ShouldInclude(filterTypes, "Prescription"))
        {
            var prescriptions = await db.Prescriptions
                .AsNoTracking()
                .Where(p => p.PatientId == request.PatientProfileId)
                .Select(p => new TimelineEntryDto(
                    p.Id,
                    "Prescription",
                    p.CreatedAt,
                    p.DrugName + " " + p.Dosage,
                    p.Status.ToString(),
                    p.Doctor.User.FirstName + " " + p.Doctor.User.LastName))
                .ToListAsync(ct);
            entries.AddRange(prescriptions);
        }

        if (ShouldInclude(filterTypes, "Condition"))
        {
            var conditions = await db.PatientConditions
                .AsNoTracking()
                .Where(c => c.PatientProfileId == request.PatientProfileId)
                .Select(c => new TimelineEntryDto(
                    c.Id,
                    "Condition",
                    c.CreatedAt,
                    c.IcdDescription,
                    c.Severity.ToString(),
                    c.AddedByUser.FirstName + " " + c.AddedByUser.LastName))
                .ToListAsync(ct);
            entries.AddRange(conditions);
        }

        if (ShouldInclude(filterTypes, "Allergy"))
        {
            var allergies = await db.PatientAllergies
                .AsNoTracking()
                .Where(a => a.PatientProfileId == request.PatientProfileId)
                .Select(a => new TimelineEntryDto(
                    a.Id,
                    "Allergy",
                    a.CreatedAt,
                    a.AllergenName,
                    a.Severity.ToString(),
                    a.AddedByUser.FirstName + " " + a.AddedByUser.LastName))
                .ToListAsync(ct);
            entries.AddRange(allergies);
        }

        if (ShouldInclude(filterTypes, "Vital"))
        {
            var vitals = await db.PatientVitals
                .AsNoTracking()
                .Where(v => v.PatientProfileId == request.PatientProfileId)
                .Select(v => new TimelineEntryDto(
                    v.Id,
                    "Vital",
                    v.MeasuredAt,
                    v.VitalType.ToString() + ": " + v.Value + " " + v.Unit,
                    v.Source.ToString(),
                    v.AddedByUser.FirstName + " " + v.AddedByUser.LastName))
                .ToListAsync(ct);
            entries.AddRange(vitals);
        }

        if (ShouldInclude(filterTypes, "Document"))
        {
            var documents = await db.MedicalDocuments
                .AsNoTracking()
                .Where(d => d.PatientProfileId == request.PatientProfileId)
                .Select(d => new TimelineEntryDto(
                    d.Id,
                    "Document",
                    d.CreatedAt,
                    d.Title,
                    d.DocumentType.ToString(),
                    d.UploadedByUser.FirstName + " " + d.UploadedByUser.LastName))
                .ToListAsync(ct);
            entries.AddRange(documents);
        }

        if (ShouldInclude(filterTypes, "Medication"))
        {
            var medications = await db.PatientMedications
                .AsNoTracking()
                .Where(m => m.PatientProfileId == request.PatientProfileId)
                .Select(m => new TimelineEntryDto(
                    m.Id,
                    "Medication",
                    m.CreatedAt,
                    m.MedicationName + " " + m.Dosage,
                    m.IsActive ? "Active" : "Inactive",
                    m.AddedByUser.FirstName + " " + m.AddedByUser.LastName))
                .ToListAsync(ct);
            entries.AddRange(medications);
        }

        if (ShouldInclude(filterTypes, "FamilyHistory"))
        {
            var familyHistory = await db.PatientFamilyHistories
                .AsNoTracking()
                .Where(f => f.PatientProfileId == request.PatientProfileId)
                .Select(f => new TimelineEntryDto(
                    f.Id,
                    "FamilyHistory",
                    f.CreatedAt,
                    f.ConditionName,
                    f.Relationship,
                    f.AddedByUser.FirstName + " " + f.AddedByUser.LastName))
                .ToListAsync(ct);
            entries.AddRange(familyHistory);
        }

        // Apply date filters
        if (request.From.HasValue)
            entries = entries.Where(e => e.OccurredAt >= request.From.Value).ToList();
        if (request.To.HasValue)
            entries = entries.Where(e => e.OccurredAt <= request.To.Value).ToList();

        // Sort by OccurredAt descending
        entries = entries.OrderByDescending(e => e.OccurredAt).ToList();

        var totalCount = entries.Count;

        // Paginate
        var paginated = entries
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToArray();

        return new GetPatientTimelineResult(true, paginated, totalCount);
    }

    private static bool ShouldInclude(HashSet<string>? filterTypes, string type)
        => filterTypes is null || filterTypes.Count == 0 || filterTypes.Contains(type);
}

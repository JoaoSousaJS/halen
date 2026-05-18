using Halen.Application.Common;
using Halen.Application.Events;
using Halen.Application.Interfaces;
using Halen.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Halen.Application.MedicalRecords.Commands;

public class AddMedicationCommandHandler(
    IAppDbContext db,
    ITenantContext tenantContext,
    IEventBus eventBus,
    IRecordAccessChecker accessChecker,
    ILogger<AddMedicationCommandHandler> logger
) : IRequestHandler<AddMedicationCommand, AddMedicationResult>
{
    public async Task<AddMedicationResult> Handle(AddMedicationCommand request, CancellationToken ct)
    {
        var hasAccess = await accessChecker.CanAccessPatientRecord(
            request.CallerUserId, request.CallerRole, request.PatientProfileId, ct);

        if (!hasAccess)
            return new AddMedicationResult(false, Error: "Access denied.", Kind: ErrorKind.Forbidden);

        // Validate EndDate is after StartDate when both are provided
        if (request.StartDate.HasValue && request.EndDate.HasValue && request.EndDate < request.StartDate)
            return new AddMedicationResult(false, Error: "End date must be after start date.", Kind: ErrorKind.Validation);

        // Verify linked prescription exists if provided
        if (request.LinkedPrescriptionId.HasValue)
        {
            var prescriptionExists = await db.Prescriptions
                .AsNoTracking()
                .AnyAsync(p => p.Id == request.LinkedPrescriptionId.Value, ct);

            if (!prescriptionExists)
                return new AddMedicationResult(false, Error: "Prescription not found.", Kind: ErrorKind.NotFound);
        }

        var medication = new PatientMedication
        {
            PatientProfileId = request.PatientProfileId,
            MedicationName = request.MedicationName,
            Dosage = request.Dosage,
            Frequency = request.Frequency,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsActive = true,
            PrescribedByName = request.PrescribedByName,
            LinkedPrescriptionId = request.LinkedPrescriptionId,
            AddedByUserId = request.CallerUserId,
            ClinicId = tenantContext.ClinicId,
        };

        db.PatientMedications.Add(medication);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Medication {MedicationId} added for patient {PatientProfileId} by {CallerUserId}",
            medication.Id, request.PatientProfileId, request.CallerUserId);

        try
        {
            var patient = await db.PatientProfiles
                .AsNoTracking()
                .FirstAsync(p => p.Id == request.PatientProfileId, ct);

            await eventBus.PublishAsync(Topics.MedicalRecordUpdated, new MedicalRecordUpdatedEvent(
                request.PatientProfileId, patient.UserId, "Medication", "Added",
                request.CallerUserId, DateTime.UtcNow), ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to publish event for medication {MedicationId}", medication.Id);
        }

        return new AddMedicationResult(true, medication.Id);
    }
}

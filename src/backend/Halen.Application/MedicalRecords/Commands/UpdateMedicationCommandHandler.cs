using Halen.Application.Common;
using Halen.Application.Events;
using Halen.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Halen.Application.MedicalRecords.Commands;

public class UpdateMedicationCommandHandler(
    IAppDbContext db,
    ITenantContext tenantContext,
    IEventBus eventBus,
    IRecordAccessChecker accessChecker,
    ILogger<UpdateMedicationCommandHandler> logger
) : IRequestHandler<UpdateMedicationCommand, UpdateMedicationResult>
{
    public async Task<UpdateMedicationResult> Handle(UpdateMedicationCommand request, CancellationToken ct)
    {
        var medication = await db.PatientMedications
            .FirstOrDefaultAsync(m => m.Id == request.MedicationId, ct);

        if (medication is null)
            return new UpdateMedicationResult(false, Error: "Medication not found.", Kind: ErrorKind.NotFound);

        var hasAccess = await accessChecker.CanAccessPatientRecord(
            request.CallerUserId, request.CallerRole, medication.PatientProfileId, ct);

        if (!hasAccess)
            return new UpdateMedicationResult(false, Error: "Access denied.", Kind: ErrorKind.Forbidden);

        medication.Dosage = request.Dosage;
        medication.Frequency = request.Frequency;
        medication.EndDate = request.EndDate;
        medication.IsActive = request.IsActive;

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Medication {MedicationId} updated by {CallerUserId}",
            request.MedicationId, request.CallerUserId);

        try
        {
            var patient = await db.PatientProfiles
                .AsNoTracking()
                .FirstAsync(p => p.Id == medication.PatientProfileId, ct);

            await eventBus.PublishAsync(Topics.MedicalRecordUpdated, new MedicalRecordUpdatedEvent(
                medication.PatientProfileId, patient.UserId, "Medication", "Updated",
                request.CallerUserId, DateTime.UtcNow), ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to publish event for medication {MedicationId}", request.MedicationId);
        }

        return new UpdateMedicationResult(true);
    }
}

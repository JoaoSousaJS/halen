using Halen.Application.Common;
using Halen.Application.Events;
using Halen.Application.Interfaces;
using Halen.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Halen.Application.MedicalRecords.Commands;

public class AddAllergyCommandHandler(
    IAppDbContext db,
    ITenantContext tenantContext,
    IEventBus eventBus,
    IRecordAccessChecker accessChecker,
    ILogger<AddAllergyCommandHandler> logger
) : IRequestHandler<AddAllergyCommand, AddAllergyResult>
{
    public async Task<AddAllergyResult> Handle(AddAllergyCommand request, CancellationToken ct)
    {
        var hasAccess = await accessChecker.CanAccessPatientRecord(
            request.CallerUserId, request.CallerRole, request.PatientProfileId, ct);

        if (!hasAccess)
            return new AddAllergyResult(false, Error: "Access denied.", Kind: ErrorKind.Forbidden);

        // Check for duplicate allergen (case-insensitive)
        var duplicateExists = await db.PatientAllergies
            .AsNoTracking()
            .AnyAsync(a =>
                a.PatientProfileId == request.PatientProfileId &&
                a.AllergenName.ToLower() == request.AllergenName.ToLower(), ct);

        if (duplicateExists)
            return new AddAllergyResult(false, Error: "Allergy already exists for this allergen.", Kind: ErrorKind.Validation);

        var allergy = new PatientAllergy
        {
            PatientProfileId = request.PatientProfileId,
            AllergenName = request.AllergenName,
            Reaction = request.Reaction,
            Severity = request.Severity,
            DateIdentified = request.DateIdentified,
            IsActive = true,
            AddedByUserId = request.CallerUserId,
            ClinicId = tenantContext.ClinicId,
        };

        db.PatientAllergies.Add(allergy);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Allergy {AllergyId} added for patient {PatientProfileId} by {CallerUserId}",
            allergy.Id, request.PatientProfileId, request.CallerUserId);

        try
        {
            var patient = await db.PatientProfiles
                .AsNoTracking()
                .FirstAsync(p => p.Id == request.PatientProfileId, ct);

            await eventBus.PublishAsync(Topics.MedicalRecordUpdated, new MedicalRecordUpdatedEvent(
                request.PatientProfileId, patient.UserId, "Allergy", "Added",
                request.CallerUserId, DateTime.UtcNow), ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to publish event for allergy {AllergyId}", allergy.Id);
        }

        return new AddAllergyResult(true, allergy.Id);
    }
}

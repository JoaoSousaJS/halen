using Halen.Application.Common;
using Halen.Application.Events;
using Halen.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Halen.Application.MedicalRecords.Commands;

public class UpdateAllergyCommandHandler(
    IAppDbContext db,
    ITenantContext tenantContext,
    IEventBus eventBus,
    IRecordAccessChecker accessChecker,
    ILogger<UpdateAllergyCommandHandler> logger
) : IRequestHandler<UpdateAllergyCommand, UpdateAllergyResult>
{
    public async Task<UpdateAllergyResult> Handle(UpdateAllergyCommand request, CancellationToken ct)
    {
        var allergy = await db.PatientAllergies
            .FirstOrDefaultAsync(a => a.Id == request.AllergyId, ct);

        if (allergy is null)
            return new UpdateAllergyResult(false, Error: "Allergy not found.", Kind: ErrorKind.NotFound);

        var hasAccess = await accessChecker.CanAccessPatientRecord(
            request.CallerUserId, request.CallerRole, allergy.PatientProfileId, ct);

        if (!hasAccess)
            return new UpdateAllergyResult(false, Error: "Access denied.", Kind: ErrorKind.Forbidden);

        allergy.Reaction = request.Reaction;
        allergy.Severity = request.Severity;
        allergy.IsActive = request.IsActive;

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Allergy {AllergyId} updated by {CallerUserId}",
            request.AllergyId, request.CallerUserId);

        try
        {
            var patient = await db.PatientProfiles
                .AsNoTracking()
                .FirstAsync(p => p.Id == allergy.PatientProfileId, ct);

            await eventBus.PublishAsync(Topics.MedicalRecordUpdated, new MedicalRecordUpdatedEvent(
                allergy.PatientProfileId, patient.UserId, "Allergy", "Updated",
                request.CallerUserId, DateTime.UtcNow), ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to publish event for allergy {AllergyId}", request.AllergyId);
        }

        return new UpdateAllergyResult(true);
    }
}

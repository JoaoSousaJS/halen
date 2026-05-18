using Halen.Application.Common;
using Halen.Application.Events;
using Halen.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Halen.Application.MedicalRecords.Commands;

public class UpdateConditionCommandHandler(
    IAppDbContext db,
    ITenantContext tenantContext,
    IEventBus eventBus,
    IRecordAccessChecker accessChecker,
    ILogger<UpdateConditionCommandHandler> logger
) : IRequestHandler<UpdateConditionCommand, UpdateConditionResult>
{
    public async Task<UpdateConditionResult> Handle(UpdateConditionCommand request, CancellationToken ct)
    {
        var condition = await db.PatientConditions
            .FirstOrDefaultAsync(c => c.Id == request.ConditionId, ct);

        if (condition is null)
            return new UpdateConditionResult(false, Error: "Condition not found.", Kind: ErrorKind.NotFound);

        var hasAccess = await accessChecker.CanAccessPatientRecord(
            request.CallerUserId, request.CallerRole, condition.PatientProfileId, ct);

        if (!hasAccess)
            return new UpdateConditionResult(false, Error: "Access denied.", Kind: ErrorKind.Forbidden);

        condition.Severity = request.Severity;
        condition.Status = request.Status;
        condition.ClinicalNotes = request.ClinicalNotes;

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Condition {ConditionId} updated by {CallerUserId}",
            request.ConditionId, request.CallerUserId);

        try
        {
            var patient = await db.PatientProfiles
                .AsNoTracking()
                .FirstAsync(p => p.Id == condition.PatientProfileId, ct);

            await eventBus.PublishAsync(Topics.MedicalRecordUpdated, new MedicalRecordUpdatedEvent(
                condition.PatientProfileId, patient.UserId, "Condition", "Updated",
                request.CallerUserId, DateTime.UtcNow), ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to publish event for condition {ConditionId}", request.ConditionId);
        }

        return new UpdateConditionResult(true);
    }
}

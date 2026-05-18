using Halen.Application.Common;
using Halen.Application.Events;
using Halen.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Halen.Application.MedicalRecords.Commands;

public class UpdateFamilyHistoryCommandHandler(
    IAppDbContext db,
    ITenantContext tenantContext,
    IEventBus eventBus,
    IRecordAccessChecker accessChecker,
    ILogger<UpdateFamilyHistoryCommandHandler> logger
) : IRequestHandler<UpdateFamilyHistoryCommand, UpdateFamilyHistoryResult>
{
    public async Task<UpdateFamilyHistoryResult> Handle(UpdateFamilyHistoryCommand request, CancellationToken ct)
    {
        var entry = await db.PatientFamilyHistories
            .FirstOrDefaultAsync(f => f.Id == request.FamilyHistoryId, ct);

        if (entry is null)
            return new UpdateFamilyHistoryResult(false, Error: "Family history entry not found.", Kind: ErrorKind.NotFound);

        var hasAccess = await accessChecker.CanAccessPatientRecord(
            request.CallerUserId, request.CallerRole, entry.PatientProfileId, ct);

        if (!hasAccess)
            return new UpdateFamilyHistoryResult(false, Error: "Access denied.", Kind: ErrorKind.Forbidden);

        entry.ConditionName = request.ConditionName;
        entry.AgeAtOnset = request.AgeAtOnset;
        entry.Notes = request.Notes;

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Family history {FamilyHistoryId} updated by {CallerUserId}",
            request.FamilyHistoryId, request.CallerUserId);

        try
        {
            var patient = await db.PatientProfiles
                .AsNoTracking()
                .FirstAsync(p => p.Id == entry.PatientProfileId, ct);

            await eventBus.PublishAsync(Topics.MedicalRecordUpdated, new MedicalRecordUpdatedEvent(
                entry.PatientProfileId, patient.UserId, "FamilyHistory", "Updated",
                request.CallerUserId, DateTime.UtcNow), ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to publish event for family history {FamilyHistoryId}", request.FamilyHistoryId);
        }

        return new UpdateFamilyHistoryResult(true);
    }
}

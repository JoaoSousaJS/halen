using Halen.Application.Common;
using Halen.Application.Events;
using Halen.Application.Interfaces;
using Halen.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Halen.Application.MedicalRecords.Commands;

public class AddFamilyHistoryCommandHandler(
    IAppDbContext db,
    ITenantContext tenantContext,
    IEventBus eventBus,
    IRecordAccessChecker accessChecker,
    ILogger<AddFamilyHistoryCommandHandler> logger
) : IRequestHandler<AddFamilyHistoryCommand, AddFamilyHistoryResult>
{
    public async Task<AddFamilyHistoryResult> Handle(AddFamilyHistoryCommand request, CancellationToken ct)
    {
        var hasAccess = await accessChecker.CanAccessPatientRecord(
            request.CallerUserId, request.CallerRole, request.PatientProfileId, ct);

        if (!hasAccess)
            return new AddFamilyHistoryResult(false, Error: "Access denied.", Kind: ErrorKind.Forbidden);

        var entry = new PatientFamilyHistory
        {
            PatientProfileId = request.PatientProfileId,
            Relationship = request.Relationship,
            ConditionName = request.ConditionName,
            AgeAtOnset = request.AgeAtOnset,
            Notes = request.Notes,
            AddedByUserId = request.CallerUserId,
            ClinicId = tenantContext.ClinicId,
        };

        db.PatientFamilyHistories.Add(entry);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Family history {FamilyHistoryId} added for patient {PatientProfileId} by {CallerUserId}",
            entry.Id, request.PatientProfileId, request.CallerUserId);

        try
        {
            var patient = await db.PatientProfiles
                .AsNoTracking()
                .FirstAsync(p => p.Id == request.PatientProfileId, ct);

            await eventBus.PublishAsync(Topics.MedicalRecordUpdated, new MedicalRecordUpdatedEvent(
                request.PatientProfileId, patient.UserId, "FamilyHistory", "Added",
                request.CallerUserId, DateTime.UtcNow), ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to publish event for family history {FamilyHistoryId}", entry.Id);
        }

        return new AddFamilyHistoryResult(true, entry.Id);
    }
}

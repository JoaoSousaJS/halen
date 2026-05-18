using Halen.Application.Common;
using Halen.Application.Events;
using Halen.Application.Interfaces;
using Halen.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Halen.Application.MedicalRecords.Commands;

public class AddVitalCommandHandler(
    IAppDbContext db,
    ITenantContext tenantContext,
    IEventBus eventBus,
    IRecordAccessChecker accessChecker,
    ILogger<AddVitalCommandHandler> logger
) : IRequestHandler<AddVitalCommand, AddVitalResult>
{
    public async Task<AddVitalResult> Handle(AddVitalCommand request, CancellationToken ct)
    {
        var hasAccess = await accessChecker.CanAccessPatientRecord(
            request.CallerUserId, request.CallerRole, request.PatientProfileId, ct);

        if (!hasAccess)
            return new AddVitalResult(false, Error: "Access denied.", Kind: ErrorKind.Forbidden);

        // Validate MeasuredAt is not more than 5 minutes in the future
        if (request.MeasuredAt > DateTime.UtcNow.AddMinutes(5))
            return new AddVitalResult(false, Error: "Measured time cannot be in the future.", Kind: ErrorKind.Validation);

        var vital = new PatientVital
        {
            PatientProfileId = request.PatientProfileId,
            VitalType = request.VitalType,
            Value = request.Value,
            SecondaryValue = request.SecondaryValue,
            Unit = request.Unit,
            MeasuredAt = request.MeasuredAt,
            Source = request.Source,
            Notes = request.Notes,
            AddedByUserId = request.CallerUserId,
            ClinicId = tenantContext.ClinicId,
        };

        db.PatientVitals.Add(vital);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Vital {VitalId} ({VitalType}) added for patient {PatientProfileId} by {CallerUserId}",
            vital.Id, request.VitalType, request.PatientProfileId, request.CallerUserId);

        try
        {
            var patient = await db.PatientProfiles
                .AsNoTracking()
                .FirstAsync(p => p.Id == request.PatientProfileId, ct);

            await eventBus.PublishAsync(Topics.MedicalRecordUpdated, new MedicalRecordUpdatedEvent(
                request.PatientProfileId, patient.UserId, "Vital", "Added",
                request.CallerUserId, DateTime.UtcNow), ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to publish event for vital {VitalId}", vital.Id);
        }

        return new AddVitalResult(true, vital.Id);
    }
}

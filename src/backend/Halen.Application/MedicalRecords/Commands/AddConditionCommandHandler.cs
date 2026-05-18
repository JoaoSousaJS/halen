using Halen.Application.Common;
using Halen.Application.Events;
using Halen.Application.Interfaces;
using Halen.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Halen.Application.MedicalRecords.Commands;

public class AddConditionCommandHandler(
    IAppDbContext db,
    ITenantContext tenantContext,
    IEventBus eventBus,
    IRecordAccessChecker accessChecker,
    ILogger<AddConditionCommandHandler> logger
) : IRequestHandler<AddConditionCommand, AddConditionResult>
{
    public async Task<AddConditionResult> Handle(AddConditionCommand request, CancellationToken ct)
    {
        var patient = await db.PatientProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PatientProfileId, ct);

        if (patient is null)
            return new AddConditionResult(false, Error: "Patient not found.", Kind: ErrorKind.NotFound);

        var hasAccess = await accessChecker.CanAccessPatientRecord(
            request.CallerUserId, request.CallerRole, request.PatientProfileId, ct);

        if (!hasAccess)
            return new AddConditionResult(false, Error: "Access denied.", Kind: ErrorKind.Forbidden);

        if (request.LinkedAppointmentId.HasValue)
        {
            var appointmentExists = await db.Appointments
                .AsNoTracking()
                .AnyAsync(a => a.Id == request.LinkedAppointmentId.Value, ct);

            if (!appointmentExists)
                return new AddConditionResult(false, Error: "Appointment not found.", Kind: ErrorKind.NotFound);
        }

        var condition = new PatientCondition
        {
            PatientProfileId = request.PatientProfileId,
            IcdCode = request.IcdCode,
            IcdDescription = request.IcdDescription,
            DateOfOnset = request.DateOfOnset,
            Severity = request.Severity,
            Status = request.Status,
            ClinicalNotes = request.ClinicalNotes,
            AddedByUserId = request.CallerUserId,
            LinkedAppointmentId = request.LinkedAppointmentId,
            ClinicId = tenantContext.ClinicId,
        };

        db.PatientConditions.Add(condition);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Condition {ConditionId} added for patient {PatientProfileId} by {CallerUserId}",
            condition.Id, request.PatientProfileId, request.CallerUserId);

        try
        {
            await eventBus.PublishAsync(Topics.MedicalRecordUpdated, new MedicalRecordUpdatedEvent(
                request.PatientProfileId, patient.UserId, "Condition", "Added",
                request.CallerUserId, DateTime.UtcNow), ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to publish event for condition {ConditionId}", condition.Id);
        }

        return new AddConditionResult(true, condition.Id);
    }
}

using Halen.Application.Common;
using Halen.Application.Events;
using Halen.Application.Interfaces;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Halen.Application.Prescriptions.Commands;

public class CancelPrescriptionCommandHandler(
    IAppDbContext db,
    IEventBus eventBus,
    ILogger<CancelPrescriptionCommandHandler> logger
) : IRequestHandler<CancelPrescriptionCommand, CancelPrescriptionResult>
{
    public async Task<CancelPrescriptionResult> Handle(CancelPrescriptionCommand request, CancellationToken ct)
    {
        var prescription = await db.Prescriptions
            .Include(p => p.Doctor).ThenInclude(d => d.User)
            .Include(p => p.Patient)
            .FirstOrDefaultAsync(p => p.Id == request.PrescriptionId, ct);

        if (prescription is null)
            return new CancelPrescriptionResult(false, "Prescription not found.", ErrorKind.NotFound);

        if (prescription.Doctor.UserId != request.DoctorUserId)
            return new CancelPrescriptionResult(false, "You can only cancel your own prescriptions.", ErrorKind.Forbidden);

        if (prescription.Status != PrescriptionStatus.Active)
            return new CancelPrescriptionResult(false, $"Cannot cancel a prescription with status '{prescription.Status}'.");

        prescription.Status = PrescriptionStatus.Cancelled;
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Prescription {PrescriptionId} cancelled by doctor {DoctorUserId}",
            prescription.Id, request.DoctorUserId);

        try
        {
            await eventBus.PublishAsync(Topics.PrescriptionCancelled, new PrescriptionCancelledEvent(
                prescription.Id,
                request.DoctorUserId,
                prescription.Patient.UserId,
                prescription.DrugName,
                $"Dr. {prescription.Doctor.User.LastName}",
                DateTime.UtcNow), ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to publish cancelled event for prescription {PrescriptionId}", prescription.Id);
        }

        return new CancelPrescriptionResult(true);
    }
}

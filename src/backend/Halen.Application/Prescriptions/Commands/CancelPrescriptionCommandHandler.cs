using Halen.Application.Common;
using Halen.Application.Interfaces;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Halen.Application.Prescriptions.Commands;

public class CancelPrescriptionCommandHandler(
    IAppDbContext db,
    ILogger<CancelPrescriptionCommandHandler> logger
) : IRequestHandler<CancelPrescriptionCommand, CancelPrescriptionResult>
{
    public async Task<CancelPrescriptionResult> Handle(CancelPrescriptionCommand request, CancellationToken ct)
    {
        var prescription = await db.Prescriptions
            .Include(p => p.Doctor)
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

        return new CancelPrescriptionResult(true);
    }
}

using Halen.Application.Common;
using Halen.Application.Events;
using Halen.Application.Interfaces;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Halen.Application.Prescriptions.Commands;

public class IssuePrescriptionCommandHandler(
    IAppDbContext db,
    IEventBus eventBus,
    ILogger<IssuePrescriptionCommandHandler> logger
) : IRequestHandler<IssuePrescriptionCommand, IssuePrescriptionResult>
{
    public async Task<IssuePrescriptionResult> Handle(IssuePrescriptionCommand request, CancellationToken ct)
    {
        var doctorProfile = await db.DoctorProfiles
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.UserId == request.DoctorUserId, ct);

        if (doctorProfile is null)
            return new IssuePrescriptionResult(false, Error: "Doctor profile not found.", Kind: ErrorKind.NotFound);

        if (doctorProfile.KycStatus != KycStatus.Approved)
            return new IssuePrescriptionResult(false, Error: "Doctor is not yet approved to issue prescriptions.", Kind: ErrorKind.Forbidden);

        var patient = await db.PatientProfiles
            .FirstOrDefaultAsync(p => p.Id == request.PatientId, ct);

        if (patient is null)
            return new IssuePrescriptionResult(false, Error: "Patient not found.", Kind: ErrorKind.NotFound);

        var prescription = new Prescription
        {
            DoctorId = doctorProfile.Id,
            PatientId = request.PatientId,
            DrugName = request.DrugName,
            Dosage = request.Dosage,
            Frequency = request.Frequency,
            RefillsRemaining = request.RefillsRemaining,
            PharmacyName = request.PharmacyName,
            Status = PrescriptionStatus.Active,
        };

        db.Prescriptions.Add(prescription);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Prescription {PrescriptionId} issued by doctor {DoctorUserId} for patient {PatientId}",
            prescription.Id, request.DoctorUserId, request.PatientId);

        try
        {
            await eventBus.PublishAsync(Topics.PrescriptionIssued, new PrescriptionIssuedEvent(
                prescription.Id,
                request.DoctorUserId,
                patient.UserId,
                request.DrugName,
                $"Dr. {doctorProfile.User.LastName}",
                DateTime.UtcNow), ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to publish issued event for prescription {PrescriptionId}", prescription.Id);
        }

        return new IssuePrescriptionResult(true, prescription.Id);
    }
}

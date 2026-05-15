using Halen.Application.Common;
using Halen.Application.Interfaces;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Halen.Application.Prescriptions.Commands;

public class IssuePrescriptionCommandHandler(
    IAppDbContext db,
    ILogger<IssuePrescriptionCommandHandler> logger
) : IRequestHandler<IssuePrescriptionCommand, IssuePrescriptionResult>
{
    public async Task<IssuePrescriptionResult> Handle(IssuePrescriptionCommand request, CancellationToken ct)
    {
        var doctorProfile = await db.DoctorProfiles
            .FirstOrDefaultAsync(d => d.UserId == request.DoctorUserId, ct);

        if (doctorProfile is null)
            return new IssuePrescriptionResult(false, Error: "Doctor profile not found.", Kind: ErrorKind.NotFound);

        var patientExists = await db.PatientProfiles.AnyAsync(p => p.Id == request.PatientId, ct);
        if (!patientExists)
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

        return new IssuePrescriptionResult(true, prescription.Id);
    }
}

using Halen.Application.Common;
using Halen.Application.Events;
using Halen.Application.Interfaces;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Halen.Application.Doctor.Commands;

public class SubmitKycDocumentsCommandHandler(
    IAppDbContext db,
    IEventBus eventBus,
    ILogger<SubmitKycDocumentsCommandHandler> logger
) : IRequestHandler<SubmitKycDocumentsCommand, SubmitKycDocumentsResult>
{
    public async Task<SubmitKycDocumentsResult> Handle(SubmitKycDocumentsCommand request, CancellationToken ct)
    {
        var doctor = await db.DoctorProfiles
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.UserId == request.UserId, ct);

        if (doctor is null)
            return new SubmitKycDocumentsResult(false, "Doctor profile not found.", ErrorKind.NotFound);

        if (doctor.KycStatus is not (KycStatus.NotSubmitted or KycStatus.Rejected))
            return new SubmitKycDocumentsResult(false, "Documents already submitted and pending review.", ErrorKind.Validation);

        var submittedTypes = request.Documents.Select(d => d.DocumentType).ToHashSet();
        var requiredTypes = new[] { KycDocumentType.LicensePhoto, KycDocumentType.MedicalCertificate, KycDocumentType.IdentityProof };
        var missing = requiredTypes.Where(t => !submittedTypes.Contains(t)).ToList();

        if (missing.Count > 0)
            return new SubmitKycDocumentsResult(false, $"Missing required documents: {string.Join(", ", missing)}.", ErrorKind.Validation);

        var existingDocs = await db.KycDocuments
            .Where(d => d.DoctorProfileId == doctor.Id)
            .ToListAsync(ct);
        if (existingDocs.Count > 0)
            db.KycDocuments.RemoveRange(existingDocs);

        foreach (var doc in request.Documents)
        {
            db.KycDocuments.Add(new KycDocument
            {
                DoctorProfileId = doctor.Id,
                DocumentType = doc.DocumentType,
                FileName = doc.FileName,
                FilePath = doc.FilePath,
                ContentType = doc.ContentType,
                FileSizeBytes = doc.FileSizeBytes,
                UploadedAt = DateTime.UtcNow,
            });
        }

        doctor.KycStatus = KycStatus.Submitted;
        doctor.KycSubmittedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("KYC documents submitted by doctor {DoctorUserId}", request.UserId);

        try
        {
            await eventBus.PublishAsync(Topics.KycSubmitted, new KycDocumentsSubmittedEvent(
                doctor.Id,
                request.UserId,
                $"Dr. {doctor.User.LastName}"), ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to publish KYC submitted event for doctor {DoctorUserId}", request.UserId);
        }

        return new SubmitKycDocumentsResult(true);
    }
}

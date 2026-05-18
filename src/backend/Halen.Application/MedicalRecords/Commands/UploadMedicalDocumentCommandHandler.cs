using Halen.Application.Common;
using Halen.Application.Interfaces;
using Halen.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Halen.Application.MedicalRecords.Commands;

public class UploadMedicalDocumentCommandHandler(
    IAppDbContext db,
    ITenantContext tenantContext,
    IRecordAccessChecker accessChecker,
    IFileStorage fileStorage,
    ILogger<UploadMedicalDocumentCommandHandler> logger
) : IRequestHandler<UploadMedicalDocumentCommand, UploadMedicalDocumentResult>
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/jpeg",
        "image/png"
    };

    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    public async Task<UploadMedicalDocumentResult> Handle(
        UploadMedicalDocumentCommand request, CancellationToken ct)
    {
        var hasAccess = await accessChecker.CanAccessPatientRecord(
            request.CallerUserId, request.CallerRole, request.PatientProfileId, ct);

        if (!hasAccess)
            return new UploadMedicalDocumentResult(false,
                Error: "You do not have access to this patient's records.",
                Kind: ErrorKind.Forbidden);

        if (!AllowedContentTypes.Contains(request.ContentType))
            return new UploadMedicalDocumentResult(false,
                Error: "Invalid content type. Allowed types: PDF, JPEG, PNG.",
                Kind: ErrorKind.Validation);

        if (request.FileSizeBytes > MaxFileSizeBytes)
            return new UploadMedicalDocumentResult(false,
                Error: "File size exceeds the 10 MB limit.",
                Kind: ErrorKind.Validation);

        var safeFileName = Path.GetFileName(request.FileName);

        var filePath = await fileStorage.SaveAsync(
            $"medical-documents/{request.PatientProfileId}",
            safeFileName,
            request.FileContent,
            ct);

        var document = new MedicalDocument
        {
            ClinicId = tenantContext.ClinicId,
            PatientProfileId = request.PatientProfileId,
            DocumentType = request.DocumentType,
            Title = request.Title,
            Description = request.Description,
            FileName = safeFileName,
            FilePath = filePath,
            ContentType = request.ContentType,
            FileSizeBytes = request.FileSizeBytes,
            UploadedByUserId = request.CallerUserId,
            LinkedAppointmentId = request.LinkedAppointmentId,
        };

        db.MedicalDocuments.Add(document);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Document {DocumentId} uploaded by {UserId} for patient {PatientProfileId}",
            document.Id, request.CallerUserId, request.PatientProfileId);

        return new UploadMedicalDocumentResult(true, document.Id);
    }
}

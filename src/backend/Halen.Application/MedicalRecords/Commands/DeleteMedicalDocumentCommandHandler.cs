using Halen.Application.Common;
using Halen.Application.Interfaces;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Halen.Application.MedicalRecords.Commands;

public class DeleteMedicalDocumentCommandHandler(
    IAppDbContext db,
    IFileStorage fileStorage,
    ILogger<DeleteMedicalDocumentCommandHandler> logger
) : IRequestHandler<DeleteMedicalDocumentCommand, DeleteMedicalDocumentResult>
{
    public async Task<DeleteMedicalDocumentResult> Handle(
        DeleteMedicalDocumentCommand request, CancellationToken ct)
    {
        var document = await db.MedicalDocuments
            .FirstOrDefaultAsync(d => d.Id == request.DocumentId, ct);

        if (document is null)
            return new DeleteMedicalDocumentResult(false,
                Error: "Document not found.",
                Kind: ErrorKind.NotFound);

        var isUploader = document.UploadedByUserId == request.CallerUserId;
        var isAdmin = request.CallerRole == UserRole.PlatformAdmin;

        if (!isUploader && !isAdmin)
            return new DeleteMedicalDocumentResult(false,
                Error: "Only the uploader or a platform admin can delete this document.",
                Kind: ErrorKind.Forbidden);

        await fileStorage.DeleteAsync(document.FilePath, ct);

        db.MedicalDocuments.Remove(document);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Document {DocumentId} deleted by {UserId}",
            request.DocumentId, request.CallerUserId);

        return new DeleteMedicalDocumentResult(true);
    }
}

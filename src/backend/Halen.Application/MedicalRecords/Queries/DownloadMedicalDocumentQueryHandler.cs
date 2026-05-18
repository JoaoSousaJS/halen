using Halen.Application.Common;
using Halen.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Halen.Application.MedicalRecords.Queries;

public class DownloadMedicalDocumentQueryHandler(
    IAppDbContext db,
    IRecordAccessChecker accessChecker,
    IFileStorage fileStorage
) : IRequestHandler<DownloadMedicalDocumentQuery, DownloadMedicalDocumentResult>
{
    public async Task<DownloadMedicalDocumentResult> Handle(DownloadMedicalDocumentQuery request, CancellationToken ct)
    {
        var document = await db.MedicalDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == request.DocumentId, ct);

        if (document is null)
            return new DownloadMedicalDocumentResult(false, Error: "Document not found", Kind: ErrorKind.NotFound);

        var canAccess = await accessChecker.CanAccessPatientRecord(
            request.CallerUserId, request.CallerRole, document.PatientProfileId, ct);

        if (!canAccess)
            return new DownloadMedicalDocumentResult(false, Error: "Access denied", Kind: ErrorKind.Forbidden);

        var stream = await fileStorage.ReadAsync(document.FilePath, ct);

        return new DownloadMedicalDocumentResult(true, stream, document.FileName, document.ContentType);
    }
}

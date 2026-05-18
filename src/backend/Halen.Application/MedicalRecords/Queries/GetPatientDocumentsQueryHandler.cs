using Halen.Application.Common;
using Halen.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Halen.Application.MedicalRecords.Queries;

public class GetPatientDocumentsQueryHandler(
    IAppDbContext db,
    IRecordAccessChecker accessChecker
) : IRequestHandler<GetPatientDocumentsQuery, GetPatientDocumentsResult>
{
    public async Task<GetPatientDocumentsResult> Handle(GetPatientDocumentsQuery request, CancellationToken ct)
    {
        var canAccess = await accessChecker.CanAccessPatientRecord(
            request.CallerUserId, request.CallerRole, request.PatientProfileId, ct);

        if (!canAccess)
            return new GetPatientDocumentsResult(false, Error: "Access denied", Kind: ErrorKind.Forbidden);

        var query = db.MedicalDocuments
            .AsNoTracking()
            .Where(d => d.PatientProfileId == request.PatientProfileId);

        if (request.FilterType.HasValue)
            query = query.Where(d => d.DocumentType == request.FilterType.Value);

        var documents = await query
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new DocumentDto(
                d.Id,
                d.DocumentType.ToString(),
                d.Title,
                d.Description,
                d.FileName,
                d.ContentType,
                d.FileSizeBytes,
                d.UploadedByUser.FirstName + " " + d.UploadedByUser.LastName,
                d.CreatedAt))
            .ToArrayAsync(ct);

        return new GetPatientDocumentsResult(true, documents);
    }
}

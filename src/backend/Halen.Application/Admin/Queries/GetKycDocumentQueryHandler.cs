using Halen.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Halen.Application.Admin.Queries;

public class GetKycDocumentQueryHandler(
    IAppDbContext db
) : IRequestHandler<GetKycDocumentQuery, KycDocumentFileResult?>
{
    public async Task<KycDocumentFileResult?> Handle(GetKycDocumentQuery request, CancellationToken ct)
    {
        var doc = await db.KycDocuments
            .AsNoTracking()
            .Where(d => d.Id == request.DocumentId)
            .Select(d => new KycDocumentFileResult(d.FilePath, d.ContentType, d.FileName))
            .FirstOrDefaultAsync(ct);

        return doc;
    }
}

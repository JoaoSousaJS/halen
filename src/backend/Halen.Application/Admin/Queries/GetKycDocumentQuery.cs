using MediatR;

namespace Halen.Application.Admin.Queries;

public record GetKycDocumentQuery(Guid DocumentId) : IRequest<KycDocumentFileResult?>;

public record KycDocumentFileResult(string FilePath, string ContentType, string FileName);

using Halen.Application.Common;
using Halen.Domain.Enums;
using MediatR;

namespace Halen.Application.Doctor.Commands;

public record SubmitKycDocumentsCommand(
    Guid UserId,
    List<KycDocumentUpload> Documents
) : IRequest<SubmitKycDocumentsResult>;

public record KycDocumentUpload(
    KycDocumentType DocumentType,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    string FilePath);

public record SubmitKycDocumentsResult(
    bool Success,
    string? Error = null,
    ErrorKind? Kind = null);

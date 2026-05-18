using Halen.Application.Common;
using Halen.Domain.Enums;
using MediatR;

namespace Halen.Application.MedicalRecords.Queries;

public record DownloadMedicalDocumentQuery(
    Guid CallerUserId,
    UserRole CallerRole,
    Guid DocumentId
) : IRequest<DownloadMedicalDocumentResult>;

public record DownloadMedicalDocumentResult(
    bool Success,
    Stream? FileStream = null,
    string? FileName = null,
    string? ContentType = null,
    string? Error = null,
    ErrorKind? Kind = null);

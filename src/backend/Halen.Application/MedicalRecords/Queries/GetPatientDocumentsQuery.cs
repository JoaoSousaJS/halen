using Halen.Application.Common;
using Halen.Domain.Enums;
using MediatR;

namespace Halen.Application.MedicalRecords.Queries;

public record GetPatientDocumentsQuery(
    Guid CallerUserId,
    UserRole CallerRole,
    Guid PatientProfileId,
    MedicalDocumentType? FilterType
) : IRequest<GetPatientDocumentsResult>;

public record GetPatientDocumentsResult(
    bool Success,
    DocumentDto[] Documents = default!,
    string? Error = null,
    ErrorKind? Kind = null)
{
    public DocumentDto[] Documents { get; init; } = Documents ?? [];
}

public record DocumentDto(
    Guid Id,
    string DocumentType,
    string Title,
    string? Description,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    string UploadedBy,
    DateTime CreatedAt);

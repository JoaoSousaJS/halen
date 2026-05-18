using Halen.Application.Common;
using Halen.Domain.Enums;
using MediatR;

namespace Halen.Application.MedicalRecords.Commands;

public record UploadMedicalDocumentCommand(
    Guid CallerUserId,
    UserRole CallerRole,
    Guid PatientProfileId,
    MedicalDocumentType DocumentType,
    string Title,
    string? Description,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    Stream FileContent,
    Guid? LinkedAppointmentId
) : IRequest<UploadMedicalDocumentResult>;

public record UploadMedicalDocumentResult(
    bool Success,
    Guid? DocumentId = null,
    string? Error = null,
    ErrorKind? Kind = null);

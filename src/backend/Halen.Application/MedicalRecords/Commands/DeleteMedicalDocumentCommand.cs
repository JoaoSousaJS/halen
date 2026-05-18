using Halen.Application.Common;
using Halen.Domain.Enums;
using MediatR;

namespace Halen.Application.MedicalRecords.Commands;

public record DeleteMedicalDocumentCommand(
    Guid CallerUserId,
    UserRole CallerRole,
    Guid DocumentId
) : IRequest<DeleteMedicalDocumentResult>;

public record DeleteMedicalDocumentResult(
    bool Success,
    string? Error = null,
    ErrorKind? Kind = null);

using Halen.Application.Common;
using Halen.Domain.Enums;
using MediatR;

namespace Halen.Application.MedicalRecords.Commands;

public record GrantRecordAccessCommand(
    Guid CallerUserId,
    Guid PatientProfileId,
    Guid GrantToUserId,
    RecordAccessLevel AccessLevel,
    string? Reason
) : IRequest<GrantRecordAccessResult>;

public record GrantRecordAccessResult(
    bool Success,
    Guid? AccessId = null,
    string? Error = null,
    ErrorKind? Kind = null);

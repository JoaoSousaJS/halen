using Halen.Application.Common;
using MediatR;

namespace Halen.Application.MedicalRecords.Commands;

public record RevokeRecordAccessCommand(
    Guid CallerUserId,
    Guid RecordAccessId,
    string? Reason
) : IRequest<RevokeRecordAccessResult>;

public record RevokeRecordAccessResult(
    bool Success,
    string? Error = null,
    ErrorKind? Kind = null);

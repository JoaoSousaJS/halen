using Halen.Application.Common;
using Halen.Application.Interfaces;
using Halen.Domain.Enums;
using MediatR;

namespace Halen.Application.Admin.Commands;

public record ReviewKycCommand(
    Guid AdminUserId,
    Guid DoctorProfileId,
    KycDecision Decision,
    string? RejectionReason
) : IRequest<ReviewKycResult>, IAuditableCommand
{
    Guid IAuditableCommand.ActorId => AdminUserId;
    string? IAuditableCommand.AuditTargetId => DoctorProfileId.ToString();
}


public record ReviewKycResult(
    bool Success,
    string? Error = null,
    ErrorKind? Kind = null);

using Halen.Application.Common;
using Halen.Domain.Enums;
using MediatR;

namespace Halen.Application.Admin.Commands;

public record ReviewKycCommand(
    Guid AdminUserId,
    Guid DoctorProfileId,
    KycDecision Decision,
    string? RejectionReason
) : IRequest<ReviewKycResult>;

public record ReviewKycResult(
    bool Success,
    string? Error = null,
    ErrorKind? Kind = null);

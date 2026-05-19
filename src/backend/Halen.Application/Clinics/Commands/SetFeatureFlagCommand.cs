using Halen.Application.Common;
using Halen.Application.Interfaces;
using MediatR;

namespace Halen.Application.Clinics.Commands;

public record SetFeatureFlagCommand(Guid ClinicId, string FeatureKey, bool IsEnabled)
    : IRequest<SetFeatureFlagResult>, IAuditableCommand
{
    string? IAuditableCommand.AuditTargetId => ClinicId.ToString();
}


public record SetFeatureFlagResult(bool Success, string? Error = null, ErrorKind? Kind = null);

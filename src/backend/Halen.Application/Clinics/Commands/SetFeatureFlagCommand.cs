using Halen.Application.Common;
using MediatR;

namespace Halen.Application.Clinics.Commands;

public record SetFeatureFlagCommand(Guid ClinicId, string FeatureKey, bool IsEnabled)
    : IRequest<SetFeatureFlagResult>;

public record SetFeatureFlagResult(bool Success, string? Error = null, ErrorKind? Kind = null);

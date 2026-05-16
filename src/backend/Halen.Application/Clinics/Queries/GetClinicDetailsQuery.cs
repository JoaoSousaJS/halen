using Halen.Application.Common;
using MediatR;

namespace Halen.Application.Clinics.Queries;

public record GetClinicDetailsQuery(Guid ClinicId) : IRequest<GetClinicDetailsResult>;

public record GetClinicDetailsResult(
    bool Success,
    ClinicDetailsDto? Clinic = null,
    string? Error = null,
    ErrorKind? Kind = null
);

public record ClinicDetailsDto(
    Guid Id,
    string Name,
    string Slug,
    bool IsActive,
    int UserCount,
    List<FeatureFlagDto> FeatureFlags,
    DateTime CreatedAt
);

public record FeatureFlagDto(string Key, bool IsEnabled);

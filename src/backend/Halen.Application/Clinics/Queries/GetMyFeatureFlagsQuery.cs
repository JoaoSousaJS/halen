using MediatR;

namespace Halen.Application.Clinics.Queries;

public record GetMyFeatureFlagsQuery : IRequest<List<FeatureFlagDto>>;

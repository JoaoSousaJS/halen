using MediatR;

namespace Halen.Application.Analytics.Queries;

public record GetGeographyAnalyticsQuery(string Period) : IRequest<GeographyAnalyticsResult>;

public record GeographyAnalyticsResult(
    RegionDto[] Regions,
    CohortRetentionDto Retention);

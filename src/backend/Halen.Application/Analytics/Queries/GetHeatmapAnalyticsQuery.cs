using MediatR;

namespace Halen.Application.Analytics.Queries;

public record GetHeatmapAnalyticsQuery(string Period) : IRequest<HeatmapAnalyticsResult>;

public record HeatmapAnalyticsResult(
    int[][] Grid,
    SpecialtySeasonDto[] SpecialtySeries,
    SpecialtyWaitDto[] AvgWaitBySpecialty);

using MediatR;

namespace Halen.Application.Analytics.Queries;

public record GetAnalyticsOverviewQuery(string Period) : IRequest<AnalyticsOverviewResult>;

public record AnalyticsOverviewResult(
    KpiDto AppointmentKpi,
    DecimalKpiDto RevenueKpi,
    KpiDto ActiveUsersKpi,
    RateKpiDto NoShowKpi,
    TimeSeriesDto AppointmentSeries,
    BarSeriesDto RevenueSeries,
    FunnelStageDto[] Funnel,
    ActiveUsersDto ActiveUsers,
    ClinicBreakdownDto[] ClinicBreakdown,
    SpecialtyMixDto[] SpecialtyMix);

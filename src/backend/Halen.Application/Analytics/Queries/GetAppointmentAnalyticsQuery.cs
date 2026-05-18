using MediatR;

namespace Halen.Application.Analytics.Queries;

public record GetAppointmentAnalyticsQuery(string Period) : IRequest<AppointmentAnalyticsResult>;

public record AppointmentAnalyticsResult(
    KpiDto BookedKpi,
    KpiDto CompletedKpi,
    KpiDto CancelledKpi,
    DecimalKpiDto AvgLeadTimeKpi,
    TimeSeriesDto DailySeries,
    DayOfWeekDto[] ByDayOfWeek,
    HourOfDayDto[] ByHourOfDay);

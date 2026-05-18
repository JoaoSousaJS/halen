using MediatR;

namespace Halen.Application.Analytics.Queries;

public record GetRevenueAnalyticsQuery(string Period) : IRequest<RevenueAnalyticsResult>;

public record RevenueAnalyticsResult(
    DecimalKpiDto GrossKpi,
    DecimalKpiDto NetKpi,
    DecimalKpiDto RefundsKpi,
    DecimalKpiDto ArpuKpi,
    WeeklySpecialtyDto[] WeeklyBySpecialty,
    PaymentStatusDto[] PaymentStatusBreakdown,
    ClinicRevenueDto[] ClinicRevenue);

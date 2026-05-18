namespace Halen.Application.Analytics.Queries;

public record KpiDto(int Total, decimal DeltaPct, decimal[] Sparkline);
public record DecimalKpiDto(decimal Value, decimal DeltaPct, decimal[] Sparkline);
public record RateKpiDto(decimal Rate, decimal DeltaPct, decimal[] Sparkline);
public record TimeSeriesDto(string[] Labels, int[] Current, int[] Previous);
public record BarSeriesDto(string[] Labels, decimal[] Values);
public record FunnelStageDto(string Label, int Value);
public record ActiveUsersDto(int Dau, int Wau, int Mau, decimal DauDelta, decimal WauDelta, decimal MauDelta, decimal Stickiness);
public record ClinicBreakdownDto(string Name, int Value);
public record SpecialtyMixDto(string Label, int Value);

public record DayOfWeekDto(string Day, decimal Ratio);
public record HourOfDayDto(int Hour, int Count);

public record WeeklySpecialtyDto(string Week, SpecialtyAmountDto[] Segments);
public record SpecialtyAmountDto(string Specialty, decimal Amount);
public record PaymentStatusDto(string Label, decimal Amount, decimal Percentage);
public record ClinicRevenueDto(string Name, int Consults, decimal Arpu, decimal Revenue, decimal DeltaPct);

public record MonthDataPointDto(string Month, int Count);
public record SpecialtySeasonDto(string Specialty, MonthDataPointDto[] DataPoints);
public record SpecialtyWaitDto(string Specialty, decimal Days);

public record RankedDoctorDto(string Name, string Specialty, int Consults, decimal CompletionPct, decimal Rating, decimal Revenue, decimal[] Trend, string? Badge);
public record TopRatedDoctorDto(string Name, decimal Rating, int ReviewCount, string Specialty);
public record NeedsAttentionDto(string Name, string Message, string Severity);

public record RegionDto(string Name, int Consults, decimal DeltaPct, bool IsTop);
public record CohortRetentionDto(CohortWeekDto[] Cohorts);
public record CohortWeekDto(string CohortLabel, decimal[] Weeks);

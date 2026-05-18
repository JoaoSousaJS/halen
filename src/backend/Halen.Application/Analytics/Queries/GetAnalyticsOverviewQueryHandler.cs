using Halen.Application.Interfaces;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Halen.Application.Analytics.Queries;

public class GetAnalyticsOverviewQueryHandler(IAppDbContext db)
    : IRequestHandler<GetAnalyticsOverviewQuery, AnalyticsOverviewResult>
{
    private static readonly AppointmentStatus[] ActiveStatuses =
        [AppointmentStatus.Scheduled, AppointmentStatus.InProgress, AppointmentStatus.Completed];

    public async Task<AnalyticsOverviewResult> Handle(GetAnalyticsOverviewQuery request, CancellationToken ct)
    {
        var (start, end, prevStart, prevEnd) = AnalyticsPeriod.ParsePeriod(request.Period);
        var periodDays = (int)Math.Ceiling((end - start).TotalDays);

        var appointmentKpi = await BuildAppointmentKpiAsync(start, end, prevStart, prevEnd, periodDays, ct);
        var revenueKpi = await BuildRevenueKpiAsync(start, end, prevStart, prevEnd, periodDays, ct);
        var activeUsersKpi = await BuildActiveUsersKpiAsync(start, end, prevStart, prevEnd, periodDays, ct);
        var noShowKpi = await BuildNoShowKpiAsync(start, end, prevStart, prevEnd, periodDays, ct);
        var appointmentSeries = await BuildAppointmentSeriesAsync(start, end, prevStart, prevEnd, ct);
        var revenueSeries = await BuildRevenueSeriesAsync(end, ct);
        var funnel = await BuildFunnelAsync(start, end, ct);
        var activeUsersMetrics = await BuildActiveUsersMetricsAsync(end, ct);
        var clinicBreakdown = await BuildClinicBreakdownAsync(start, end, ct);
        var specialtyMix = await BuildSpecialtyMixAsync(start, end, ct);

        return new AnalyticsOverviewResult(
            appointmentKpi,
            revenueKpi,
            activeUsersKpi,
            noShowKpi,
            appointmentSeries,
            revenueSeries,
            funnel,
            activeUsersMetrics,
            clinicBreakdown,
            specialtyMix);
    }

    private async Task<KpiDto> BuildAppointmentKpiAsync(
        DateTime start, DateTime end, DateTime prevStart, DateTime prevEnd, int periodDays, CancellationToken ct)
    {
        var current = await db.Appointments.AsNoTracking()
            .Where(a => a.ScheduledAt >= start && a.ScheduledAt < end && a.Status != AppointmentStatus.Cancelled)
            .CountAsync(ct);

        var previous = await db.Appointments.AsNoTracking()
            .Where(a => a.ScheduledAt >= prevStart && a.ScheduledAt < prevEnd && a.Status != AppointmentStatus.Cancelled)
            .CountAsync(ct);

        var sparkline = await BuildDailySparkline(
            db.Appointments.AsNoTracking()
                .Where(a => a.Status != AppointmentStatus.Cancelled && a.ScheduledAt >= start && a.ScheduledAt < end),
            a => a.ScheduledAt, start, periodDays, ct);

        return new KpiDto(current, ComputeDelta(current, previous), sparkline);
    }

    private async Task<DecimalKpiDto> BuildRevenueKpiAsync(
        DateTime start, DateTime end, DateTime prevStart, DateTime prevEnd, int periodDays, CancellationToken ct)
    {
        var current = await db.Payments.AsNoTracking()
            .Where(p => p.Status == PaymentStatus.Captured && p.CapturedAt >= start && p.CapturedAt < end)
            .SumAsync(p => (decimal?)p.Amount ?? 0, ct);

        var previous = await db.Payments.AsNoTracking()
            .Where(p => p.Status == PaymentStatus.Captured && p.CapturedAt >= prevStart && p.CapturedAt < prevEnd)
            .SumAsync(p => (decimal?)p.Amount ?? 0, ct);

        var sparkline = await BuildDailyDecimalSparkline(start, end, periodDays, ct);

        return new DecimalKpiDto(current, ComputeDelta(current, previous), sparkline);
    }

    private async Task<KpiDto> BuildActiveUsersKpiAsync(
        DateTime start, DateTime end, DateTime prevStart, DateTime prevEnd, int periodDays, CancellationToken ct)
    {
        var current = await db.Appointments.AsNoTracking()
            .Where(a => a.ScheduledAt >= start && a.ScheduledAt < end && ActiveStatuses.Contains(a.Status))
            .Select(a => a.PatientId)
            .Distinct()
            .CountAsync(ct);

        var previous = await db.Appointments.AsNoTracking()
            .Where(a => a.ScheduledAt >= prevStart && a.ScheduledAt < prevEnd && ActiveStatuses.Contains(a.Status))
            .Select(a => a.PatientId)
            .Distinct()
            .CountAsync(ct);

        var activeByDay = await db.Appointments.AsNoTracking()
            .Where(a => a.ScheduledAt >= start && a.ScheduledAt < end && ActiveStatuses.Contains(a.Status))
            .Select(a => new { a.PatientId, Day = a.ScheduledAt.Date })
            .Distinct()
            .GroupBy(x => x.Day)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var activeDict = activeByDay.ToDictionary(x => x.Date, x => x.Count);
        var sparkline = new decimal[periodDays];
        for (var i = 0; i < periodDays; i++)
            sparkline[i] = activeDict.GetValueOrDefault(start.AddDays(i).Date, 0);

        return new KpiDto(current, ComputeDelta(current, previous), sparkline);
    }

    private async Task<RateKpiDto> BuildNoShowKpiAsync(
        DateTime start, DateTime end, DateTime prevStart, DateTime prevEnd, int periodDays, CancellationToken ct)
    {
        var completed = await db.Appointments.AsNoTracking()
            .Where(a => a.ScheduledAt >= start && a.ScheduledAt < end && a.Status == AppointmentStatus.Completed)
            .CountAsync(ct);
        var cancelled = await db.Appointments.AsNoTracking()
            .Where(a => a.ScheduledAt >= start && a.ScheduledAt < end && a.Status == AppointmentStatus.Cancelled)
            .CountAsync(ct);
        var total = completed + cancelled;
        var rate = total > 0 ? Math.Round((decimal)cancelled / total * 100, 2) : 0;

        var prevCompleted = await db.Appointments.AsNoTracking()
            .Where(a => a.ScheduledAt >= prevStart && a.ScheduledAt < prevEnd && a.Status == AppointmentStatus.Completed)
            .CountAsync(ct);
        var prevCancelled = await db.Appointments.AsNoTracking()
            .Where(a => a.ScheduledAt >= prevStart && a.ScheduledAt < prevEnd && a.Status == AppointmentStatus.Cancelled)
            .CountAsync(ct);
        var prevTotal = prevCompleted + prevCancelled;
        var prevRate = prevTotal > 0 ? Math.Round((decimal)prevCancelled / prevTotal * 100, 2) : 0;

        var sparkline = new decimal[periodDays];

        return new RateKpiDto(rate, rate - prevRate, sparkline);
    }

    private async Task<TimeSeriesDto> BuildAppointmentSeriesAsync(
        DateTime start, DateTime end, DateTime prevStart, DateTime prevEnd, CancellationToken ct)
    {
        var periodDays = (int)Math.Ceiling((end - start).TotalDays);

        var currentData = await db.Appointments.AsNoTracking()
            .Where(a => a.Status != AppointmentStatus.Cancelled && a.ScheduledAt >= start && a.ScheduledAt < end)
            .GroupBy(a => a.ScheduledAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var previousData = await db.Appointments.AsNoTracking()
            .Where(a => a.Status != AppointmentStatus.Cancelled && a.ScheduledAt >= prevStart && a.ScheduledAt < prevEnd)
            .GroupBy(a => a.ScheduledAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var currentDict = currentData.ToDictionary(x => x.Date, x => x.Count);
        var previousDict = previousData.ToDictionary(x => x.Date, x => x.Count);

        var labels = new string[periodDays];
        var current = new int[periodDays];
        var previous = new int[periodDays];

        for (var i = 0; i < periodDays; i++)
        {
            var date = start.AddDays(i).Date;
            labels[i] = date.ToString("MMM d");
            current[i] = currentDict.GetValueOrDefault(date, 0);
            var prevDate = prevStart.AddDays(i).Date;
            previous[i] = previousDict.GetValueOrDefault(prevDate, 0);
        }

        return new TimeSeriesDto(labels, current, previous);
    }

    private async Task<BarSeriesDto> BuildRevenueSeriesAsync(DateTime end, CancellationToken ct)
    {
        var weeksBack = 7;
        var weekStart = end.AddDays(-(int)end.DayOfWeek - 7 * (weeksBack - 1));

        var payments = await db.Payments.AsNoTracking()
            .Where(p => p.Status == PaymentStatus.Captured && p.CapturedAt >= weekStart && p.CapturedAt < end)
            .Select(p => new { p.CapturedAt, p.Amount })
            .ToListAsync(ct);

        var labels = new List<string>();
        var values = new List<decimal>();

        for (var w = 0; w < weeksBack; w++)
        {
            var wStart = weekStart.AddDays(w * 7);
            var wEnd = wStart.AddDays(7);
            var weekNum = GetIsoWeekNumber(wStart);
            labels.Add($"W{weekNum}");
            values.Add(payments
                .Where(p => p.CapturedAt.HasValue && p.CapturedAt.Value >= wStart && p.CapturedAt.Value < wEnd)
                .Sum(p => p.Amount));
        }

        return new BarSeriesDto(labels.ToArray(), values.ToArray());
    }

    private async Task<FunnelStageDto[]> BuildFunnelAsync(DateTime start, DateTime end, CancellationToken ct)
    {
        var booked = await db.Appointments.AsNoTracking()
            .Where(a => a.CreatedAt >= start && a.CreatedAt < end)
            .CountAsync(ct);

        var scheduled = await db.Appointments.AsNoTracking()
            .Where(a => a.CreatedAt >= start && a.CreatedAt < end && ActiveStatuses.Contains(a.Status))
            .CountAsync(ct);

        var completed = await db.Appointments.AsNoTracking()
            .Where(a => a.CreatedAt >= start && a.CreatedAt < end && a.Status == AppointmentStatus.Completed)
            .CountAsync(ct);

        var paid = await db.Appointments.AsNoTracking()
            .Where(a => a.CreatedAt >= start && a.CreatedAt < end
                && a.Status == AppointmentStatus.Completed
                && a.Payment != null && a.Payment.Status == PaymentStatus.Captured)
            .CountAsync(ct);

        return
        [
            new FunnelStageDto("Booked", booked),
            new FunnelStageDto("Scheduled", scheduled),
            new FunnelStageDto("Completed", completed),
            new FunnelStageDto("Paid", paid),
        ];
    }

    private async Task<ActiveUsersDto> BuildActiveUsersMetricsAsync(DateTime end, CancellationToken ct)
    {
        var today = end.Date;
        var tomorrow = today.AddDays(1);

        var dau = await db.Appointments.AsNoTracking()
            .Where(a => a.ScheduledAt >= today && a.ScheduledAt < tomorrow && ActiveStatuses.Contains(a.Status))
            .Select(a => a.PatientId).Distinct().CountAsync(ct);

        var weekAgo = end.AddDays(-7);
        var wau = await db.Appointments.AsNoTracking()
            .Where(a => a.ScheduledAt >= weekAgo && a.ScheduledAt < end && ActiveStatuses.Contains(a.Status))
            .Select(a => a.PatientId).Distinct().CountAsync(ct);

        var monthAgo = end.AddDays(-30);
        var mau = await db.Appointments.AsNoTracking()
            .Where(a => a.ScheduledAt >= monthAgo && a.ScheduledAt < end && ActiveStatuses.Contains(a.Status))
            .Select(a => a.PatientId).Distinct().CountAsync(ct);

        var prevDau = await db.Appointments.AsNoTracking()
            .Where(a => a.ScheduledAt >= today.AddDays(-1) && a.ScheduledAt < today && ActiveStatuses.Contains(a.Status))
            .Select(a => a.PatientId).Distinct().CountAsync(ct);
        var prevWau = await db.Appointments.AsNoTracking()
            .Where(a => a.ScheduledAt >= weekAgo.AddDays(-7) && a.ScheduledAt < weekAgo && ActiveStatuses.Contains(a.Status))
            .Select(a => a.PatientId).Distinct().CountAsync(ct);
        var prevMau = await db.Appointments.AsNoTracking()
            .Where(a => a.ScheduledAt >= monthAgo.AddDays(-30) && a.ScheduledAt < monthAgo && ActiveStatuses.Contains(a.Status))
            .Select(a => a.PatientId).Distinct().CountAsync(ct);

        var stickiness = mau > 0 ? Math.Round((decimal)dau / mau * 100, 1) : 0;

        return new ActiveUsersDto(dau, wau, mau,
            ComputeDelta(dau, prevDau), ComputeDelta(wau, prevWau), ComputeDelta(mau, prevMau),
            stickiness);
    }

    private async Task<ClinicBreakdownDto[]> BuildClinicBreakdownAsync(DateTime start, DateTime end, CancellationToken ct)
    {
        var appointments = await db.Appointments.AsNoTracking()
            .Where(a => a.ScheduledAt >= start && a.ScheduledAt < end && a.Status != AppointmentStatus.Cancelled)
            .Select(a => a.ClinicId)
            .ToListAsync(ct);

        var clinicNames = await db.Clinics.AsNoTracking()
            .ToDictionaryAsync(c => c.Id, c => c.Name, ct);

        return appointments
            .GroupBy(id => clinicNames.GetValueOrDefault(id, "Unknown"))
            .Select(g => new ClinicBreakdownDto(g.Key, g.Count()))
            .OrderByDescending(c => c.Value)
            .Take(10)
            .ToArray();
    }

    private async Task<SpecialtyMixDto[]> BuildSpecialtyMixAsync(DateTime start, DateTime end, CancellationToken ct)
    {
        var appointments = await db.Appointments.AsNoTracking()
            .Where(a => a.ScheduledAt >= start && a.ScheduledAt < end && a.Status != AppointmentStatus.Cancelled)
            .Select(a => a.DoctorId)
            .ToListAsync(ct);

        var doctorSpecialties = await db.DoctorProfiles.AsNoTracking()
            .ToDictionaryAsync(d => d.Id, d => d.Specialty, ct);

        return appointments
            .GroupBy(id => doctorSpecialties.GetValueOrDefault(id, "Other"))
            .Select(g => new SpecialtyMixDto(g.Key, g.Count()))
            .OrderByDescending(s => s.Value)
            .ToArray();
    }

    private async Task<decimal[]> BuildDailySparkline(
        IQueryable<Appointment> query, Func<Appointment, DateTime> dateSelector,
        DateTime start, int days, CancellationToken ct)
    {
        var data = await query
            .GroupBy(a => a.ScheduledAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var dict = data.ToDictionary(x => x.Date, x => x.Count);
        var sparkline = new decimal[days];
        for (var i = 0; i < days; i++)
            sparkline[i] = dict.GetValueOrDefault(start.AddDays(i).Date, 0);
        return sparkline;
    }

    private async Task<decimal[]> BuildDailyDecimalSparkline(DateTime start, DateTime end, int days, CancellationToken ct)
    {
        var data = await db.Payments.AsNoTracking()
            .Where(p => p.Status == PaymentStatus.Captured && p.CapturedAt >= start && p.CapturedAt < end)
            .GroupBy(p => p.CapturedAt!.Value.Date)
            .Select(g => new { Date = g.Key, Sum = g.Sum(p => p.Amount) })
            .ToListAsync(ct);

        var dict = data.ToDictionary(x => x.Date, x => x.Sum);
        var sparkline = new decimal[days];
        for (var i = 0; i < days; i++)
            sparkline[i] = dict.GetValueOrDefault(start.AddDays(i).Date, 0);
        return sparkline;
    }

    private static decimal ComputeDelta(decimal current, decimal previous)
        => previous == 0 ? (current > 0 ? 100 : 0) : Math.Round((current - previous) / previous * 100, 2);

    private static int GetIsoWeekNumber(DateTime date)
    {
        var day = System.Globalization.CultureInfo.InvariantCulture.Calendar
            .GetWeekOfYear(date, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        return day;
    }
}

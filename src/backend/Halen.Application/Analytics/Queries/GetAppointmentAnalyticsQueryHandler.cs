using Halen.Application.Interfaces;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Halen.Application.Analytics.Queries;

public class GetAppointmentAnalyticsQueryHandler(IAppDbContext db)
    : IRequestHandler<GetAppointmentAnalyticsQuery, AppointmentAnalyticsResult>
{
    private static readonly string[] DayLabels = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];

    public async Task<AppointmentAnalyticsResult> Handle(GetAppointmentAnalyticsQuery request, CancellationToken ct)
    {
        var (start, end, prevStart, prevEnd) = AnalyticsPeriod.ParsePeriod(request.Period);
        var periodDays = (int)Math.Ceiling((end - start).TotalDays);

        // Fetch all appointments whose CreatedAt or ScheduledAt falls within the
        // combined current + previous window. A single query avoids multiple
        // round-trips to the database.
        var allAppointments = await db.Appointments.AsNoTracking()
            .Where(a => a.CreatedAt >= prevStart || a.ScheduledAt >= prevStart)
            .Where(a => a.CreatedAt < end || a.ScheduledAt < end)
            .Select(a => new AppointmentRow(a.CreatedAt, a.ScheduledAt, a.Status))
            .ToListAsync(ct);

        var bookedKpi = BuildBookedKpi(allAppointments, start, end, prevStart, prevEnd, periodDays);
        var completedKpi = BuildCompletedKpi(allAppointments, start, end, prevStart, prevEnd, periodDays);
        var cancelledKpi = BuildCancelledKpi(allAppointments, start, end, prevStart, prevEnd, periodDays);
        var avgLeadTimeKpi = BuildAvgLeadTimeKpi(allAppointments, start, end, prevStart, prevEnd, periodDays);
        var dailySeries = BuildDailySeries(allAppointments, start, end, prevStart, prevEnd, periodDays);
        var byDayOfWeek = BuildByDayOfWeek(allAppointments, start, end);
        var byHourOfDay = BuildByHourOfDay(allAppointments, start, end);

        return new AppointmentAnalyticsResult(
            bookedKpi, completedKpi, cancelledKpi, avgLeadTimeKpi,
            dailySeries, byDayOfWeek, byHourOfDay);
    }

    // ── KPIs ──

    /// <summary>
    /// Booked = appointments whose CreatedAt falls within the period (any status).
    /// This captures the act of booking, not the appointment outcome.
    /// </summary>
    private static KpiDto BuildBookedKpi(
        List<AppointmentRow> rows,
        DateTime start, DateTime end, DateTime prevStart, DateTime prevEnd, int periodDays)
    {
        var current = rows.Count(r => r.CreatedAt >= start && r.CreatedAt < end);
        var previous = rows.Count(r => r.CreatedAt >= prevStart && r.CreatedAt < prevEnd);

        var sparkline = BuildSparkline(
            rows.Where(r => r.CreatedAt >= start && r.CreatedAt < end),
            r => r.CreatedAt, start, periodDays);

        return new KpiDto(current, ComputeDelta(current, previous), sparkline);
    }

    /// <summary>
    /// Completed = appointments with Status == Completed and ScheduledAt in the period.
    /// We filter by ScheduledAt because a "completed" appointment matters on the day
    /// it was delivered, not the day it was created.
    /// </summary>
    private static KpiDto BuildCompletedKpi(
        List<AppointmentRow> rows,
        DateTime start, DateTime end, DateTime prevStart, DateTime prevEnd, int periodDays)
    {
        var current = rows.Count(r => r.Status == AppointmentStatus.Completed
                                      && r.ScheduledAt >= start && r.ScheduledAt < end);
        var previous = rows.Count(r => r.Status == AppointmentStatus.Completed
                                       && r.ScheduledAt >= prevStart && r.ScheduledAt < prevEnd);

        var sparkline = BuildSparkline(
            rows.Where(r => r.Status == AppointmentStatus.Completed
                            && r.ScheduledAt >= start && r.ScheduledAt < end),
            r => r.ScheduledAt, start, periodDays);

        return new KpiDto(current, ComputeDelta(current, previous), sparkline);
    }

    /// <summary>
    /// Cancelled = appointments with Status == Cancelled and ScheduledAt in the period.
    /// </summary>
    private static KpiDto BuildCancelledKpi(
        List<AppointmentRow> rows,
        DateTime start, DateTime end, DateTime prevStart, DateTime prevEnd, int periodDays)
    {
        var current = rows.Count(r => r.Status == AppointmentStatus.Cancelled
                                      && r.ScheduledAt >= start && r.ScheduledAt < end);
        var previous = rows.Count(r => r.Status == AppointmentStatus.Cancelled
                                       && r.ScheduledAt >= prevStart && r.ScheduledAt < prevEnd);

        var sparkline = BuildSparkline(
            rows.Where(r => r.Status == AppointmentStatus.Cancelled
                            && r.ScheduledAt >= start && r.ScheduledAt < end),
            r => r.ScheduledAt, start, periodDays);

        return new KpiDto(current, ComputeDelta(current, previous), sparkline);
    }

    /// <summary>
    /// Average lead time = mean of (ScheduledAt - CreatedAt).TotalDays across all
    /// appointments created in the current period.
    /// Lead time represents how far in advance patients book their appointments.
    /// </summary>
    private static DecimalKpiDto BuildAvgLeadTimeKpi(
        List<AppointmentRow> rows,
        DateTime start, DateTime end, DateTime prevStart, DateTime prevEnd, int periodDays)
    {
        var currentRows = rows
            .Where(r => r.CreatedAt >= start && r.CreatedAt < end)
            .ToList();

        var prevRows = rows
            .Where(r => r.CreatedAt >= prevStart && r.CreatedAt < prevEnd)
            .ToList();

        var currentAvg = currentRows.Count > 0
            ? (decimal)currentRows.Average(r => (r.ScheduledAt - r.CreatedAt).TotalDays)
            : 0m;

        var prevAvg = prevRows.Count > 0
            ? (decimal)prevRows.Average(r => (r.ScheduledAt - r.CreatedAt).TotalDays)
            : 0m;

        currentAvg = Math.Round(currentAvg, 2);
        prevAvg = Math.Round(prevAvg, 2);

        // Sparkline is less meaningful for averages; fill with zeros for consistency
        var sparkline = new decimal[periodDays];

        return new DecimalKpiDto(currentAvg, ComputeDelta(currentAvg, prevAvg), sparkline);
    }

    // ── Daily Series ──

    /// <summary>
    /// Groups all non-cancelled appointments by ScheduledAt date for both current
    /// and previous periods. Returns a TimeSeriesDto with aligned labels.
    /// </summary>
    private static TimeSeriesDto BuildDailySeries(
        List<AppointmentRow> rows,
        DateTime start, DateTime end, DateTime prevStart, DateTime prevEnd, int periodDays)
    {
        var currentDict = rows
            .Where(r => r.Status != AppointmentStatus.Cancelled
                        && r.ScheduledAt >= start && r.ScheduledAt < end)
            .GroupBy(r => r.ScheduledAt.Date)
            .ToDictionary(g => g.Key, g => g.Count());

        var previousDict = rows
            .Where(r => r.Status != AppointmentStatus.Cancelled
                        && r.ScheduledAt >= prevStart && r.ScheduledAt < prevEnd)
            .GroupBy(r => r.ScheduledAt.Date)
            .ToDictionary(g => g.Key, g => g.Count());

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

    // ── Day of Week ──

    /// <summary>
    /// Groups non-cancelled appointments by day of week, remapping .NET's
    /// Sunday=0 convention to a Mon=0 scheme: (dow == 0 ? 6 : dow - 1).
    /// Returns ratio (proportion of total) for each day, always 7 items.
    /// </summary>
    private static DayOfWeekDto[] BuildByDayOfWeek(List<AppointmentRow> rows, DateTime start, DateTime end)
    {
        var periodRows = rows
            .Where(r => r.Status != AppointmentStatus.Cancelled
                        && r.ScheduledAt >= start && r.ScheduledAt < end)
            .ToList();

        var total = periodRows.Count;

        // Initialize counts for all 7 days (Mon=0 through Sun=6)
        var counts = new int[7];

        foreach (var row in periodRows)
        {
            var dow = (int)row.ScheduledAt.DayOfWeek;
            var remapped = dow == 0 ? 6 : dow - 1; // Sunday→6, Monday→0, etc.
            counts[remapped]++;
        }

        var result = new DayOfWeekDto[7];
        for (var i = 0; i < 7; i++)
        {
            var ratio = total > 0 ? Math.Round((decimal)counts[i] / total, 4) : 0m;
            result[i] = new DayOfWeekDto(DayLabels[i], ratio);
        }

        return result;
    }

    // ── Hour of Day ──

    /// <summary>
    /// Counts non-cancelled appointments by hour of ScheduledAt.
    /// Always returns exactly 24 items (hours 0–23), filling gaps with zero.
    /// </summary>
    private static HourOfDayDto[] BuildByHourOfDay(List<AppointmentRow> rows, DateTime start, DateTime end)
    {
        var periodRows = rows
            .Where(r => r.Status != AppointmentStatus.Cancelled
                        && r.ScheduledAt >= start && r.ScheduledAt < end)
            .ToList();

        var counts = new int[24];
        foreach (var row in periodRows)
            counts[row.ScheduledAt.Hour]++;

        var result = new HourOfDayDto[24];
        for (var i = 0; i < 24; i++)
            result[i] = new HourOfDayDto(i, counts[i]);

        return result;
    }

    // ── Helpers ──

    private static decimal[] BuildSparkline(
        IEnumerable<AppointmentRow> filtered,
        Func<AppointmentRow, DateTime> dateSelector,
        DateTime start, int days)
    {
        var dict = filtered
            .GroupBy(r => dateSelector(r).Date)
            .ToDictionary(g => g.Key, g => g.Count());

        var sparkline = new decimal[days];
        for (var i = 0; i < days; i++)
            sparkline[i] = dict.GetValueOrDefault(start.AddDays(i).Date, 0);
        return sparkline;
    }

    private static decimal ComputeDelta(decimal current, decimal previous)
        => previous == 0 ? (current > 0 ? 100 : 0) : Math.Round((current - previous) / previous * 100, 2);

    /// <summary>
    /// Lightweight projection record to avoid pulling full Appointment entities.
    /// Contains only the fields needed for analytics calculations.
    /// </summary>
    private record AppointmentRow(DateTime CreatedAt, DateTime ScheduledAt, AppointmentStatus Status);
}

using Halen.Application.Interfaces;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Halen.Application.Analytics.Queries;

public class GetHeatmapAnalyticsQueryHandler(IAppDbContext db)
    : IRequestHandler<GetHeatmapAnalyticsQuery, HeatmapAnalyticsResult>
{
    public async Task<HeatmapAnalyticsResult> Handle(GetHeatmapAnalyticsQuery request, CancellationToken ct)
    {
        var (start, end, _, _) = AnalyticsPeriod.ParsePeriod(request.Period);

        var grid = await BuildGridAsync(start, end, ct);
        var seasonality = await BuildSpecialtySeasonalityAsync(end, ct);
        var wait = await BuildAvgWaitBySpecialtyAsync(start, end, ct);

        return new HeatmapAnalyticsResult(grid, seasonality, wait);
    }

    private async Task<int[][]> BuildGridAsync(DateTime start, DateTime end, CancellationToken ct)
    {
        // Initialize 7x24 grid (Mon=0 through Sun=6, hours 0-23)
        var grid = new int[7][];
        for (var d = 0; d < 7; d++)
            grid[d] = new int[24];

        var appointments = await db.Appointments.AsNoTracking()
            .Where(a => a.ScheduledAt >= start && a.ScheduledAt < end
                        && a.Status != AppointmentStatus.Cancelled)
            .Select(a => a.ScheduledAt)
            .ToListAsync(ct);

        foreach (var scheduledAt in appointments)
        {
            // Remap: .NET DayOfWeek Sunday=0, Monday=1 ... Saturday=6
            // We want Monday=0, Tuesday=1 ... Sunday=6
            var dow = (int)scheduledAt.DayOfWeek;
            var row = dow == 0 ? 6 : dow - 1;
            var hour = scheduledAt.Hour;
            grid[row][hour]++;
        }

        return grid;
    }

    private async Task<SpecialtySeasonDto[]> BuildSpecialtySeasonalityAsync(DateTime end, CancellationToken ct)
    {
        // Last 6 months of data
        var sixMonthsAgo = end.AddMonths(-6);

        var appointments = await db.Appointments.AsNoTracking()
            .Where(a => a.ScheduledAt >= sixMonthsAgo && a.ScheduledAt < end
                        && a.Status != AppointmentStatus.Cancelled)
            .Select(a => new { a.DoctorId, a.ScheduledAt })
            .ToListAsync(ct);

        var doctorSpecialties = await db.DoctorProfiles.AsNoTracking()
            .ToDictionaryAsync(d => d.Id, d => d.Specialty, ct);

        // Group by specialty, count total volume to find top 3
        var bySpecialty = appointments
            .GroupBy(a => doctorSpecialties.GetValueOrDefault(a.DoctorId, "Other"))
            .OrderByDescending(g => g.Count())
            .Take(3)
            .ToList();

        return bySpecialty.Select(g =>
        {
            var monthlyData = g
                .GroupBy(a => new { a.ScheduledAt.Year, a.ScheduledAt.Month })
                .OrderBy(mg => mg.Key.Year).ThenBy(mg => mg.Key.Month)
                .Select(mg => new MonthDataPointDto(
                    $"{mg.Key.Year}-{mg.Key.Month:D2}",
                    mg.Count()))
                .ToArray();

            return new SpecialtySeasonDto(g.Key, monthlyData);
        }).ToArray();
    }

    private async Task<SpecialtyWaitDto[]> BuildAvgWaitBySpecialtyAsync(
        DateTime start, DateTime end, CancellationToken ct)
    {
        var appointments = await db.Appointments.AsNoTracking()
            .Where(a => a.ScheduledAt >= start && a.ScheduledAt < end
                        && a.Status != AppointmentStatus.Cancelled)
            .Select(a => new { a.DoctorId, a.ScheduledAt, a.CreatedAt })
            .ToListAsync(ct);

        var doctorSpecialties = await db.DoctorProfiles.AsNoTracking()
            .ToDictionaryAsync(d => d.Id, d => d.Specialty, ct);

        return appointments
            .GroupBy(a => doctorSpecialties.GetValueOrDefault(a.DoctorId, "Other"))
            .Select(g => new SpecialtyWaitDto(
                g.Key,
                Math.Round((decimal)g.Average(a => (a.ScheduledAt - a.CreatedAt).TotalDays), 0)))
            .OrderByDescending(w => w.Days)
            .ToArray();
    }
}

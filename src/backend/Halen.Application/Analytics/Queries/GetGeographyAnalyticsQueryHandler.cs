using Halen.Application.Interfaces;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Halen.Application.Analytics.Queries;

public class GetGeographyAnalyticsQueryHandler(IAppDbContext db)
    : IRequestHandler<GetGeographyAnalyticsQuery, GeographyAnalyticsResult>
{
    public async Task<GeographyAnalyticsResult> Handle(GetGeographyAnalyticsQuery request, CancellationToken ct)
    {
        var (start, end, prevStart, prevEnd) = AnalyticsPeriod.ParsePeriod(request.Period);

        var regions = await BuildRegionsAsync(start, end, prevStart, prevEnd, ct);
        var retention = await BuildCohortRetentionAsync(end, ct);

        return new GeographyAnalyticsResult(regions, retention);
    }

    private async Task<RegionDto[]> BuildRegionsAsync(
        DateTime start, DateTime end, DateTime prevStart, DateTime prevEnd, CancellationToken ct)
    {
        var currentAppointments = await db.Appointments.AsNoTracking()
            .Where(a => a.ScheduledAt >= start && a.ScheduledAt < end && a.Status != AppointmentStatus.Cancelled)
            .Select(a => a.ClinicId)
            .ToListAsync(ct);

        var prevAppointments = await db.Appointments.AsNoTracking()
            .Where(a => a.ScheduledAt >= prevStart && a.ScheduledAt < prevEnd && a.Status != AppointmentStatus.Cancelled)
            .Select(a => a.ClinicId)
            .ToListAsync(ct);

        if (currentAppointments.Count == 0)
            return [];

        var clinicNames = await db.Clinics.AsNoTracking()
            .ToDictionaryAsync(c => c.Id, c => c.Name, ct);

        var currentGroups = currentAppointments
            .GroupBy(id => clinicNames.GetValueOrDefault(id, "Unknown"))
            .ToDictionary(g => g.Key, g => g.Count());

        var prevGroups = prevAppointments
            .GroupBy(id => clinicNames.GetValueOrDefault(id, "Unknown"))
            .ToDictionary(g => g.Key, g => g.Count());

        var maxConsults = currentGroups.Values.Max();

        return currentGroups
            .OrderByDescending(g => g.Value)
            .Select(g => new RegionDto(
                g.Key,
                g.Value,
                ComputeDelta(g.Value, prevGroups.GetValueOrDefault(g.Key, 0)),
                g.Value == maxConsults))
            .ToArray();
    }

    private async Task<CohortRetentionDto> BuildCohortRetentionAsync(DateTime end, CancellationToken ct)
    {
        var weeksBack = 8;
        var daysFromMonday = ((int)end.DayOfWeek - 1 + 7) % 7;
        var thisWeekMonday = end.AddDays(-daysFromMonday).Date;
        var cohortStart = thisWeekMonday.AddDays(-7 * (weeksBack - 1));

        var appointments = await db.Appointments.AsNoTracking()
            .Where(a => a.Status != AppointmentStatus.Cancelled)
            .Select(a => new { a.PatientId, a.ScheduledAt })
            .ToListAsync(ct);

        if (appointments.Count == 0)
            return new CohortRetentionDto([]);

        var patientFirstAppointment = appointments
            .GroupBy(a => a.PatientId)
            .ToDictionary(g => g.Key, g => g.Min(a => a.ScheduledAt));

        var patientAppointmentWeeks = appointments
            .GroupBy(a => a.PatientId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(a => GetWeekMonday(a.ScheduledAt)).Distinct().ToHashSet());

        var cohorts = new List<CohortWeekDto>();

        for (var w = 0; w < weeksBack; w++)
        {
            var weekMonday = cohortStart.AddDays(w * 7);
            var weekEnd = weekMonday.AddDays(7);

            var cohortPatients = patientFirstAppointment
                .Where(p => p.Value >= weekMonday && p.Value < weekEnd)
                .Select(p => p.Key)
                .ToList();

            if (cohortPatients.Count == 0)
                continue;

            var maxOffsets = weeksBack - w;
            var weeks = new decimal[maxOffsets];

            for (var offset = 0; offset < maxOffsets; offset++)
            {
                var offsetMonday = weekMonday.AddDays(offset * 7);
                var returning = cohortPatients
                    .Count(pid => patientAppointmentWeeks[pid].Contains(offsetMonday));

                weeks[offset] = Math.Round((decimal)returning / cohortPatients.Count * 100, 2);
            }

            cohorts.Add(new CohortWeekDto(weekMonday.ToString("MMM d"), weeks));
        }

        return new CohortRetentionDto(cohorts.ToArray());
    }

    private static DateTime GetWeekMonday(DateTime date)
    {
        var daysFromMonday = ((int)date.DayOfWeek - 1 + 7) % 7;
        return date.AddDays(-daysFromMonday).Date;
    }

    private static decimal ComputeDelta(decimal current, decimal previous)
        => previous == 0 ? (current > 0 ? 100 : 0) : Math.Round((current - previous) / previous * 100, 2);
}

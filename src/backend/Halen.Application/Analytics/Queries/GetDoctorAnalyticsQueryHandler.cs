using Halen.Application.Interfaces;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Halen.Application.Analytics.Queries;

public class GetDoctorAnalyticsQueryHandler(IAppDbContext db)
    : IRequestHandler<GetDoctorAnalyticsQuery, DoctorAnalyticsResult>
{
    public async Task<DoctorAnalyticsResult> Handle(GetDoctorAnalyticsQuery request, CancellationToken ct)
    {
        var (start, end, _, _) = AnalyticsPeriod.ParsePeriod(request.Period);

        var ranked = await BuildRankedAsync(start, end, ct);
        var topRated = await BuildTopRatedAsync(ct);
        var needsAttention = await BuildNeedsAttentionAsync(start, end, ct);

        return new DoctorAnalyticsResult(ranked, topRated, needsAttention);
    }

    private async Task<RankedDoctorDto[]> BuildRankedAsync(
        DateTime start, DateTime end, CancellationToken ct)
    {
        var appointments = await db.Appointments.AsNoTracking()
            .Where(a => a.ScheduledAt >= start && a.ScheduledAt < end)
            .Select(a => new { a.Id, a.DoctorId, a.Status, a.ScheduledAt })
            .ToListAsync(ct);

        var payments = await db.Payments.AsNoTracking()
            .Where(p => p.Status == PaymentStatus.Captured
                        && p.CapturedAt >= start && p.CapturedAt < end)
            .Select(p => new { p.AppointmentId, p.Amount })
            .ToListAsync(ct);

        var paymentByAppointment = payments.ToDictionary(p => p.AppointmentId, p => p.Amount);

        var revenueByDoctor = appointments
            .Where(a => paymentByAppointment.ContainsKey(a.Id))
            .GroupBy(a => a.DoctorId)
            .ToDictionary(g => g.Key, g => g.Sum(a => paymentByAppointment[a.Id]));

        // Fetch doctor profiles
        var doctors = await db.DoctorProfiles.AsNoTracking()
            .Select(d => new { d.Id, d.UserId })
            .ToListAsync(ct);

        var users = await db.Users.AsNoTracking()
            .Select(u => new { u.Id, u.FirstName, u.LastName })
            .ToListAsync(ct);

        var userNameById = users.ToDictionary(u => u.Id, u => $"{u.FirstName} {u.LastName}");
        var doctorUserMap = doctors.ToDictionary(d => d.Id, d => d.UserId);

        var doctorSpecialties = await db.DoctorProfiles.AsNoTracking()
            .ToDictionaryAsync(d => d.Id, d => d.Specialty, ct);

        var doctorRatings = await db.DoctorProfiles.AsNoTracking()
            .ToDictionaryAsync(d => d.Id, d => d.AverageRating ?? 0m, ct);

        // Build trend: weekly counts for last 4 weeks
        var fourWeeksAgo = end.AddDays(-28);

        // Group by doctor
        var grouped = appointments
            .Where(a => a.Status != AppointmentStatus.Cancelled)
            .GroupBy(a => a.DoctorId)
            .Select(g =>
            {
                var completed = appointments.Count(a => a.DoctorId == g.Key && a.Status == AppointmentStatus.Completed);
                var cancelled = appointments.Count(a => a.DoctorId == g.Key && a.Status == AppointmentStatus.Cancelled);
                var total = completed + cancelled;
                var completionPct = total > 0 ? Math.Round((decimal)completed / total * 100, 0) : 100m;

                var doctorName = doctorUserMap.TryGetValue(g.Key, out var userId)
                    ? userNameById.GetValueOrDefault(userId, "Unknown")
                    : "Unknown";

                // Weekly trend for last 4 weeks
                var trend = new decimal[4];
                for (var w = 0; w < 4; w++)
                {
                    var weekStart = end.AddDays(-28 + w * 7);
                    var weekEnd = weekStart.AddDays(7);
                    trend[w] = g.Count(a => a.ScheduledAt >= weekStart && a.ScheduledAt < weekEnd);
                }

                return new RankedDoctorDto(
                    doctorName,
                    doctorSpecialties.GetValueOrDefault(g.Key, "Other"),
                    g.Count(),
                    completionPct,
                    doctorRatings.GetValueOrDefault(g.Key, 0),
                    revenueByDoctor.GetValueOrDefault(g.Key, 0),
                    trend,
                    null);
            })
            .OrderByDescending(d => d.Consults)
            .Take(20)
            .ToArray();

        return grouped;
    }

    private async Task<TopRatedDoctorDto[]> BuildTopRatedAsync(CancellationToken ct)
    {
        var doctors = await db.DoctorProfiles.AsNoTracking()
            .Where(d => d.ReviewCount >= 50 && d.AverageRating != null)
            .Select(d => new { d.Id, d.UserId, d.AverageRating, d.ReviewCount, d.Specialty })
            .ToListAsync(ct);

        if (doctors.Count == 0)
            return [];

        var userIds = doctors.Select(d => d.UserId).ToList();
        var users = await db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FirstName, u.LastName })
            .ToListAsync(ct);

        var userNameById = users.ToDictionary(u => u.Id, u => $"{u.FirstName} {u.LastName}");

        return doctors
            .OrderByDescending(d => d.AverageRating)
            .Take(5)
            .Select(d => new TopRatedDoctorDto(
                userNameById.GetValueOrDefault(d.UserId, "Unknown"),
                d.AverageRating ?? 0,
                d.ReviewCount,
                d.Specialty))
            .ToArray();
    }

    private async Task<NeedsAttentionDto[]> BuildNeedsAttentionAsync(
        DateTime start, DateTime end, CancellationToken ct)
    {
        var alerts = new List<NeedsAttentionDto>();

        // Low completion rate doctors
        var appointments = await db.Appointments.AsNoTracking()
            .Where(a => a.ScheduledAt >= start && a.ScheduledAt < end)
            .Select(a => new { a.DoctorId, a.Status })
            .ToListAsync(ct);

        var doctorProfiles = await db.DoctorProfiles.AsNoTracking()
            .Select(d => new { d.Id, d.UserId, d.AverageRating, d.ReviewCount })
            .ToListAsync(ct);

        var userIds = doctorProfiles.Select(d => d.UserId).Distinct().ToList();
        var users = await db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FirstName, u.LastName })
            .ToListAsync(ct);

        var userNameById = users.ToDictionary(u => u.Id, u => $"{u.FirstName} {u.LastName}");
        var profileMap = doctorProfiles.ToDictionary(d => d.Id);

        // Check completion rates
        var byDoctor = appointments.GroupBy(a => a.DoctorId);
        foreach (var g in byDoctor)
        {
            var completed = g.Count(a => a.Status == AppointmentStatus.Completed);
            var cancelled = g.Count(a => a.Status == AppointmentStatus.Cancelled);
            var total = completed + cancelled;

            if (total < 5) continue;

            var completionPct = (decimal)completed / total * 100;
            if (completionPct < 85)
            {
                var name = "Unknown";
                if (profileMap.TryGetValue(g.Key, out var profile))
                    name = userNameById.GetValueOrDefault(profile.UserId, "Unknown");

                alerts.Add(new NeedsAttentionDto(
                    name,
                    $"Completion rate {Math.Round(completionPct, 0)}% (below 85%)",
                    "warn"));
            }
        }

        // Check low ratings
        foreach (var dp in doctorProfiles)
        {
            if (dp.ReviewCount >= 10 && dp.AverageRating.HasValue && dp.AverageRating.Value < 3.5m)
            {
                var name = userNameById.GetValueOrDefault(dp.UserId, "Unknown");
                alerts.Add(new NeedsAttentionDto(
                    name,
                    $"Rating {dp.AverageRating.Value} (below 3.5)",
                    "danger"));
            }
        }

        return alerts.ToArray();
    }
}

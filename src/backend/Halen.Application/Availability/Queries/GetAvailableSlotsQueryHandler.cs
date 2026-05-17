using Halen.Application.Interfaces;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Halen.Application.Availability.Queries;

public class GetAvailableSlotsQueryHandler(
    IAppDbContext db
) : IRequestHandler<GetAvailableSlotsQuery, GetAvailableSlotsResult>
{
    public async Task<GetAvailableSlotsResult> Handle(GetAvailableSlotsQuery request, CancellationToken ct)
    {
        var dayOfWeek = request.Date.DayOfWeek;

        // Load active availability windows for the doctor on that day of week
        var windows = await db.DoctorAvailabilities
            .AsNoTracking()
            .Where(a => a.DoctorProfileId == request.DoctorProfileId
                     && a.IsActive
                     && a.DayOfWeek == dayOfWeek)
            .OrderBy(a => a.StartTime)
            .ToListAsync(ct);

        if (windows.Count == 0)
            return new GetAvailableSlotsResult([]);

        // Generate all possible slots from the windows, carrying the duration for overlap checks
        var allSlots = new List<(DateTime Start, int Duration)>();
        foreach (var window in windows)
        {
            var current = window.StartTime;
            while (current.AddMinutes(window.SlotDurationMinutes) <= window.EndTime)
            {
                var slotDateTime = request.Date.ToDateTime(current, DateTimeKind.Utc);
                allSlots.Add((slotDateTime, window.SlotDurationMinutes));
                current = current.AddMinutes(window.SlotDurationMinutes);
            }
        }

        // Load existing scheduled appointments for the doctor on that date
        var dateStart = request.Date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var dateEnd = request.Date.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var appointments = await db.Appointments
            .AsNoTracking()
            .Where(a => a.DoctorId == request.DoctorProfileId
                     && a.Status == AppointmentStatus.Scheduled
                     && a.ScheduledAt >= dateStart
                     && a.ScheduledAt <= dateEnd)
            .Select(a => new { a.ScheduledAt, a.DurationMinutes })
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        var isToday = request.Date == DateOnly.FromDateTime(now);

        var slots = allSlots
            .Select(slot =>
            {
                if (isToday && slot.Start <= now)
                    return null;

                var isBooked = appointments.Any(a =>
                    slot.Start < a.ScheduledAt.AddMinutes(a.DurationMinutes) &&
                    a.ScheduledAt < slot.Start.AddMinutes(slot.Duration));

                return new TimeSlotDto(slot.Start, TimeOnly.FromDateTime(slot.Start).ToString("HH:mm"), !isBooked);
            })
            .Where(s => s is not null)
            .Cast<TimeSlotDto>()
            .ToList();

        return new GetAvailableSlotsResult(slots);
    }
}

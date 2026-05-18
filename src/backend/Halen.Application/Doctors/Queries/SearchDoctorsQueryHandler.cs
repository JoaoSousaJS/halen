using Halen.Application.Interfaces;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Halen.Application.Doctors.Queries;

public class SearchDoctorsQueryHandler(
    IAppDbContext db
) : IRequestHandler<SearchDoctorsQuery, SearchDoctorsResult>
{
    public async Task<SearchDoctorsResult> Handle(SearchDoctorsQuery request, CancellationToken ct)
    {
        // 1. Base query: approved doctors with active accounts
        var query = db.DoctorProfiles
            .AsNoTracking()
            .Where(d => d.KycStatus == KycStatus.Approved && d.User.Status == AccountStatus.Active);

        // 2. Fuzzy search on name and specialty
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var escaped = request.SearchTerm
                .Replace("\\", "\\\\")
                .Replace("%", "\\%")
                .Replace("_", "\\_");
            var pattern = $"%{escaped}%";
            query = query.Where(d =>
                EF.Functions.ILike(d.User.FirstName, pattern) ||
                EF.Functions.ILike(d.User.LastName, pattern) ||
                EF.Functions.ILike(d.Specialty, pattern));
        }

        // 3. Exact specialty filter
        if (!string.IsNullOrWhiteSpace(request.Specialty))
        {
            query = query.Where(d => d.Specialty == request.Specialty);
        }

        // 4. Fee range filter
        if (request.MinFee.HasValue)
        {
            query = query.Where(d => d.ConsultationFee >= request.MinFee.Value);
        }

        if (request.MaxFee.HasValue)
        {
            query = query.Where(d => d.ConsultationFee <= request.MaxFee.Value);
        }

        // 5. Availability day filter
        if (request.AvailableOn.HasValue)
        {
            query = query.Where(d =>
                d.Availabilities.Any(a => a.DayOfWeek == request.AvailableOn.Value && a.IsActive));
        }

        // 6. Total count (before pagination)
        var totalCount = await query.CountAsync(ct);

        // 7. Apply sorting
        var ordered = request.SortBy switch
        {
            "fee_asc" => query.OrderBy(d => d.ConsultationFee),
            "fee_desc" => query.OrderByDescending(d => d.ConsultationFee),
            "experience" => query.OrderByDescending(d => d.YearsOfExperience),
            "rating" => query.OrderByDescending(d => d.AverageRating ?? 0),
            "name" => query.OrderBy(d => d.User.LastName).ThenBy(d => d.User.FirstName),
            _ => query.OrderBy(d => d.User.LastName).ThenBy(d => d.User.FirstName),
        };

        // 8. Paginate
        var doctors = await ordered
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(d => new
            {
                d.Id,
                Name = $"{d.User.FirstName} {d.User.LastName}",
                d.Specialty,
                d.ConsultationFee,
                d.YearsOfExperience,
                d.Languages,
                d.AverageRating,
                d.ReviewCount,
                Availabilities = d.Availabilities
                    .Where(a => a.IsActive)
                    .Select(a => new { a.DayOfWeek, a.StartTime, a.EndTime, a.SlotDurationMinutes })
                    .ToList()
            })
            .ToListAsync(ct);

        // 9. Project to DTOs — compute NextAvailableSlot in-memory
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);
        var currentTime = TimeOnly.FromDateTime(now);

        var results = doctors.Select(d =>
        {
            NextSlotDto? nextSlot = null;

            // Look ahead up to 7 days to find the next available slot
            for (var dayOffset = 0; dayOffset < 7; dayOffset++)
            {
                var candidateDate = today.AddDays(dayOffset);
                var candidateDayOfWeek = candidateDate.DayOfWeek;

                var windows = d.Availabilities
                    .Where(a => a.DayOfWeek == candidateDayOfWeek)
                    .OrderBy(a => a.StartTime)
                    .ToList();

                foreach (var window in windows)
                {
                    var slotStart = window.StartTime;

                    // If today, skip slots that have already passed
                    if (dayOffset == 0 && slotStart <= currentTime)
                    {
                        slotStart = currentTime;
                        // Round up to the next slot boundary within the window
                        var minutesSinceWindowStart = (slotStart.ToTimeSpan() - window.StartTime.ToTimeSpan()).TotalMinutes;
                        var slotsToSkip = (int)Math.Ceiling(minutesSinceWindowStart / window.SlotDurationMinutes);
                        slotStart = window.StartTime.AddMinutes(slotsToSkip * window.SlotDurationMinutes);
                    }

                    if (slotStart.AddMinutes(window.SlotDurationMinutes) <= window.EndTime)
                    {
                        var startUtc = new DateTimeOffset(candidateDate.ToDateTime(slotStart, DateTimeKind.Utc));
                        nextSlot = new NextSlotDto(startUtc, candidateDayOfWeek.ToString());
                        break;
                    }
                }

                if (nextSlot is not null)
                    break;
            }

            return new DoctorSearchDto(
                d.Id,
                d.Name,
                d.Specialty,
                d.ConsultationFee,
                d.YearsOfExperience,
                d.Languages,
                nextSlot,
                d.AverageRating,
                d.ReviewCount);
        }).ToList();

        return new SearchDoctorsResult(results, totalCount);
    }
}

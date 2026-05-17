using Halen.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Halen.Application.Availability.Queries;

public class GetDoctorAvailabilityQueryHandler(
    IAppDbContext db
) : IRequestHandler<GetDoctorAvailabilityQuery, GetDoctorAvailabilityResult>
{
    public async Task<GetDoctorAvailabilityResult> Handle(GetDoctorAvailabilityQuery request, CancellationToken ct)
    {
        var windows = await db.DoctorAvailabilities
            .AsNoTracking()
            .Where(a => a.DoctorProfileId == request.DoctorProfileId && a.IsActive)
            .OrderBy(a => a.DayOfWeek)
            .ThenBy(a => a.StartTime)
            .Select(a => new AvailabilityWindowDto(
                a.Id,
                a.DayOfWeek.ToString(),
                a.StartTime.ToString("HH:mm"),
                a.EndTime.ToString("HH:mm"),
                a.SlotDurationMinutes))
            .ToListAsync(ct);

        return new GetDoctorAvailabilityResult(windows);
    }
}

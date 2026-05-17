using Halen.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Halen.Application.Availability.Queries;

public class GetMyAvailabilityQueryHandler(
    IAppDbContext db
) : IRequestHandler<GetMyAvailabilityQuery, GetDoctorAvailabilityResult?>
{
    public async Task<GetDoctorAvailabilityResult?> Handle(GetMyAvailabilityQuery request, CancellationToken ct)
    {
        var doctorProfileId = await db.DoctorProfiles
            .Where(d => d.UserId == request.UserId)
            .Select(d => d.Id)
            .FirstOrDefaultAsync(ct);

        if (doctorProfileId == Guid.Empty)
            return null;

        var windows = await db.DoctorAvailabilities
            .AsNoTracking()
            .Where(a => a.DoctorProfileId == doctorProfileId && a.IsActive)
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

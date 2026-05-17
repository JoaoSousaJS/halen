using Halen.Application.Common;
using Halen.Application.Interfaces;
using Halen.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Halen.Application.Availability.Commands;

public class SetDoctorAvailabilityCommandHandler(
    IAppDbContext db,
    ITenantContext tenantContext
) : IRequestHandler<SetDoctorAvailabilityCommand, SetDoctorAvailabilityResult>
{
    public async Task<SetDoctorAvailabilityResult> Handle(SetDoctorAvailabilityCommand request, CancellationToken ct)
    {
        var doctorProfile = await db.DoctorProfiles
            .FirstOrDefaultAsync(d => d.UserId == request.UserId, ct);

        if (doctorProfile is null)
            return new SetDoctorAvailabilityResult(false, "Doctor profile not found.", ErrorKind.NotFound);

        // Overlap detection: group by day, sort by start time, check adjacency
        var grouped = request.Slots
            .GroupBy(s => s.DayOfWeek)
            .ToList();

        foreach (var group in grouped)
        {
            var sorted = group.OrderBy(s => s.StartTime).ToList();
            for (var i = 1; i < sorted.Count; i++)
            {
                if (sorted[i].StartTime < sorted[i - 1].EndTime)
                {
                    return new SetDoctorAvailabilityResult(false,
                        $"Overlapping slots detected on {group.Key}: " +
                        $"{sorted[i - 1].StartTime:HH:mm}-{sorted[i - 1].EndTime:HH:mm} overlaps with " +
                        $"{sorted[i].StartTime:HH:mm}-{sorted[i].EndTime:HH:mm}.",
                        ErrorKind.Validation);
                }
            }
        }

        // Remove all existing availabilities for this doctor
        var existing = await db.DoctorAvailabilities
            .Where(a => a.DoctorProfileId == doctorProfile.Id)
            .ToListAsync(ct);

        db.DoctorAvailabilities.RemoveRange(existing);

        // Add new availability entries
        foreach (var slot in request.Slots)
        {
            db.DoctorAvailabilities.Add(new DoctorAvailability
            {
                DoctorProfileId = doctorProfile.Id,
                ClinicId = tenantContext.ClinicId,
                DayOfWeek = slot.DayOfWeek,
                StartTime = slot.StartTime,
                EndTime = slot.EndTime,
                SlotDurationMinutes = 20,
                IsActive = true,
            });
        }

        await db.SaveChangesAsync(ct);

        return new SetDoctorAvailabilityResult(true);
    }
}

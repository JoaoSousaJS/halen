using Halen.Domain.Interfaces;

namespace Halen.Domain.Entities;

public class DoctorAvailability : BaseEntity, ITenantScoped
{
    public Guid DoctorProfileId { get; set; }
    public DoctorProfile DoctorProfile { get; set; } = null!;
    public Guid ClinicId { get; set; }
    public Clinic? Clinic { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int SlotDurationMinutes { get; set; } = 20;
    public bool IsActive { get; set; } = true;
}

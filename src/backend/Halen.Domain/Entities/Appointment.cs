using Halen.Domain.Enums;

namespace Halen.Domain.Entities;

public class Appointment : BaseEntity
{
    public Guid PatientId { get; set; }
    public PatientProfile Patient { get; set; } = null!;

    public Guid DoctorId { get; set; }
    public DoctorProfile Doctor { get; set; } = null!;

    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; } = 20;
    public string Reason { get; set; } = string.Empty;
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
    public string? VideoRoomId { get; set; }
    public string? Notes { get; set; }
}

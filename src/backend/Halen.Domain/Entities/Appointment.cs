using Halen.Domain.Enums;
using Halen.Domain.Interfaces;

namespace Halen.Domain.Entities;

public class Appointment : BaseEntity, ITenantScoped
{
    public Guid ClinicId { get; set; }
    public Clinic? Clinic { get; set; }

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
    public Payment? Payment { get; set; }
    public ConsultationRoom? ConsultationRoom { get; set; }
}

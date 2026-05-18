using Halen.Domain.Enums;
using Halen.Domain.Interfaces;

namespace Halen.Domain.Entities;

public class ConversationThread : BaseEntity, ITenantScoped
{
    public Guid ClinicId { get; set; }
    public Clinic? Clinic { get; set; }

    public Guid AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }

    public Guid PatientUserId { get; set; }
    public User? PatientUser { get; set; }

    public Guid DoctorUserId { get; set; }
    public User? DoctorUser { get; set; }

    public ThreadStatus Status { get; set; } = ThreadStatus.Active;
    public string Subject { get; set; } = string.Empty;

    public DateTimeOffset? LastMessageAt { get; set; }
    public string? LastMessagePreview { get; set; }
    public int PatientUnreadCount { get; set; }
    public int DoctorUnreadCount { get; set; }

    public DateTimeOffset? ClosedAt { get; set; }
    public Guid? ClosedByUserId { get; set; }

    public List<ChatMessage> Messages { get; set; } = [];
}

using Halen.Domain.Enums;
using Halen.Domain.Interfaces;

namespace Halen.Domain.Entities;

public class ConsultationRoom : BaseEntity, ITenantScoped
{
    public Guid ClinicId { get; set; }
    public Clinic? Clinic { get; set; }

    public Guid AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }

    public string RoomCode { get; set; } = string.Empty;
    public ConsultationRoomStatus Status { get; set; } = ConsultationRoomStatus.Waiting;

    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
    public DateTimeOffset? DoctorJoinedAt { get; set; }
    public DateTimeOffset? PatientJoinedAt { get; set; }

    public string? Notes { get; set; }

    public static string GenerateRoomCode() =>
        $"VC-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
}

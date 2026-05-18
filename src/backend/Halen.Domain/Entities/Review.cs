using Halen.Domain.Enums;
using Halen.Domain.Interfaces;

namespace Halen.Domain.Entities;

public class Review : BaseEntity, ITenantScoped
{
    public Guid ClinicId { get; set; }
    public Clinic? Clinic { get; set; }

    public Guid AppointmentId { get; set; }
    public Appointment Appointment { get; set; } = null!;

    public Guid PatientProfileId { get; set; }
    public PatientProfile PatientProfile { get; set; } = null!;

    public Guid DoctorProfileId { get; set; }
    public DoctorProfile DoctorProfile { get; set; } = null!;

    public int Rating { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string[] Tags { get; set; } = [];
    public bool IsVerified { get; set; }
    public int HelpfulCount { get; set; }
    public ReviewModerationStatus ModerationStatus { get; set; } = ReviewModerationStatus.Approved;
    public string? DoctorResponse { get; set; }
    public DateTime? DoctorRespondedAt { get; set; }
    public string PostedAs { get; set; } = string.Empty;
}

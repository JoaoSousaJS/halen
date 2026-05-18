using Halen.Domain.Enums;
using Halen.Domain.Interfaces;

namespace Halen.Domain.Entities;

public class RecordAccess : BaseEntity, ITenantScoped
{
    public Guid ClinicId { get; set; }
    public Clinic? Clinic { get; set; }

    public Guid PatientProfileId { get; set; }
    public PatientProfile PatientProfile { get; set; } = null!;

    public Guid GrantedToUserId { get; set; }
    public User GrantedToUser { get; set; } = null!;

    public RecordAccessLevel AccessLevel { get; set; }
    public DateTime GrantedAt { get; set; }

    public Guid GrantedByUserId { get; set; }
    public User GrantedByUser { get; set; } = null!;

    public DateTime? RevokedAt { get; set; }
    public string? Reason { get; set; }
}

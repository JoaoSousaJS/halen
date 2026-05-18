using Halen.Domain.Interfaces;

namespace Halen.Domain.Entities;

public class RecordAccessLog : BaseEntity, ITenantScoped
{
    public Guid ClinicId { get; set; }
    public Clinic? Clinic { get; set; }

    public Guid PatientProfileId { get; set; }
    public PatientProfile PatientProfile { get; set; } = null!;

    public Guid AccessedByUserId { get; set; }
    public User AccessedByUser { get; set; } = null!;

    public DateTime AccessedAt { get; set; }
    public string Action { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public Guid? ResourceId { get; set; }
    public string IpAddress { get; set; } = string.Empty;
}

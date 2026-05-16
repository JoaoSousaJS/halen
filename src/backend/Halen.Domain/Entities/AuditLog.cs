using Halen.Domain.Interfaces;

namespace Halen.Domain.Entities;

public class AuditLog : BaseEntity, ITenantScoped
{
    public Guid ClinicId { get; set; }
    public Clinic? Clinic { get; set; }

    public Guid ActorId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public string? Metadata { get; set; }
    public string IpAddress { get; set; } = string.Empty;
}

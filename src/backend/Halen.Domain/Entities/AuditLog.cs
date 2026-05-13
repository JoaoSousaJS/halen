namespace Halen.Domain.Entities;

public class AuditLog : BaseEntity
{
    public string ActorId { get; set; } = string.Empty;
    public string ActorName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public string? Metadata { get; set; }
    public string IpAddress { get; set; } = string.Empty;
}

namespace Halen.Domain.Entities;

public class ClinicFeatureFlag : BaseEntity
{
    public Guid ClinicId { get; set; }
    public Clinic Clinic { get; set; } = null!;
    public string FeatureKey { get; set; } = null!;
    public bool IsEnabled { get; set; }
}

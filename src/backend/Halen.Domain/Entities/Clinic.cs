namespace Halen.Domain.Entities;

public class Clinic : BaseEntity
{
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public ICollection<ClinicFeatureFlag> FeatureFlags { get; set; } = [];
}

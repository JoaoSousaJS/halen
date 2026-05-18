using Halen.Domain.Interfaces;

namespace Halen.Domain.Entities;

public class PatientFamilyHistory : BaseEntity, ITenantScoped
{
    public Guid ClinicId { get; set; }
    public Clinic? Clinic { get; set; }

    public Guid PatientProfileId { get; set; }
    public PatientProfile PatientProfile { get; set; } = null!;

    public string Relationship { get; set; } = string.Empty;
    public string ConditionName { get; set; } = string.Empty;
    public int? AgeAtOnset { get; set; }
    public string? Notes { get; set; }

    public Guid AddedByUserId { get; set; }
    public User AddedByUser { get; set; } = null!;
}

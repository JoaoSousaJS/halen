using Halen.Domain.Enums;
using Halen.Domain.Interfaces;

namespace Halen.Domain.Entities;

public class PatientAllergy : BaseEntity, ITenantScoped
{
    public Guid ClinicId { get; set; }
    public Clinic? Clinic { get; set; }

    public Guid PatientProfileId { get; set; }
    public PatientProfile PatientProfile { get; set; } = null!;

    public string AllergenName { get; set; } = string.Empty;
    public string? Reaction { get; set; }
    public ConditionSeverity Severity { get; set; }
    public DateOnly? DateIdentified { get; set; }
    public bool IsActive { get; set; } = true;

    public Guid AddedByUserId { get; set; }
    public User AddedByUser { get; set; } = null!;
}

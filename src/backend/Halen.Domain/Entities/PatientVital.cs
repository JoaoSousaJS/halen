using Halen.Domain.Enums;
using Halen.Domain.Interfaces;

namespace Halen.Domain.Entities;

public class PatientVital : BaseEntity, ITenantScoped
{
    public Guid ClinicId { get; set; }
    public Clinic? Clinic { get; set; }

    public Guid PatientProfileId { get; set; }
    public PatientProfile PatientProfile { get; set; } = null!;

    public VitalType VitalType { get; set; }
    public decimal Value { get; set; }
    public decimal? SecondaryValue { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTime MeasuredAt { get; set; }
    public VitalSource Source { get; set; }
    public string? Notes { get; set; }

    public Guid AddedByUserId { get; set; }
    public User AddedByUser { get; set; } = null!;
}

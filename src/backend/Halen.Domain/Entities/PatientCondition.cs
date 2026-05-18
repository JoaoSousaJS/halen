using Halen.Domain.Enums;
using Halen.Domain.Interfaces;

namespace Halen.Domain.Entities;

public class PatientCondition : BaseEntity, ITenantScoped
{
    public Guid ClinicId { get; set; }
    public Clinic? Clinic { get; set; }

    public Guid PatientProfileId { get; set; }
    public PatientProfile PatientProfile { get; set; } = null!;

    public string IcdCode { get; set; } = string.Empty;
    public string IcdDescription { get; set; } = string.Empty;
    public DateOnly? DateOfOnset { get; set; }
    public ConditionSeverity Severity { get; set; }
    public ConditionStatus Status { get; set; } = ConditionStatus.Active;
    public string? ClinicalNotes { get; set; }

    public Guid AddedByUserId { get; set; }
    public User AddedByUser { get; set; } = null!;

    public Guid? LinkedAppointmentId { get; set; }
    public Appointment? LinkedAppointment { get; set; }
}

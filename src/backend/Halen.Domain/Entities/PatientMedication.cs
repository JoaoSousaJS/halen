using Halen.Domain.Interfaces;

namespace Halen.Domain.Entities;

public class PatientMedication : BaseEntity, ITenantScoped
{
    public Guid ClinicId { get; set; }
    public Clinic? Clinic { get; set; }

    public Guid PatientProfileId { get; set; }
    public PatientProfile PatientProfile { get; set; } = null!;

    public string MedicationName { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public string? PrescribedByName { get; set; }

    public Guid? LinkedPrescriptionId { get; set; }
    public Prescription? LinkedPrescription { get; set; }

    public Guid AddedByUserId { get; set; }
    public User AddedByUser { get; set; } = null!;
}

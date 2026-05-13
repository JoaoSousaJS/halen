using Halen.Domain.Enums;

namespace Halen.Domain.Entities;

public class Prescription : BaseEntity
{
    public Guid PatientId { get; set; }
    public PatientProfile Patient { get; set; } = null!;

    public Guid DoctorId { get; set; }
    public DoctorProfile Doctor { get; set; } = null!;

    public string DrugName { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public int RefillsRemaining { get; set; }
    public PrescriptionStatus Status { get; set; } = PrescriptionStatus.Active;
    public string? PharmacyName { get; set; }
}

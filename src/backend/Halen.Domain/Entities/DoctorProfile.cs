namespace Halen.Domain.Entities;

public class DoctorProfile : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string Specialty { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public decimal ConsultationFee { get; set; }
    public int YearsOfExperience { get; set; }
    public string[] Languages { get; set; } = [];

    public ICollection<Appointment> Appointments { get; set; } = [];
    public ICollection<Prescription> Prescriptions { get; set; } = [];
}

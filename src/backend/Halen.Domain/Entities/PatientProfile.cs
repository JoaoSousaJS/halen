namespace Halen.Domain.Entities;

public class PatientProfile : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public DateOnly DateOfBirth { get; set; }
    public string City { get; set; } = string.Empty;
    public string SubscriptionPlan { get; set; } = "Essentials";

    public ICollection<Appointment> Appointments { get; set; } = [];
    public ICollection<Prescription> Prescriptions { get; set; } = [];
}

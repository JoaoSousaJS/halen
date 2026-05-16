using Halen.Domain.Enums;
using Halen.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Halen.Domain.Entities;

public class User : IdentityUser<Guid>, ITenantScoped
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public AccountStatus Status { get; set; } = AccountStatus.Active;
    public bool IsFlagged { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    public Guid ClinicId { get; set; }
    public Clinic? Clinic { get; set; }

    public DoctorProfile? DoctorProfile { get; set; }
    public PatientProfile? PatientProfile { get; set; }
}

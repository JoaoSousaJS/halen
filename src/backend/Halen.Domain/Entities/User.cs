using Halen.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Halen.Domain.Entities;

// Extends IdentityUser so ASP.NET Identity handles password hashing,
// login, lockout, etc. We just add our own fields on top.
public class User : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public AccountStatus Status { get; set; } = AccountStatus.Active;
    public bool IsFlagged { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    public DoctorProfile? DoctorProfile { get; set; }
    public PatientProfile? PatientProfile { get; set; }
}

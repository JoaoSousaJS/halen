using Halen.Application.Interfaces;
using Halen.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Halen.Infrastructure.Persistence;

// IdentityDbContext wires up all the Identity tables (AspNetUsers, AspNetRoles, etc.)
// We inherit from the generic version to use Guid as the primary key type.
public class HalenDbContext(DbContextOptions<HalenDbContext> options)
    : IdentityDbContext<User, IdentityRole<Guid>, Guid>(options), IAppDbContext
{
    public DbSet<DoctorProfile> DoctorProfiles => Set<DoctorProfile>();
    public DbSet<PatientProfile> PatientProfiles => Set<PatientProfile>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder); // required — sets up Identity tables

        builder.Entity<User>(e =>
        {
            e.HasOne(u => u.DoctorProfile)
                .WithOne(d => d.User)
                .HasForeignKey<DoctorProfile>(d => d.UserId);

            e.HasOne(u => u.PatientProfile)
                .WithOne(p => p.User)
                .HasForeignKey<PatientProfile>(p => p.UserId);
        });

        builder.Entity<Appointment>(e =>
        {
            e.HasIndex(a => new { a.DoctorId, a.ScheduledAt });
            e.HasIndex(a => a.PatientId);
        });

        builder.Entity<DoctorProfile>(e =>
        {
            e.Property(d => d.Languages).HasColumnType("text[]");
            e.HasIndex(d => d.LicenseNumber).IsUnique();
        });

        // Store enums as strings for readability in the DB
        builder.Entity<Appointment>()
            .Property(a => a.Status)
            .HasConversion<string>();

        builder.Entity<Prescription>()
            .Property(p => p.Status)
            .HasConversion<string>();

        builder.Entity<User>()
            .Property(u => u.Role)
            .HasConversion<string>();
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        // Auto-update UpdatedAt on every save
        foreach (var entry in ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.State == EntityState.Modified))
        {
            entry.Entity.UpdatedAt = DateTime.UtcNow;
        }

        return base.SaveChangesAsync(ct);
    }
}

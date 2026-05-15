using Halen.Application.Interfaces;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Halen.Infrastructure.Persistence;

// IdentityDbContext wires up all the Identity tables (AspNetUsers, AspNetRoles, etc.)
// We inherit from the generic version to use Guid as the primary key type.
public class HalenDbContext(DbContextOptions<HalenDbContext> options)
    : IdentityDbContext<User, IdentityRole<Guid>, Guid>(options), IAppDbContext
{
    public new DbSet<User> Users => Set<User>();
    public DbSet<DoctorProfile> DoctorProfiles => Set<DoctorProfile>();
    public DbSet<PatientProfile> PatientProfiles => Set<PatientProfile>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<KycDocument> KycDocuments => Set<KycDocument>();
    public DbSet<KycReview> KycReviews => Set<KycReview>();
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

            e.HasIndex(u => u.Role);
            e.HasIndex(u => new { u.IsFlagged, u.LastLoginAt });
        });

        builder.Entity<Appointment>(e =>
        {
            e.HasIndex(a => new { a.DoctorId, a.Status, a.ScheduledAt });
            e.HasIndex(a => a.PatientId);
        });

        builder.Entity<Prescription>(e =>
        {
            e.HasIndex(p => new { p.DoctorId, p.Status });
            e.HasIndex(p => new { p.PatientId, p.Status });
        });

        builder.Entity<AuditLog>(e =>
        {
            e.HasIndex(a => a.ActorId);
            e.HasIndex(a => a.CreatedAt);
        });

        builder.Entity<DoctorProfile>(e =>
        {
            e.Property(d => d.Languages).HasColumnType("text[]");
            e.HasIndex(d => d.LicenseNumber).IsUnique();
            e.Property(d => d.KycStatus).HasConversion<string>().HasDefaultValue(KycStatus.NotSubmitted);
        });

        builder.Entity<KycDocument>(e =>
        {
            e.HasOne(d => d.DoctorProfile).WithMany(p => p.KycDocuments).HasForeignKey(d => d.DoctorProfileId);
            e.Property(d => d.DocumentType).HasConversion<string>();
            e.Property(d => d.FileName).HasMaxLength(256);
            e.Property(d => d.FilePath).HasMaxLength(1024);
            e.Property(d => d.ContentType).HasMaxLength(100);
        });

        builder.Entity<KycReview>(e =>
        {
            e.HasOne(r => r.DoctorProfile).WithMany(p => p.KycReviews).HasForeignKey(r => r.DoctorProfileId);
            e.HasOne(r => r.ReviewedByUser).WithMany().HasForeignKey(r => r.ReviewedByUserId).OnDelete(DeleteBehavior.Restrict);
            e.Property(r => r.Decision).HasConversion<string>();
            e.Property(r => r.RejectionReason).HasMaxLength(1000);
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

        builder.Entity<User>()
            .Property(u => u.Status)
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

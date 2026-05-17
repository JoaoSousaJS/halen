using Halen.Application.Interfaces;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Halen.Infrastructure.Persistence;

public class HalenDbContext(DbContextOptions<HalenDbContext> options, ITenantContext tenantContext)
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
    /// <summary>Intentionally unfiltered — Clinic is the tenant root, not scoped to another tenant.</summary>
    public DbSet<Clinic> Clinics => Set<Clinic>();
    /// <summary>Intentionally unfiltered — managed cross-tenant by PlatformAdmin; handlers scope by ClinicId explicitly.</summary>
    public DbSet<ClinicFeatureFlag> ClinicFeatureFlags => Set<ClinicFeatureFlag>();

    private Guid TenantClinicId => tenantContext.ClinicId;
    private bool IsPlatformAdmin => tenantContext.IsPlatformAdmin;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ── PostgreSQL extensions ────────────────────────────────────────────
        builder.HasPostgresExtension("pg_trgm");

        // ── Tenant query filters ─────────────────────────────────────────────
        // EF Core evaluates these per-query using the current service scope.
        // PlatformAdmin bypasses filters automatically.
        builder.Entity<User>().HasQueryFilter(e => IsPlatformAdmin || e.ClinicId == TenantClinicId);
        builder.Entity<DoctorProfile>().HasQueryFilter(e => IsPlatformAdmin || e.ClinicId == TenantClinicId);
        builder.Entity<PatientProfile>().HasQueryFilter(e => IsPlatformAdmin || e.ClinicId == TenantClinicId);
        builder.Entity<Appointment>().HasQueryFilter(e => IsPlatformAdmin || e.ClinicId == TenantClinicId);
        builder.Entity<Prescription>().HasQueryFilter(e => IsPlatformAdmin || e.ClinicId == TenantClinicId);
        builder.Entity<KycDocument>().HasQueryFilter(e => IsPlatformAdmin || e.ClinicId == TenantClinicId);
        builder.Entity<KycReview>().HasQueryFilter(e => IsPlatformAdmin || e.ClinicId == TenantClinicId);
        builder.Entity<AuditLog>().HasQueryFilter(e => IsPlatformAdmin || e.ClinicId == TenantClinicId);

        // ── Clinic ───────────────────────────────────────────────────────────
        builder.Entity<Clinic>(e =>
        {
            e.Property(c => c.Name).HasMaxLength(200).IsRequired();
            e.Property(c => c.Slug).HasMaxLength(100).IsRequired();
            e.HasIndex(c => c.Slug).IsUnique();
            e.HasIndex(c => c.Name)
                .HasMethod("gin")
                .HasOperators("gin_trgm_ops");
        });

        // ── ClinicFeatureFlag ────────────────────────────────────────────────
        builder.Entity<ClinicFeatureFlag>(e =>
        {
            e.Property(f => f.FeatureKey).HasMaxLength(100).IsRequired();
            e.HasIndex(f => new { f.ClinicId, f.FeatureKey }).IsUnique();
            e.HasOne(f => f.Clinic).WithMany(c => c.FeatureFlags).HasForeignKey(f => f.ClinicId);
        });

        // ── User ─────────────────────────────────────────────────────────────
        builder.Entity<User>(e =>
        {
            e.HasOne(u => u.DoctorProfile)
                .WithOne(d => d.User)
                .HasForeignKey<DoctorProfile>(d => d.UserId);

            e.HasOne(u => u.PatientProfile)
                .WithOne(p => p.User)
                .HasForeignKey<PatientProfile>(p => p.UserId);

            e.HasOne(u => u.Clinic).WithMany().HasForeignKey(u => u.ClinicId);
            e.HasIndex(u => u.ClinicId);
            e.HasIndex(u => u.Role);
            e.HasIndex(u => new { u.IsFlagged, u.LastLoginAt });
            e.HasIndex(u => u.FirstName)
                .HasMethod("gin")
                .HasOperators("gin_trgm_ops");
            e.HasIndex(u => u.LastName)
                .HasMethod("gin")
                .HasOperators("gin_trgm_ops");
        });

        // ── Appointment ──────────────────────────────────────────────────────
        builder.Entity<Appointment>(e =>
        {
            e.HasOne(a => a.Clinic).WithMany().HasForeignKey(a => a.ClinicId);
            e.HasIndex(a => new { a.ClinicId, a.DoctorId, a.Status, a.ScheduledAt });
            e.HasIndex(a => new { a.ClinicId, a.PatientId });
            e.Property(a => a.Reason).HasMaxLength(500);
            e.Property(a => a.Notes).HasMaxLength(2000);
        });

        // ── Prescription ─────────────────────────────────────────────────────
        builder.Entity<Prescription>(e =>
        {
            e.HasOne(p => p.Clinic).WithMany().HasForeignKey(p => p.ClinicId);
            e.HasIndex(p => new { p.ClinicId, p.DoctorId, p.Status });
            e.HasIndex(p => new { p.ClinicId, p.PatientId, p.Status });
            e.Property(p => p.DrugName).HasMaxLength(200);
            e.Property(p => p.Dosage).HasMaxLength(100);
            e.Property(p => p.Frequency).HasMaxLength(100);
            e.Property(p => p.PharmacyName).HasMaxLength(200);
        });

        // ── AuditLog ─────────────────────────────────────────────────────────
        builder.Entity<AuditLog>(e =>
        {
            e.HasOne(a => a.Clinic).WithMany().HasForeignKey(a => a.ClinicId);
            e.HasIndex(a => new { a.ClinicId, a.ActorId });
            e.HasIndex(a => new { a.ClinicId, a.CreatedAt });
            e.Property(a => a.Action).HasMaxLength(100);
        });

        // ── DoctorProfile ────────────────────────────────────────────────────
        builder.Entity<DoctorProfile>(e =>
        {
            e.HasOne(d => d.Clinic).WithMany().HasForeignKey(d => d.ClinicId);
            e.Property(d => d.Languages).HasColumnType("text[]");
            e.HasIndex(d => d.LicenseNumber).IsUnique();
            e.Property(d => d.KycStatus).HasConversion<string>().HasDefaultValue(KycStatus.NotSubmitted);
            e.Property(d => d.ConsultationFee).HasPrecision(10, 2);
            e.Property(d => d.Specialty).HasMaxLength(100);
            e.Property(d => d.LicenseNumber).HasMaxLength(50);
            e.HasIndex(d => d.KycStatus);
        });

        // ── PatientProfile ───────────────────────────────────────────────────
        builder.Entity<PatientProfile>(e =>
        {
            e.HasOne(p => p.Clinic).WithMany().HasForeignKey(p => p.ClinicId);
        });

        // ── KycDocument ──────────────────────────────────────────────────────
        builder.Entity<KycDocument>(e =>
        {
            e.HasOne(d => d.Clinic).WithMany().HasForeignKey(d => d.ClinicId);
            e.HasOne(d => d.DoctorProfile).WithMany(p => p.KycDocuments).HasForeignKey(d => d.DoctorProfileId);
            e.Property(d => d.DocumentType).HasConversion<string>();
            e.Property(d => d.FileName).HasMaxLength(256);
            e.Property(d => d.FilePath).HasMaxLength(1024);
            e.Property(d => d.ContentType).HasMaxLength(100);
        });

        // ── KycReview ────────────────────────────────────────────────────────
        builder.Entity<KycReview>(e =>
        {
            e.HasOne(r => r.Clinic).WithMany().HasForeignKey(r => r.ClinicId);
            e.HasOne(r => r.DoctorProfile).WithMany(p => p.KycReviews).HasForeignKey(r => r.DoctorProfileId);
            e.HasOne(r => r.ReviewedByUser).WithMany().HasForeignKey(r => r.ReviewedByUserId).OnDelete(DeleteBehavior.Restrict);
            e.Property(r => r.Decision).HasConversion<string>();
            e.Property(r => r.RejectionReason).HasMaxLength(1000);
        });

        // ── Enum conversions (stored as strings) ─────────────────────────────
        builder.Entity<Appointment>().Property(a => a.Status).HasConversion<string>();
        builder.Entity<Prescription>().Property(p => p.Status).HasConversion<string>();
        builder.Entity<User>().Property(u => u.Role).HasConversion<string>();
        builder.Entity<User>().Property(u => u.Status).HasConversion<string>();
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.State == EntityState.Modified))
        {
            entry.Entity.UpdatedAt = DateTime.UtcNow;
        }

        return base.SaveChangesAsync(ct);
    }
}

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
    public DbSet<DoctorAvailability> DoctorAvailabilities => Set<DoctorAvailability>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<ConsultationRoom> ConsultationRooms => Set<ConsultationRoom>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<ReviewHelpfulVote> ReviewHelpfulVotes => Set<ReviewHelpfulVote>();
    public DbSet<PatientCondition> PatientConditions => Set<PatientCondition>();
    public DbSet<PatientAllergy> PatientAllergies => Set<PatientAllergy>();
    public DbSet<PatientVital> PatientVitals => Set<PatientVital>();
    public DbSet<PatientMedication> PatientMedications => Set<PatientMedication>();
    public DbSet<PatientFamilyHistory> PatientFamilyHistories => Set<PatientFamilyHistory>();
    public DbSet<MedicalDocument> MedicalDocuments => Set<MedicalDocument>();
    public DbSet<RecordAccess> RecordAccesses => Set<RecordAccess>();
    public DbSet<RecordAccessLog> RecordAccessLogs => Set<RecordAccessLog>();
    public DbSet<ConversationThread> ConversationThreads => Set<ConversationThread>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<MessageAttachment> MessageAttachments => Set<MessageAttachment>();
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
        builder.Entity<DoctorAvailability>().HasQueryFilter(e => IsPlatformAdmin || e.ClinicId == TenantClinicId);
        builder.Entity<AuditLog>().HasQueryFilter(e => IsPlatformAdmin || e.ClinicId == TenantClinicId);
        builder.Entity<Payment>().HasQueryFilter(e => IsPlatformAdmin || e.ClinicId == TenantClinicId);
        builder.Entity<ConsultationRoom>().HasQueryFilter(e => IsPlatformAdmin || e.ClinicId == TenantClinicId);
        builder.Entity<Review>().HasQueryFilter(e => IsPlatformAdmin || e.ClinicId == TenantClinicId);
        builder.Entity<ReviewHelpfulVote>().HasQueryFilter(e => IsPlatformAdmin || e.ClinicId == TenantClinicId);
        builder.Entity<PatientCondition>().HasQueryFilter(e => IsPlatformAdmin || e.ClinicId == TenantClinicId);
        builder.Entity<PatientAllergy>().HasQueryFilter(e => IsPlatformAdmin || e.ClinicId == TenantClinicId);
        builder.Entity<PatientVital>().HasQueryFilter(e => IsPlatformAdmin || e.ClinicId == TenantClinicId);
        builder.Entity<PatientMedication>().HasQueryFilter(e => IsPlatformAdmin || e.ClinicId == TenantClinicId);
        builder.Entity<PatientFamilyHistory>().HasQueryFilter(e => IsPlatformAdmin || e.ClinicId == TenantClinicId);
        builder.Entity<MedicalDocument>().HasQueryFilter(e => IsPlatformAdmin || e.ClinicId == TenantClinicId);
        builder.Entity<RecordAccess>().HasQueryFilter(e => IsPlatformAdmin || e.ClinicId == TenantClinicId);
        builder.Entity<RecordAccessLog>().HasQueryFilter(e => IsPlatformAdmin || e.ClinicId == TenantClinicId);
        builder.Entity<ConversationThread>().HasQueryFilter(e => IsPlatformAdmin || e.ClinicId == TenantClinicId);
        builder.Entity<ChatMessage>().HasQueryFilter(e => IsPlatformAdmin || e.ClinicId == TenantClinicId);
        builder.Entity<MessageAttachment>().HasQueryFilter(e => IsPlatformAdmin || e.ClinicId == TenantClinicId);

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
            e.HasIndex(a => new { a.ClinicId, a.TargetId });
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

            // Search indexes for doctor search & filtering
            e.HasIndex(d => d.Specialty)
                .HasMethod("gin")
                .HasOperators("gin_trgm_ops");
            e.HasIndex(d => d.ConsultationFee);
            e.Property(d => d.AverageRating).HasPrecision(3, 2);
            e.Property(d => d.ReviewCount).HasDefaultValue(0);
        });

        // ── DoctorAvailability ────────────────────────────────────────────────
        builder.Entity<DoctorAvailability>(e =>
        {
            e.HasOne(a => a.Clinic).WithMany().HasForeignKey(a => a.ClinicId);
            e.HasOne(a => a.DoctorProfile).WithMany(d => d.Availabilities).HasForeignKey(a => a.DoctorProfileId);
            e.HasIndex(a => new { a.ClinicId, a.DoctorProfileId, a.DayOfWeek });
            e.Property(a => a.DayOfWeek).HasConversion<string>();
            e.Property(a => a.StartTime).HasColumnType("time");
            e.Property(a => a.EndTime).HasColumnType("time");
            e.Property(a => a.SlotDurationMinutes).HasDefaultValue(20);
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

        // ── Payment ──────────────────────────────────────────────────────────
        builder.Entity<Payment>(e =>
        {
            e.HasOne(p => p.Clinic).WithMany().HasForeignKey(p => p.ClinicId);
            e.HasOne(p => p.Appointment).WithOne(a => a.Payment).HasForeignKey<Payment>(p => p.AppointmentId);
            e.HasOne(p => p.PatientProfile).WithMany().HasForeignKey(p => p.PatientProfileId);
            e.Property(p => p.Amount).HasPrecision(10, 2);
            e.Property(p => p.Currency).HasMaxLength(3);
            e.Property(p => p.IdempotencyKey).HasMaxLength(200);
            e.Property(p => p.PaymentIntentId).HasMaxLength(200);
            e.Property(p => p.FailureReason).HasMaxLength(500);
            e.HasIndex(p => new { p.ClinicId, p.AppointmentId }).IsUnique();
            e.HasIndex(p => p.IdempotencyKey).IsUnique();
        });

        // ── ConsultationRoom ─────────────────────────────────────────────────
        builder.Entity<ConsultationRoom>(e =>
        {
            e.HasOne(r => r.Clinic).WithMany().HasForeignKey(r => r.ClinicId);
            e.HasOne(r => r.Appointment).WithOne(a => a.ConsultationRoom)
                .HasForeignKey<ConsultationRoom>(r => r.AppointmentId);
            e.HasIndex(r => r.AppointmentId).IsUnique();
            e.HasIndex(r => new { r.ClinicId, r.Status });
            e.HasIndex(r => r.RoomCode).IsUnique();
            e.Property(r => r.RoomCode).HasMaxLength(20).IsRequired();
            e.Property(r => r.Notes).HasMaxLength(5000);
            e.Property(r => r.Status).HasConversion<string>();
            e.Property<uint>("xmin").IsRowVersion();
        });

        // ── Review ────────────────────────────────────────────────────────────
        builder.Entity<Review>(e =>
        {
            e.HasOne(r => r.Clinic).WithMany().HasForeignKey(r => r.ClinicId);
            e.HasOne(r => r.Appointment).WithMany().HasForeignKey(r => r.AppointmentId);
            e.HasOne(r => r.PatientProfile).WithMany().HasForeignKey(r => r.PatientProfileId);
            e.HasOne(r => r.DoctorProfile).WithMany(d => d.Reviews).HasForeignKey(r => r.DoctorProfileId);
            e.HasIndex(r => r.AppointmentId).IsUnique();
            e.HasIndex(r => new { r.ClinicId, r.DoctorProfileId, r.ModerationStatus, r.CreatedAt });
            e.Property(r => r.Title).HasMaxLength(120).IsRequired();
            e.Property(r => r.Body).HasMaxLength(600);
            e.Property(r => r.Tags).HasColumnType("text[]");
            e.Property(r => r.ModerationStatus).HasConversion<string>().HasDefaultValue(ReviewModerationStatus.Approved);
            e.Property(r => r.DoctorResponse).HasMaxLength(600);
            e.Property(r => r.PostedAs).HasMaxLength(50).IsRequired();
            e.ToTable(t => t.HasCheckConstraint("CK_Review_Rating", "\"Rating\" >= 1 AND \"Rating\" <= 5"));
        });

        // ── ReviewHelpfulVote ────────────────────────────────────────────────
        builder.Entity<ReviewHelpfulVote>(e =>
        {
            e.HasOne(v => v.Review).WithMany().HasForeignKey(v => v.ReviewId);
            e.HasIndex(v => new { v.ReviewId, v.UserId }).IsUnique();
        });

        // ── PatientCondition ─────────────────────────────────────────────────
        builder.Entity<PatientCondition>(e =>
        {
            e.HasOne(c => c.Clinic).WithMany().HasForeignKey(c => c.ClinicId);
            e.HasOne(c => c.PatientProfile).WithMany().HasForeignKey(c => c.PatientProfileId);
            e.HasOne(c => c.AddedByUser).WithMany().HasForeignKey(c => c.AddedByUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(c => c.LinkedAppointment).WithMany().HasForeignKey(c => c.LinkedAppointmentId);
            e.HasIndex(c => new { c.ClinicId, c.PatientProfileId });
            e.Property(c => c.IcdCode).HasMaxLength(20);
            e.Property(c => c.IcdDescription).HasMaxLength(500);
            e.Property(c => c.ClinicalNotes).HasMaxLength(2000);
            e.Property(c => c.Severity).HasConversion<string>();
            e.Property(c => c.Status).HasConversion<string>();
        });

        // ── PatientAllergy ──────────────────────────────────────────────────
        builder.Entity<PatientAllergy>(e =>
        {
            e.HasOne(a => a.Clinic).WithMany().HasForeignKey(a => a.ClinicId);
            e.HasOne(a => a.PatientProfile).WithMany().HasForeignKey(a => a.PatientProfileId);
            e.HasOne(a => a.AddedByUser).WithMany().HasForeignKey(a => a.AddedByUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(a => new { a.ClinicId, a.PatientProfileId });
            e.Property(a => a.AllergenName).HasMaxLength(200);
            e.Property(a => a.Reaction).HasMaxLength(500);
            e.Property(a => a.Severity).HasConversion<string>();
        });

        // ── PatientVital ────────────────────────────────────────────────────
        builder.Entity<PatientVital>(e =>
        {
            e.HasOne(v => v.Clinic).WithMany().HasForeignKey(v => v.ClinicId);
            e.HasOne(v => v.PatientProfile).WithMany().HasForeignKey(v => v.PatientProfileId);
            e.HasOne(v => v.AddedByUser).WithMany().HasForeignKey(v => v.AddedByUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(v => new { v.ClinicId, v.PatientProfileId, v.VitalType, v.MeasuredAt });
            e.Property(v => v.Value).HasPrecision(10, 2);
            e.Property(v => v.SecondaryValue).HasPrecision(10, 2);
            e.Property(v => v.Unit).HasMaxLength(20);
            e.Property(v => v.Notes).HasMaxLength(500);
            e.Property(v => v.VitalType).HasConversion<string>();
            e.Property(v => v.Source).HasConversion<string>();
        });

        // ── PatientMedication ───────────────────────────────────────────────
        builder.Entity<PatientMedication>(e =>
        {
            e.HasOne(m => m.Clinic).WithMany().HasForeignKey(m => m.ClinicId);
            e.HasOne(m => m.PatientProfile).WithMany().HasForeignKey(m => m.PatientProfileId);
            e.HasOne(m => m.AddedByUser).WithMany().HasForeignKey(m => m.AddedByUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.LinkedPrescription).WithMany().HasForeignKey(m => m.LinkedPrescriptionId);
            e.HasIndex(m => new { m.ClinicId, m.PatientProfileId });
            e.Property(m => m.MedicationName).HasMaxLength(200);
            e.Property(m => m.Dosage).HasMaxLength(100);
            e.Property(m => m.Frequency).HasMaxLength(100);
            e.Property(m => m.PrescribedByName).HasMaxLength(200);
        });

        // ── PatientFamilyHistory ────────────────────────────────────────────
        builder.Entity<PatientFamilyHistory>(e =>
        {
            e.HasOne(f => f.Clinic).WithMany().HasForeignKey(f => f.ClinicId);
            e.HasOne(f => f.PatientProfile).WithMany().HasForeignKey(f => f.PatientProfileId);
            e.HasOne(f => f.AddedByUser).WithMany().HasForeignKey(f => f.AddedByUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(f => new { f.ClinicId, f.PatientProfileId });
            e.Property(f => f.Relationship).HasMaxLength(100);
            e.Property(f => f.ConditionName).HasMaxLength(200);
            e.Property(f => f.Notes).HasMaxLength(1000);
        });

        // ── MedicalDocument ─────────────────────────────────────────────────
        builder.Entity<MedicalDocument>(e =>
        {
            e.HasOne(d => d.Clinic).WithMany().HasForeignKey(d => d.ClinicId);
            e.HasOne(d => d.PatientProfile).WithMany().HasForeignKey(d => d.PatientProfileId);
            e.HasOne(d => d.UploadedByUser).WithMany().HasForeignKey(d => d.UploadedByUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(d => d.LinkedAppointment).WithMany().HasForeignKey(d => d.LinkedAppointmentId);
            e.HasIndex(d => new { d.ClinicId, d.PatientProfileId });
            e.Property(d => d.DocumentType).HasConversion<string>();
            e.Property(d => d.Title).HasMaxLength(200);
            e.Property(d => d.Description).HasMaxLength(500);
            e.Property(d => d.FileName).HasMaxLength(256);
            e.Property(d => d.FilePath).HasMaxLength(1024);
            e.Property(d => d.ContentType).HasMaxLength(100);
        });

        // ── RecordAccess ────────────────────────────────────────────────────
        builder.Entity<RecordAccess>(e =>
        {
            e.HasOne(r => r.Clinic).WithMany().HasForeignKey(r => r.ClinicId);
            e.HasOne(r => r.PatientProfile).WithMany().HasForeignKey(r => r.PatientProfileId);
            e.HasOne(r => r.GrantedToUser).WithMany().HasForeignKey(r => r.GrantedToUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(r => r.GrantedByUser).WithMany().HasForeignKey(r => r.GrantedByUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(r => new { r.ClinicId, r.PatientProfileId, r.GrantedToUserId }).IsUnique();
            e.Property(r => r.AccessLevel).HasConversion<string>();
            e.Property(r => r.Reason).HasMaxLength(500);
        });

        // ── RecordAccessLog ─────────────────────────────────────────────────
        builder.Entity<RecordAccessLog>(e =>
        {
            e.HasOne(r => r.Clinic).WithMany().HasForeignKey(r => r.ClinicId);
            e.HasOne(r => r.PatientProfile).WithMany().HasForeignKey(r => r.PatientProfileId);
            e.HasOne(r => r.AccessedByUser).WithMany().HasForeignKey(r => r.AccessedByUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(r => new { r.ClinicId, r.PatientProfileId, r.AccessedAt });
            e.Property(r => r.Action).HasMaxLength(100);
            e.Property(r => r.ResourceType).HasMaxLength(100);
            e.Property(r => r.IpAddress).HasMaxLength(45);
        });

        // ── ConversationThread ──────────────────────────────────────────────
        builder.Entity<ConversationThread>(e =>
        {
            e.HasOne(t => t.Clinic).WithMany().HasForeignKey(t => t.ClinicId);
            e.HasOne(t => t.Appointment).WithOne(a => a.ConversationThread)
                .HasForeignKey<ConversationThread>(t => t.AppointmentId);
            e.HasOne(t => t.PatientUser).WithMany().HasForeignKey(t => t.PatientUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(t => t.DoctorUser).WithMany().HasForeignKey(t => t.DoctorUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(t => t.AppointmentId).IsUnique();
            e.HasIndex(t => new { t.PatientUserId, t.LastMessageAt });
            e.HasIndex(t => new { t.DoctorUserId, t.LastMessageAt });
            e.Property(t => t.Subject).HasMaxLength(200);
            e.Property(t => t.LastMessagePreview).HasMaxLength(200);
            e.Property(t => t.Status).HasConversion<string>();
            e.Property<uint>("xmin").IsRowVersion();
        });

        // ── ChatMessage ─────────────────────────────────────────────────────
        builder.Entity<ChatMessage>(e =>
        {
            e.HasOne(m => m.Clinic).WithMany().HasForeignKey(m => m.ClinicId);
            e.HasOne(m => m.Thread).WithMany(t => t.Messages).HasForeignKey(m => m.ThreadId);
            e.HasOne(m => m.SenderUser).WithMany().HasForeignKey(m => m.SenderUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(m => new { m.ThreadId, m.CreatedAt });
            e.Property(m => m.Content).HasMaxLength(4000);
            e.Property(m => m.MessageType).HasConversion<string>();
        });

        // ── MessageAttachment ───────────────────────────────────────────────
        builder.Entity<MessageAttachment>(e =>
        {
            e.HasOne(a => a.Clinic).WithMany().HasForeignKey(a => a.ClinicId);
            e.HasOne(a => a.Message).WithMany(m => m.Attachments).HasForeignKey(a => a.MessageId);
            e.Property(a => a.FileName).HasMaxLength(255);
            e.Property(a => a.ContentType).HasMaxLength(100);
            e.Property(a => a.StoragePath).HasMaxLength(500);
            e.Property(a => a.AttachmentType).HasConversion<string>();
        });

        // ── Enum conversions (stored as strings) ─────────────────────────────
        builder.Entity<Payment>().Property(p => p.Status).HasConversion<string>();
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

using Halen.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Halen.Application.Interfaces;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Appointment> Appointments { get; }
    DbSet<DoctorProfile> DoctorProfiles { get; }
    DbSet<PatientProfile> PatientProfiles { get; }
    DbSet<Prescription> Prescriptions { get; }
    DbSet<KycDocument> KycDocuments { get; }
    DbSet<KycReview> KycReviews { get; }
    DbSet<Clinic> Clinics { get; }
    DbSet<ClinicFeatureFlag> ClinicFeatureFlags { get; }
    DbSet<DoctorAvailability> DoctorAvailabilities { get; }
    DbSet<Payment> Payments { get; }
    DbSet<ConsultationRoom> ConsultationRooms { get; }
    DbSet<Review> Reviews { get; }
    DbSet<ReviewHelpfulVote> ReviewHelpfulVotes { get; }
    DbSet<PatientCondition> PatientConditions { get; }
    DbSet<PatientAllergy> PatientAllergies { get; }
    DbSet<PatientVital> PatientVitals { get; }
    DbSet<PatientMedication> PatientMedications { get; }
    DbSet<PatientFamilyHistory> PatientFamilyHistories { get; }
    DbSet<MedicalDocument> MedicalDocuments { get; }
    DbSet<RecordAccess> RecordAccesses { get; }
    DbSet<RecordAccessLog> RecordAccessLogs { get; }
    DbSet<ConversationThread> ConversationThreads { get; }
    DbSet<ChatMessage> ChatMessages { get; }
    DbSet<MessageAttachment> MessageAttachments { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

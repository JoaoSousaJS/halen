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
    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

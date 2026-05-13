using Halen.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Halen.Application.Interfaces;

public interface IAppDbContext
{
    DbSet<Appointment> Appointments { get; }
    DbSet<DoctorProfile> DoctorProfiles { get; }
    DbSet<PatientProfile> PatientProfiles { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

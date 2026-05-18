using Halen.Application.Interfaces;
using Halen.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Halen.Application.MedicalRecords;

public class RecordAccessChecker(IAppDbContext db) : IRecordAccessChecker
{
    public async Task<bool> CanAccessPatientRecord(
        Guid callerUserId, UserRole callerRole, Guid patientProfileId, CancellationToken ct)
    {
        switch (callerRole)
        {
            case UserRole.PlatformAdmin:
                return true;

            case UserRole.Patient:
                return await db.PatientProfiles
                    .AsNoTracking()
                    .AnyAsync(p => p.Id == patientProfileId && p.UserId == callerUserId, ct);

            case UserRole.Doctor:
                var doctorProfile = await db.DoctorProfiles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.UserId == callerUserId, ct);

                if (doctorProfile is null)
                    return false;

                var hasAppointment = await db.Appointments
                    .AsNoTracking()
                    .AnyAsync(a =>
                        a.DoctorId == doctorProfile.Id &&
                        a.PatientId == patientProfileId &&
                        (a.Status == AppointmentStatus.Scheduled || a.Status == AppointmentStatus.Completed), ct);

                if (hasAppointment)
                    return true;

                return await db.RecordAccesses
                    .AsNoTracking()
                    .AnyAsync(ra =>
                        ra.GrantedToUserId == callerUserId &&
                        ra.PatientProfileId == patientProfileId &&
                        ra.AccessLevel != RecordAccessLevel.Revoked, ct);

            default:
                return false;
        }
    }
}

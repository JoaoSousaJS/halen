using FluentAssertions;
using Halen.Application.MedicalRecords;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.Infrastructure.Persistence;
using Halen.UnitTests.Helpers;

namespace Halen.UnitTests.MedicalRecords;

[TestClass]
public class RecordAccessCheckerTests
{
    private HalenDbContext _db = null!;
    private RecordAccessChecker _checker = null!;

    private Guid _patientUserId;
    private Guid _patientProfileId;
    private Guid _doctorUserId;
    private Guid _doctorProfileId;
    private Guid _adminUserId;

    [TestInitialize]
    public async Task Initialize()
    {
        _db = TestDbFactory.Create();

        _patientUserId = Guid.NewGuid();
        _patientProfileId = Guid.NewGuid();
        _doctorUserId = Guid.NewGuid();
        _doctorProfileId = Guid.NewGuid();
        _adminUserId = Guid.NewGuid();

        _db.Users.AddRange(
            new User { Id = _patientUserId, FirstName = "Pat", LastName = "Ient", Email = "pat@test.com", UserName = "pat@test.com", Role = UserRole.Patient },
            new User { Id = _doctorUserId, FirstName = "Dr", LastName = "House", Email = "doc@test.com", UserName = "doc@test.com", Role = UserRole.Doctor },
            new User { Id = _adminUserId, FirstName = "Admin", LastName = "User", Email = "admin@test.com", UserName = "admin@test.com", Role = UserRole.PlatformAdmin }
        );

        _db.PatientProfiles.Add(new PatientProfile { Id = _patientProfileId, UserId = _patientUserId });

        _db.DoctorProfiles.Add(new DoctorProfile
        {
            Id = _doctorProfileId, UserId = _doctorUserId,
            Specialty = "General", LicenseNumber = "LIC-001",
            ConsultationFee = 100, YearsOfExperience = 5,
            KycStatus = KycStatus.Approved,
        });

        // Doctor has a scheduled appointment with this patient
        _db.Appointments.Add(new Appointment
        {
            DoctorId = _doctorProfileId,
            PatientId = _patientProfileId,
            ScheduledAt = DateTime.UtcNow.AddDays(1),
            Reason = "Checkup",
            Status = AppointmentStatus.Scheduled,
        });

        await _db.SaveChangesAsync();
        _checker = new RecordAccessChecker(_db);
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Patient_CanAccessOwnRecord()
    {
        var result = await _checker.CanAccessPatientRecord(
            _patientUserId, UserRole.Patient, _patientProfileId, CancellationToken.None);

        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task Patient_CannotAccessOtherPatientsRecord()
    {
        var otherPatientProfileId = Guid.NewGuid();
        var otherPatientUserId = Guid.NewGuid();
        _db.Users.Add(new User { Id = otherPatientUserId, FirstName = "Other", LastName = "Pat", Email = "other@test.com", UserName = "other@test.com", Role = UserRole.Patient });
        _db.PatientProfiles.Add(new PatientProfile { Id = otherPatientProfileId, UserId = otherPatientUserId });
        await _db.SaveChangesAsync();

        var result = await _checker.CanAccessPatientRecord(
            _patientUserId, UserRole.Patient, otherPatientProfileId, CancellationToken.None);

        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task Doctor_CanAccessPatientWithAppointment()
    {
        var result = await _checker.CanAccessPatientRecord(
            _doctorUserId, UserRole.Doctor, _patientProfileId, CancellationToken.None);

        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task Doctor_CannotAccessPatientWithoutAppointment()
    {
        // Create a patient with no appointments with this doctor
        var otherPatientProfileId = Guid.NewGuid();
        var otherPatientUserId = Guid.NewGuid();
        _db.Users.Add(new User { Id = otherPatientUserId, FirstName = "Other", LastName = "Pat", Email = "other2@test.com", UserName = "other2@test.com", Role = UserRole.Patient });
        _db.PatientProfiles.Add(new PatientProfile { Id = otherPatientProfileId, UserId = otherPatientUserId });
        await _db.SaveChangesAsync();

        var result = await _checker.CanAccessPatientRecord(
            _doctorUserId, UserRole.Doctor, otherPatientProfileId, CancellationToken.None);

        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task PlatformAdmin_AlwaysHasAccess()
    {
        var result = await _checker.CanAccessPatientRecord(
            _adminUserId, UserRole.PlatformAdmin, _patientProfileId, CancellationToken.None);

        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task ClinicAdmin_IsDenied()
    {
        var clinicAdminUserId = Guid.NewGuid();
        _db.Users.Add(new User { Id = clinicAdminUserId, FirstName = "Clinic", LastName = "Admin", Email = "cadmin@test.com", UserName = "cadmin@test.com", Role = UserRole.ClinicAdmin });
        await _db.SaveChangesAsync();

        var result = await _checker.CanAccessPatientRecord(
            clinicAdminUserId, UserRole.ClinicAdmin, _patientProfileId, CancellationToken.None);

        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task Doctor_CanAccessPatientWithCompletedAppointment()
    {
        // Add a completed appointment
        var patientId2 = Guid.NewGuid();
        var patientUserId2 = Guid.NewGuid();
        _db.Users.Add(new User { Id = patientUserId2, FirstName = "P2", LastName = "P2", Email = "p2@test.com", UserName = "p2@test.com", Role = UserRole.Patient });
        _db.PatientProfiles.Add(new PatientProfile { Id = patientId2, UserId = patientUserId2 });
        _db.Appointments.Add(new Appointment
        {
            DoctorId = _doctorProfileId,
            PatientId = patientId2,
            ScheduledAt = DateTime.UtcNow.AddDays(-1),
            Reason = "Past visit",
            Status = AppointmentStatus.Completed,
        });
        await _db.SaveChangesAsync();

        var result = await _checker.CanAccessPatientRecord(
            _doctorUserId, UserRole.Doctor, patientId2, CancellationToken.None);

        result.Should().BeTrue();
    }
}

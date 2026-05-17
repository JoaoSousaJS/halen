using FluentAssertions;
using Halen.Application.Appointments.Queries;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.Infrastructure.Persistence;
using Halen.UnitTests.Helpers;

namespace Halen.UnitTests.Appointments;

[TestClass]
public class GetMyAppointmentsQueryHandlerTests
{
    private HalenDbContext _db = null!;
    private GetMyAppointmentsQueryHandler _handler = null!;
    private Guid _patientUserId;
    private Guid _doctorUserId;
    private Guid _patientProfileId;
    private Guid _doctorProfileId;

    [TestInitialize]
    public async Task Initialize()
    {
        _db = TestDbFactory.Create();

        _patientUserId = Guid.NewGuid();
        _doctorUserId = Guid.NewGuid();
        _patientProfileId = Guid.NewGuid();
        _doctorProfileId = Guid.NewGuid();

        _db.Users.AddRange(
            new User
            {
                Id = _patientUserId, FirstName = "John", LastName = "Doe",
                Email = "john@test.com", UserName = "john@test.com", Role = UserRole.Patient,
            },
            new User
            {
                Id = _doctorUserId, FirstName = "Dr", LastName = "House",
                Email = "house@test.com", UserName = "house@test.com", Role = UserRole.Doctor,
            }
        );

        _db.PatientProfiles.Add(new PatientProfile
        {
            Id = _patientProfileId, UserId = _patientUserId,
        });

        _db.DoctorProfiles.Add(new DoctorProfile
        {
            Id = _doctorProfileId, UserId = _doctorUserId,
            Specialty = "Diagnostics", LicenseNumber = "LIC-001",
            ConsultationFee = 150, YearsOfExperience = 10,
            KycStatus = KycStatus.Approved,
        });

        await _db.SaveChangesAsync();

        _handler = new GetMyAppointmentsQueryHandler(_db);
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_AsPatient_ReturnsOnlyOwnAppointments()
    {
        // Seed another patient with their own appointment
        var otherPatientUserId = Guid.NewGuid();
        var otherPatientProfileId = Guid.NewGuid();
        _db.Users.Add(new User
        {
            Id = otherPatientUserId, FirstName = "Other", LastName = "Patient",
            Email = "other@test.com", UserName = "other@test.com", Role = UserRole.Patient,
        });
        _db.PatientProfiles.Add(new PatientProfile { Id = otherPatientProfileId, UserId = otherPatientUserId });

        _db.Appointments.AddRange(
            new Appointment
            {
                PatientId = _patientProfileId, DoctorId = _doctorProfileId,
                ScheduledAt = DateTime.UtcNow.AddDays(1), Reason = "My appointment",
            },
            new Appointment
            {
                PatientId = otherPatientProfileId, DoctorId = _doctorProfileId,
                ScheduledAt = DateTime.UtcNow.AddDays(2), Reason = "Other patient appointment",
            }
        );
        await _db.SaveChangesAsync();

        var query = new GetMyAppointmentsQuery(_patientUserId, UserRole.Patient);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Appointments.Should().HaveCount(1);
        result.Appointments[0].Reason.Should().Be("My appointment");
        result.TotalCount.Should().Be(1);
    }

    [TestMethod]
    public async Task Handle_AsDoctor_ReturnsOnlyDoctorsAppointments()
    {
        // Create second doctor with their own appointment
        var otherDoctorUserId = Guid.NewGuid();
        var otherDoctorProfileId = Guid.NewGuid();
        _db.Users.Add(new User
        {
            Id = otherDoctorUserId, FirstName = "Dr", LastName = "Wilson",
            Email = "wilson@test.com", UserName = "wilson@test.com", Role = UserRole.Doctor,
        });
        _db.DoctorProfiles.Add(new DoctorProfile
        {
            Id = otherDoctorProfileId, UserId = otherDoctorUserId,
            Specialty = "Oncology", LicenseNumber = "LIC-002",
            ConsultationFee = 200, YearsOfExperience = 8,
            KycStatus = KycStatus.Approved,
        });

        _db.Appointments.AddRange(
            new Appointment
            {
                PatientId = _patientProfileId, DoctorId = _doctorProfileId,
                ScheduledAt = DateTime.UtcNow.AddDays(1), Reason = "My doctor's appointment",
            },
            new Appointment
            {
                PatientId = _patientProfileId, DoctorId = otherDoctorProfileId,
                ScheduledAt = DateTime.UtcNow.AddDays(2), Reason = "Other doctor's appointment",
            }
        );
        await _db.SaveChangesAsync();

        var query = new GetMyAppointmentsQuery(_doctorUserId, UserRole.Doctor);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Appointments.Should().HaveCount(1);
        result.Appointments[0].Reason.Should().Be("My doctor's appointment");
        result.TotalCount.Should().Be(1);
    }

    [TestMethod]
    public async Task Handle_NoProfile_ReturnsEmpty()
    {
        // Create a user with no patient or doctor profile
        var orphanUserId = Guid.NewGuid();
        _db.Users.Add(new User
        {
            Id = orphanUserId, FirstName = "No", LastName = "Profile",
            Email = "noprofile@test.com", UserName = "noprofile@test.com", Role = UserRole.Patient,
        });
        await _db.SaveChangesAsync();

        var query = new GetMyAppointmentsQuery(orphanUserId, UserRole.Patient);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Appointments.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [TestMethod]
    public async Task Handle_Pagination_ReturnsCorrectPageAndTotalCount()
    {
        // Seed 5 appointments for the patient
        for (var i = 0; i < 5; i++)
        {
            _db.Appointments.Add(new Appointment
            {
                PatientId = _patientProfileId, DoctorId = _doctorProfileId,
                ScheduledAt = DateTime.UtcNow.AddDays(i + 1), Reason = $"Appointment {i}",
            });
        }
        await _db.SaveChangesAsync();

        var query = new GetMyAppointmentsQuery(_patientUserId, UserRole.Patient, Page: 2, PageSize: 2);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.TotalCount.Should().Be(5);
        result.Appointments.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task Handle_OrdersByScheduledAtDescending()
    {
        var earliest = DateTime.UtcNow.AddDays(1);
        var middle = DateTime.UtcNow.AddDays(3);
        var latest = DateTime.UtcNow.AddDays(5);

        _db.Appointments.AddRange(
            new Appointment
            {
                PatientId = _patientProfileId, DoctorId = _doctorProfileId,
                ScheduledAt = middle, Reason = "Middle",
            },
            new Appointment
            {
                PatientId = _patientProfileId, DoctorId = _doctorProfileId,
                ScheduledAt = earliest, Reason = "Earliest",
            },
            new Appointment
            {
                PatientId = _patientProfileId, DoctorId = _doctorProfileId,
                ScheduledAt = latest, Reason = "Latest",
            }
        );
        await _db.SaveChangesAsync();

        var query = new GetMyAppointmentsQuery(_patientUserId, UserRole.Patient);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Appointments.Should().HaveCount(3);
        result.Appointments[0].Reason.Should().Be("Latest");
        result.Appointments[1].Reason.Should().Be("Middle");
        result.Appointments[2].Reason.Should().Be("Earliest");
    }
}

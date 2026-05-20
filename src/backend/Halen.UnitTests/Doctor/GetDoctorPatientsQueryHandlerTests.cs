using FluentAssertions;
using Halen.Application.Doctor.Queries;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.Infrastructure.Persistence;
using Halen.UnitTests.Helpers;

namespace Halen.UnitTests.Doctor;

[TestClass]
public class GetDoctorPatientsQueryHandlerTests
{
    private HalenDbContext _db = null!;
    private GetDoctorPatientsQueryHandler _handler = null!;
    private Guid _doctorUserId;
    private Guid _doctorProfileId;
    private Guid _patientUserId;
    private Guid _patientProfileId;

    [TestInitialize]
    public async Task Initialize()
    {
        _db = TestDbFactory.Create();

        _doctorUserId = Guid.NewGuid();
        _doctorProfileId = Guid.NewGuid();
        _patientUserId = Guid.NewGuid();
        _patientProfileId = Guid.NewGuid();

        _db.Users.AddRange(
            new User
            {
                Id = _doctorUserId, FirstName = "Dr", LastName = "House",
                Email = "house@test.com", UserName = "house@test.com", Role = UserRole.Doctor,
            },
            new User
            {
                Id = _patientUserId, FirstName = "John", LastName = "Doe",
                Email = "john@test.com", UserName = "john@test.com", Role = UserRole.Patient,
            }
        );

        _db.DoctorProfiles.Add(new DoctorProfile
        {
            Id = _doctorProfileId, UserId = _doctorUserId,
            Specialty = "Diagnostics", LicenseNumber = "LIC-001",
            ConsultationFee = 150, YearsOfExperience = 10,
            KycStatus = KycStatus.Approved,
        });

        _db.PatientProfiles.Add(new PatientProfile
        {
            Id = _patientProfileId, UserId = _patientUserId,
        });

        await _db.SaveChangesAsync();

        _handler = new GetDoctorPatientsQueryHandler(_db);
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_ReturnsOnlyPatientsWithCompletedAppointments()
    {
        _db.Appointments.AddRange(
            new Appointment
            {
                PatientId = _patientProfileId, DoctorId = _doctorProfileId,
                ScheduledAt = DateTime.UtcNow.AddDays(-1), Reason = "Checkup",
                Status = AppointmentStatus.Completed,
            },
            new Appointment
            {
                PatientId = _patientProfileId, DoctorId = _doctorProfileId,
                ScheduledAt = DateTime.UtcNow.AddDays(1), Reason = "Follow-up",
                Status = AppointmentStatus.Scheduled,
            }
        );
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(
            new GetDoctorPatientsQuery(_doctorUserId), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].PatientId.Should().Be(_patientProfileId);
        result[0].Name.Should().Be("John Doe");
    }

    [TestMethod]
    public async Task Handle_ExcludesCancelledAppointments()
    {
        _db.Appointments.Add(new Appointment
        {
            PatientId = _patientProfileId, DoctorId = _doctorProfileId,
            ScheduledAt = DateTime.UtcNow.AddDays(-1), Reason = "Cancelled visit",
            Status = AppointmentStatus.Cancelled,
        });
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(
            new GetDoctorPatientsQuery(_doctorUserId), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [TestMethod]
    public async Task Handle_ReturnsDistinctPatients()
    {
        _db.Appointments.AddRange(
            new Appointment
            {
                PatientId = _patientProfileId, DoctorId = _doctorProfileId,
                ScheduledAt = DateTime.UtcNow.AddDays(-5), Reason = "First visit",
                Status = AppointmentStatus.Completed,
            },
            new Appointment
            {
                PatientId = _patientProfileId, DoctorId = _doctorProfileId,
                ScheduledAt = DateTime.UtcNow.AddDays(-1), Reason = "Second visit",
                Status = AppointmentStatus.Completed,
            }
        );
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(
            new GetDoctorPatientsQuery(_doctorUserId), CancellationToken.None);

        result.Should().HaveCount(1);
    }

    [TestMethod]
    public async Task Handle_ExcludesPatientsFromOtherDoctors()
    {
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

        _db.Appointments.Add(new Appointment
        {
            PatientId = _patientProfileId, DoctorId = otherDoctorProfileId,
            ScheduledAt = DateTime.UtcNow.AddDays(-1), Reason = "Visit other doctor",
            Status = AppointmentStatus.Completed,
        });
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(
            new GetDoctorPatientsQuery(_doctorUserId), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [TestMethod]
    public async Task Handle_OrdersByPatientName()
    {
        var patientBUserId = Guid.NewGuid();
        var patientBProfileId = Guid.NewGuid();
        _db.Users.Add(new User
        {
            Id = patientBUserId, FirstName = "Alice", LastName = "Smith",
            Email = "alice@test.com", UserName = "alice@test.com", Role = UserRole.Patient,
        });
        _db.PatientProfiles.Add(new PatientProfile
        {
            Id = patientBProfileId, UserId = patientBUserId,
        });

        _db.Appointments.AddRange(
            new Appointment
            {
                PatientId = _patientProfileId, DoctorId = _doctorProfileId,
                ScheduledAt = DateTime.UtcNow.AddDays(-1), Reason = "Visit",
                Status = AppointmentStatus.Completed,
            },
            new Appointment
            {
                PatientId = patientBProfileId, DoctorId = _doctorProfileId,
                ScheduledAt = DateTime.UtcNow.AddDays(-2), Reason = "Visit",
                Status = AppointmentStatus.Completed,
            }
        );
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(
            new GetDoctorPatientsQuery(_doctorUserId), CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Alice Smith");
        result[1].Name.Should().Be("John Doe");
    }

    [TestMethod]
    public async Task Handle_NoAppointments_ReturnsEmpty()
    {
        var result = await _handler.Handle(
            new GetDoctorPatientsQuery(_doctorUserId), CancellationToken.None);

        result.Should().BeEmpty();
    }
}

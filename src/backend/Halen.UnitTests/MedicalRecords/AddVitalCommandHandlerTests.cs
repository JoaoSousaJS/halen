using FluentAssertions;
using Halen.Application.Common;
using Halen.Application.Interfaces;
using Halen.Application.MedicalRecords;
using Halen.Application.MedicalRecords.Commands;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.Infrastructure.Persistence;
using Halen.UnitTests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Halen.UnitTests.MedicalRecords;

[TestClass]
public class AddVitalCommandHandlerTests
{
    private HalenDbContext _db = null!;
    private Mock<IEventBus> _eventBus = null!;
    private AddVitalCommandHandler _handler = null!;
    private Guid _doctorUserId;
    private Guid _patientUserId;
    private Guid _patientProfileId;

    [TestInitialize]
    public async Task Initialize()
    {
        _db = TestDbFactory.Create();

        _doctorUserId = Guid.NewGuid();
        _patientUserId = Guid.NewGuid();
        _patientProfileId = Guid.NewGuid();
        var doctorProfileId = Guid.NewGuid();

        _db.Users.AddRange(
            new User { Id = _doctorUserId, FirstName = "Dr", LastName = "House", Email = "doc@test.com", UserName = "doc@test.com", Role = UserRole.Doctor },
            new User { Id = _patientUserId, FirstName = "Pat", LastName = "Ient", Email = "pat@test.com", UserName = "pat@test.com", Role = UserRole.Patient }
        );

        _db.DoctorProfiles.Add(new DoctorProfile
        {
            Id = doctorProfileId, UserId = _doctorUserId,
            Specialty = "General", LicenseNumber = "LIC-001",
            ConsultationFee = 100, YearsOfExperience = 5,
            KycStatus = KycStatus.Approved,
        });

        _db.PatientProfiles.Add(new PatientProfile { Id = _patientProfileId, UserId = _patientUserId });

        _db.Appointments.Add(new Appointment
        {
            DoctorId = doctorProfileId,
            PatientId = _patientProfileId,
            ScheduledAt = DateTime.UtcNow.AddDays(1),
            Reason = "Checkup",
            Status = AppointmentStatus.Scheduled,
        });

        await _db.SaveChangesAsync();

        _eventBus = new Mock<IEventBus>();
        var accessChecker = new RecordAccessChecker(_db);
        _handler = new AddVitalCommandHandler(
            _db, new TestTenantContext(), _eventBus.Object, accessChecker,
            Mock.Of<ILogger<AddVitalCommandHandler>>());
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_ValidCommand_CreatesVital()
    {
        var measuredAt = DateTime.UtcNow.AddMinutes(-10);
        var command = new AddVitalCommand(
            _doctorUserId, UserRole.Doctor, _patientProfileId,
            VitalType.BloodPressure, 120m, 80m, "mmHg",
            measuredAt, VitalSource.ClinicalEntry, "Normal reading");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.VitalId.Should().NotBeNull();

        var vital = await _db.PatientVitals.FindAsync(result.VitalId);
        vital.Should().NotBeNull();
        vital!.VitalType.Should().Be(VitalType.BloodPressure);
        vital.Value.Should().Be(120m);
        vital.SecondaryValue.Should().Be(80m);
        vital.Unit.Should().Be("mmHg");
        vital.Source.Should().Be(VitalSource.ClinicalEntry);
        vital.AddedByUserId.Should().Be(_doctorUserId);
    }

    [TestMethod]
    public async Task Handle_AccessDenied_ReturnsForbidden()
    {
        var otherUserId = Guid.NewGuid();
        _db.Users.Add(new User { Id = otherUserId, FirstName = "Other", LastName = "User", Email = "other@test.com", UserName = "other@test.com", Role = UserRole.Patient });
        await _db.SaveChangesAsync();

        var command = new AddVitalCommand(
            otherUserId, UserRole.Patient, _patientProfileId,
            VitalType.HeartRate, 72m, null, "bpm",
            DateTime.UtcNow.AddMinutes(-10), VitalSource.Manual, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Forbidden);
    }

    [TestMethod]
    public async Task Handle_FutureMeasuredAt_ReturnsValidationError()
    {
        var futureTime = DateTime.UtcNow.AddMinutes(10); // More than 5 minutes in the future
        var command = new AddVitalCommand(
            _doctorUserId, UserRole.Doctor, _patientProfileId,
            VitalType.HeartRate, 72m, null, "bpm",
            futureTime, VitalSource.Manual, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Validation);
        result.Error.Should().Contain("future");
    }
}

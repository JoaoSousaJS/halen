using FluentAssertions;
using Halen.Application.Common;
using Halen.Application.Events;
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
public class AddConditionCommandHandlerTests
{
    private HalenDbContext _db = null!;
    private Mock<IEventBus> _eventBus = null!;
    private AddConditionCommandHandler _handler = null!;
    private Guid _doctorUserId;
    private Guid _patientUserId;
    private Guid _patientProfileId;
    private Guid _appointmentId;

    [TestInitialize]
    public async Task Initialize()
    {
        _db = TestDbFactory.Create();

        _doctorUserId = Guid.NewGuid();
        _patientUserId = Guid.NewGuid();
        _patientProfileId = Guid.NewGuid();
        _appointmentId = Guid.NewGuid();
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
            Id = _appointmentId,
            DoctorId = doctorProfileId,
            PatientId = _patientProfileId,
            ScheduledAt = DateTime.UtcNow.AddDays(1),
            Reason = "Checkup",
            Status = AppointmentStatus.Scheduled,
        });

        await _db.SaveChangesAsync();

        _eventBus = new Mock<IEventBus>();
        var accessChecker = new RecordAccessChecker(_db);
        _handler = new AddConditionCommandHandler(
            _db, new TestTenantContext(), _eventBus.Object, accessChecker,
            Mock.Of<ILogger<AddConditionCommandHandler>>());
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_ValidCommand_CreatesConditionAndPublishesEvent()
    {
        var command = new AddConditionCommand(
            _doctorUserId, UserRole.Doctor, _patientProfileId,
            "J06.9", "Acute upper respiratory infection", null,
            ConditionSeverity.Mild, ConditionStatus.Active,
            "Patient presents with cold symptoms", _appointmentId);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ConditionId.Should().NotBeNull();

        var condition = await _db.PatientConditions.FindAsync(result.ConditionId);
        condition.Should().NotBeNull();
        condition!.IcdCode.Should().Be("J06.9");
        condition.IcdDescription.Should().Be("Acute upper respiratory infection");
        condition.Severity.Should().Be(ConditionSeverity.Mild);
        condition.Status.Should().Be(ConditionStatus.Active);
        condition.AddedByUserId.Should().Be(_doctorUserId);
        condition.LinkedAppointmentId.Should().Be(_appointmentId);

        _eventBus.Verify(e => e.PublishAsync(
            Topics.MedicalRecordUpdated,
            It.Is<MedicalRecordUpdatedEvent>(evt =>
                evt.PatientProfileId == _patientProfileId &&
                evt.RecordType == "Condition" &&
                evt.Action == "Added"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_PatientNotFound_ReturnsNotFound()
    {
        var command = new AddConditionCommand(
            _doctorUserId, UserRole.Doctor, Guid.NewGuid(),
            "J06.9", "Acute upper respiratory infection", null,
            ConditionSeverity.Mild, ConditionStatus.Active, null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.NotFound);
        result.Error.Should().Contain("Patient");
    }

    [TestMethod]
    public async Task Handle_AccessDenied_ReturnsForbidden()
    {
        var otherUserId = Guid.NewGuid();
        _db.Users.Add(new User { Id = otherUserId, FirstName = "Other", LastName = "User", Email = "other@test.com", UserName = "other@test.com", Role = UserRole.Patient });
        await _db.SaveChangesAsync();

        var command = new AddConditionCommand(
            otherUserId, UserRole.Patient, _patientProfileId,
            "J06.9", "Acute upper respiratory infection", null,
            ConditionSeverity.Mild, ConditionStatus.Active, null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Forbidden);
    }

    [TestMethod]
    public async Task Handle_InvalidLinkedAppointment_ReturnsNotFound()
    {
        var command = new AddConditionCommand(
            _doctorUserId, UserRole.Doctor, _patientProfileId,
            "J06.9", "Acute upper respiratory infection", null,
            ConditionSeverity.Mild, ConditionStatus.Active, null,
            Guid.NewGuid()); // Non-existent appointment

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.NotFound);
        result.Error.Should().Contain("Appointment");
    }
}

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
public class UpdateConditionCommandHandlerTests
{
    private HalenDbContext _db = null!;
    private Mock<IEventBus> _eventBus = null!;
    private UpdateConditionCommandHandler _handler = null!;
    private Guid _doctorUserId;
    private Guid _patientUserId;
    private Guid _patientProfileId;
    private Guid _conditionId;

    [TestInitialize]
    public async Task Initialize()
    {
        _db = TestDbFactory.Create();

        _doctorUserId = Guid.NewGuid();
        _patientUserId = Guid.NewGuid();
        _patientProfileId = Guid.NewGuid();
        _conditionId = Guid.NewGuid();
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

        _db.PatientConditions.Add(new PatientCondition
        {
            Id = _conditionId,
            PatientProfileId = _patientProfileId,
            IcdCode = "J06.9",
            IcdDescription = "Acute upper respiratory infection",
            Severity = ConditionSeverity.Mild,
            Status = ConditionStatus.Active,
            AddedByUserId = _doctorUserId,
        });

        await _db.SaveChangesAsync();

        _eventBus = new Mock<IEventBus>();
        var accessChecker = new RecordAccessChecker(_db);
        _handler = new UpdateConditionCommandHandler(
            _db, new TestTenantContext(), _eventBus.Object, accessChecker,
            Mock.Of<ILogger<UpdateConditionCommandHandler>>());
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_ValidUpdate_UpdatesCondition()
    {
        var command = new UpdateConditionCommand(
            _doctorUserId, UserRole.Doctor, _conditionId,
            ConditionSeverity.Severe, ConditionStatus.InRemission,
            "Updated clinical notes");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();

        var condition = await _db.PatientConditions.FindAsync(_conditionId);
        condition!.Severity.Should().Be(ConditionSeverity.Severe);
        condition.Status.Should().Be(ConditionStatus.InRemission);
        condition.ClinicalNotes.Should().Be("Updated clinical notes");
    }

    [TestMethod]
    public async Task Handle_ConditionNotFound_ReturnsNotFound()
    {
        var command = new UpdateConditionCommand(
            _doctorUserId, UserRole.Doctor, Guid.NewGuid(),
            ConditionSeverity.Severe, ConditionStatus.Resolved, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.NotFound);
        result.Error.Should().Contain("Condition");
    }

    [TestMethod]
    public async Task Handle_AccessDenied_ReturnsForbidden()
    {
        var otherUserId = Guid.NewGuid();
        _db.Users.Add(new User { Id = otherUserId, FirstName = "Other", LastName = "Doc", Email = "other@test.com", UserName = "other@test.com", Role = UserRole.Doctor });
        _db.DoctorProfiles.Add(new DoctorProfile
        {
            UserId = otherUserId,
            Specialty = "Cardiology", LicenseNumber = "LIC-999",
            ConsultationFee = 200, YearsOfExperience = 10,
            KycStatus = KycStatus.Approved,
        });
        await _db.SaveChangesAsync();

        // Other doctor has no appointment with this patient and didn't add the condition
        var command = new UpdateConditionCommand(
            otherUserId, UserRole.Doctor, _conditionId,
            ConditionSeverity.Severe, ConditionStatus.Resolved, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Forbidden);
    }
}

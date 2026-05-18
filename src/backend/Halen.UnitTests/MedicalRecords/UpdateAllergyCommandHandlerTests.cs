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
public class UpdateAllergyCommandHandlerTests
{
    private HalenDbContext _db = null!;
    private Mock<IEventBus> _eventBus = null!;
    private UpdateAllergyCommandHandler _handler = null!;
    private Guid _doctorUserId;
    private Guid _patientUserId;
    private Guid _patientProfileId;
    private Guid _allergyId;

    [TestInitialize]
    public async Task Initialize()
    {
        _db = TestDbFactory.Create();

        _doctorUserId = Guid.NewGuid();
        _patientUserId = Guid.NewGuid();
        _patientProfileId = Guid.NewGuid();
        _allergyId = Guid.NewGuid();
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

        _db.PatientAllergies.Add(new PatientAllergy
        {
            Id = _allergyId,
            PatientProfileId = _patientProfileId,
            AllergenName = "Penicillin",
            Reaction = "Rash",
            Severity = ConditionSeverity.Mild,
            IsActive = true,
            AddedByUserId = _doctorUserId,
        });

        await _db.SaveChangesAsync();

        _eventBus = new Mock<IEventBus>();
        var accessChecker = new RecordAccessChecker(_db);
        _handler = new UpdateAllergyCommandHandler(
            _db, new TestTenantContext(), _eventBus.Object, accessChecker,
            Mock.Of<ILogger<UpdateAllergyCommandHandler>>());
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_ValidUpdate_UpdatesAllergy()
    {
        var command = new UpdateAllergyCommand(
            _doctorUserId, UserRole.Doctor, _allergyId,
            "Severe anaphylaxis", ConditionSeverity.Severe, false);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();

        var allergy = await _db.PatientAllergies.FindAsync(_allergyId);
        allergy!.Reaction.Should().Be("Severe anaphylaxis");
        allergy.Severity.Should().Be(ConditionSeverity.Severe);
        allergy.IsActive.Should().BeFalse();
    }

    [TestMethod]
    public async Task Handle_AllergyNotFound_ReturnsNotFound()
    {
        var command = new UpdateAllergyCommand(
            _doctorUserId, UserRole.Doctor, Guid.NewGuid(),
            "Rash", ConditionSeverity.Mild, true);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.NotFound);
        result.Error.Should().Contain("Allergy");
    }

    [TestMethod]
    public async Task Handle_AccessDenied_ReturnsForbidden()
    {
        var otherUserId = Guid.NewGuid();
        _db.Users.Add(new User { Id = otherUserId, FirstName = "Other", LastName = "User", Email = "other@test.com", UserName = "other@test.com", Role = UserRole.Patient });
        await _db.SaveChangesAsync();

        var command = new UpdateAllergyCommand(
            otherUserId, UserRole.Patient, _allergyId,
            "Rash", ConditionSeverity.Mild, true);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Forbidden);
    }
}

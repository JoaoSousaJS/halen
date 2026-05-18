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
public class AddAllergyCommandHandlerTests
{
    private HalenDbContext _db = null!;
    private Mock<IEventBus> _eventBus = null!;
    private AddAllergyCommandHandler _handler = null!;
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
        _handler = new AddAllergyCommandHandler(
            _db, new TestTenantContext(), _eventBus.Object, accessChecker,
            Mock.Of<ILogger<AddAllergyCommandHandler>>());
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_ValidCommand_CreatesAllergy()
    {
        var command = new AddAllergyCommand(
            _doctorUserId, UserRole.Doctor, _patientProfileId,
            "Penicillin", "Rash and hives", ConditionSeverity.Severe,
            new DateOnly(2023, 1, 15));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.AllergyId.Should().NotBeNull();

        var allergy = await _db.PatientAllergies.FindAsync(result.AllergyId);
        allergy.Should().NotBeNull();
        allergy!.AllergenName.Should().Be("Penicillin");
        allergy.Reaction.Should().Be("Rash and hives");
        allergy.Severity.Should().Be(ConditionSeverity.Severe);
        allergy.IsActive.Should().BeTrue();
        allergy.AddedByUserId.Should().Be(_doctorUserId);
    }

    [TestMethod]
    public async Task Handle_AccessDenied_ReturnsForbidden()
    {
        var otherUserId = Guid.NewGuid();
        _db.Users.Add(new User { Id = otherUserId, FirstName = "Other", LastName = "User", Email = "other@test.com", UserName = "other@test.com", Role = UserRole.Patient });
        await _db.SaveChangesAsync();

        var command = new AddAllergyCommand(
            otherUserId, UserRole.Patient, _patientProfileId,
            "Penicillin", "Rash", ConditionSeverity.Severe, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Forbidden);
    }

    [TestMethod]
    public async Task Handle_DuplicateAllergen_ReturnsValidationError()
    {
        // Seed an existing allergy
        _db.PatientAllergies.Add(new PatientAllergy
        {
            PatientProfileId = _patientProfileId,
            AllergenName = "Penicillin",
            Severity = ConditionSeverity.Mild,
            AddedByUserId = _doctorUserId,
        });
        await _db.SaveChangesAsync();

        // Try to add the same allergen (case-insensitive)
        var command = new AddAllergyCommand(
            _doctorUserId, UserRole.Doctor, _patientProfileId,
            "penicillin", "Different reaction", ConditionSeverity.Severe, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Validation);
        result.Error.Should().Contain("already exists");
    }
}

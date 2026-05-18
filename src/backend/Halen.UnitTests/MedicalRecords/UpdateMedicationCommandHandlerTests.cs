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
public class UpdateMedicationCommandHandlerTests
{
    private HalenDbContext _db = null!;
    private Mock<IEventBus> _eventBus = null!;
    private UpdateMedicationCommandHandler _handler = null!;
    private Guid _doctorUserId;
    private Guid _patientUserId;
    private Guid _patientProfileId;
    private Guid _medicationId;

    [TestInitialize]
    public async Task Initialize()
    {
        _db = TestDbFactory.Create();

        _doctorUserId = Guid.NewGuid();
        _patientUserId = Guid.NewGuid();
        _patientProfileId = Guid.NewGuid();
        _medicationId = Guid.NewGuid();
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

        _db.PatientMedications.Add(new PatientMedication
        {
            Id = _medicationId,
            PatientProfileId = _patientProfileId,
            MedicationName = "Amoxicillin",
            Dosage = "500mg",
            Frequency = "Twice daily",
            IsActive = true,
            AddedByUserId = _doctorUserId,
        });

        await _db.SaveChangesAsync();

        _eventBus = new Mock<IEventBus>();
        var accessChecker = new RecordAccessChecker(_db);
        _handler = new UpdateMedicationCommandHandler(
            _db, new TestTenantContext(), _eventBus.Object, accessChecker,
            Mock.Of<ILogger<UpdateMedicationCommandHandler>>());
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_ValidUpdate_UpdatesMedication()
    {
        var command = new UpdateMedicationCommand(
            _doctorUserId, UserRole.Doctor, _medicationId,
            "250mg", "Once daily", new DateOnly(2025, 6, 1), false);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();

        var med = await _db.PatientMedications.FindAsync(_medicationId);
        med!.Dosage.Should().Be("250mg");
        med.Frequency.Should().Be("Once daily");
        med.EndDate.Should().Be(new DateOnly(2025, 6, 1));
        med.IsActive.Should().BeFalse();
    }

    [TestMethod]
    public async Task Handle_MedicationNotFound_ReturnsNotFound()
    {
        var command = new UpdateMedicationCommand(
            _doctorUserId, UserRole.Doctor, Guid.NewGuid(),
            "250mg", "Once daily", null, true);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.NotFound);
        result.Error.Should().Contain("Medication");
    }

    [TestMethod]
    public async Task Handle_AccessDenied_ReturnsForbidden()
    {
        var otherUserId = Guid.NewGuid();
        _db.Users.Add(new User { Id = otherUserId, FirstName = "Other", LastName = "User", Email = "other@test.com", UserName = "other@test.com", Role = UserRole.Patient });
        await _db.SaveChangesAsync();

        var command = new UpdateMedicationCommand(
            otherUserId, UserRole.Patient, _medicationId,
            "250mg", "Once daily", null, true);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Forbidden);
    }
}

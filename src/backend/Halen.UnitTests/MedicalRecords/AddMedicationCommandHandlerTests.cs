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
public class AddMedicationCommandHandlerTests
{
    private HalenDbContext _db = null!;
    private Mock<IEventBus> _eventBus = null!;
    private AddMedicationCommandHandler _handler = null!;
    private Guid _doctorUserId;
    private Guid _patientUserId;
    private Guid _patientProfileId;
    private Guid _prescriptionId;

    [TestInitialize]
    public async Task Initialize()
    {
        _db = TestDbFactory.Create();

        _doctorUserId = Guid.NewGuid();
        _patientUserId = Guid.NewGuid();
        _patientProfileId = Guid.NewGuid();
        _prescriptionId = Guid.NewGuid();
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

        _db.Prescriptions.Add(new Prescription
        {
            Id = _prescriptionId,
            DoctorId = doctorProfileId,
            PatientId = _patientProfileId,
            DrugName = "Amoxicillin",
            Dosage = "500mg",
            Frequency = "Twice daily",
            RefillsRemaining = 3,
            Status = PrescriptionStatus.Active,
        });

        await _db.SaveChangesAsync();

        _eventBus = new Mock<IEventBus>();
        var accessChecker = new RecordAccessChecker(_db);
        _handler = new AddMedicationCommandHandler(
            _db, new TestTenantContext(), _eventBus.Object, accessChecker,
            Mock.Of<ILogger<AddMedicationCommandHandler>>());
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_ValidCommand_CreatesMedication()
    {
        var command = new AddMedicationCommand(
            _doctorUserId, UserRole.Doctor, _patientProfileId,
            "Amoxicillin", "500mg", "Twice daily",
            new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 14),
            "Dr. House", _prescriptionId);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.MedicationId.Should().NotBeNull();

        var med = await _db.PatientMedications.FindAsync(result.MedicationId);
        med.Should().NotBeNull();
        med!.MedicationName.Should().Be("Amoxicillin");
        med.Dosage.Should().Be("500mg");
        med.Frequency.Should().Be("Twice daily");
        med.IsActive.Should().BeTrue();
        med.LinkedPrescriptionId.Should().Be(_prescriptionId);
        med.AddedByUserId.Should().Be(_doctorUserId);
    }

    [TestMethod]
    public async Task Handle_AccessDenied_ReturnsForbidden()
    {
        var otherUserId = Guid.NewGuid();
        _db.Users.Add(new User { Id = otherUserId, FirstName = "Other", LastName = "User", Email = "other@test.com", UserName = "other@test.com", Role = UserRole.Patient });
        await _db.SaveChangesAsync();

        var command = new AddMedicationCommand(
            otherUserId, UserRole.Patient, _patientProfileId,
            "Ibuprofen", "200mg", "As needed", null, null, null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Forbidden);
    }

    [TestMethod]
    public async Task Handle_EndDateBeforeStartDate_ReturnsValidationError()
    {
        var command = new AddMedicationCommand(
            _doctorUserId, UserRole.Doctor, _patientProfileId,
            "Amoxicillin", "500mg", "Twice daily",
            new DateOnly(2025, 2, 1), new DateOnly(2025, 1, 1), // EndDate before StartDate
            null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Validation);
        result.Error.Should().Contain("End date");
    }

    [TestMethod]
    public async Task Handle_LinkedPrescriptionNotFound_ReturnsNotFound()
    {
        var command = new AddMedicationCommand(
            _doctorUserId, UserRole.Doctor, _patientProfileId,
            "Amoxicillin", "500mg", "Twice daily",
            null, null, null, Guid.NewGuid()); // Non-existent prescription

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.NotFound);
        result.Error.Should().Contain("Prescription");
    }
}

using FluentAssertions;
using Halen.Application.Common;
using Halen.Application.Prescriptions.Commands;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;

namespace Halen.UnitTests.Prescriptions;

[TestClass]
public class CancelPrescriptionCommandHandlerTests
{
    private HalenDbContext _db = null!;
    private CancelPrescriptionCommandHandler _handler = null!;
    private Guid _doctorUserId;
    private Guid _prescriptionId;

    [TestInitialize]
    public async Task Initialize()
    {
        var options = new DbContextOptionsBuilder<HalenDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new HalenDbContext(options);

        _doctorUserId = Guid.NewGuid();
        _prescriptionId = Guid.NewGuid();
        var doctorProfileId = Guid.NewGuid();

        _db.Users.Add(new User
        {
            Id = _doctorUserId, FirstName = "Dr", LastName = "House",
            Email = "doc@test.com", UserName = "doc@test.com", Role = UserRole.Doctor
        });

        _db.DoctorProfiles.Add(new DoctorProfile
        {
            Id = doctorProfileId, UserId = _doctorUserId,
            Specialty = "General", LicenseNumber = "LIC-001",
            ConsultationFee = 100, YearsOfExperience = 5
        });

        var patientProfileId = Guid.NewGuid();
        _db.PatientProfiles.Add(new PatientProfile { Id = patientProfileId, UserId = Guid.NewGuid() });

        _db.Prescriptions.Add(new Prescription
        {
            Id = _prescriptionId,
            DoctorId = doctorProfileId,
            PatientId = patientProfileId,
            DrugName = "Amoxicillin",
            Dosage = "500mg",
            Frequency = "Twice daily",
            Status = PrescriptionStatus.Active,
        });

        await _db.SaveChangesAsync();

        _handler = new CancelPrescriptionCommandHandler(
            _db, Mock.Of<ILogger<CancelPrescriptionCommandHandler>>());
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_OwnActivePrescription_CancelsSuccessfully()
    {
        var command = new CancelPrescriptionCommand(_doctorUserId, _prescriptionId);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        var rx = await _db.Prescriptions.FindAsync(_prescriptionId);
        rx!.Status.Should().Be(PrescriptionStatus.Cancelled);
    }

    [TestMethod]
    public async Task Handle_PrescriptionNotFound_ReturnsNotFoundError()
    {
        var command = new CancelPrescriptionCommand(_doctorUserId, Guid.NewGuid());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.NotFound);
    }

    [TestMethod]
    public async Task Handle_OtherDoctorsPrescription_ReturnsForbiddenError()
    {
        var command = new CancelPrescriptionCommand(Guid.NewGuid(), _prescriptionId);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Forbidden);
    }

    [TestMethod]
    public async Task Handle_AlreadyCancelledPrescription_ReturnsValidationError()
    {
        var rx = await _db.Prescriptions.FindAsync(_prescriptionId);
        rx!.Status = PrescriptionStatus.Cancelled;
        await _db.SaveChangesAsync();

        var command = new CancelPrescriptionCommand(_doctorUserId, _prescriptionId);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Cancelled");
    }
}

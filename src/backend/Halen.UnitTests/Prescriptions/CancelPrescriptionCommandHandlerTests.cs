using FluentAssertions;
using Halen.Application.Common;
using Halen.Application.Events;
using Halen.Application.Interfaces;
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
    private Mock<IEventBus> _eventBus = null!;
    private CancelPrescriptionCommandHandler _handler = null!;
    private Guid _doctorUserId;
    private Guid _patientUserId;
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
        _patientUserId = Guid.NewGuid();
        _prescriptionId = Guid.NewGuid();
        var doctorProfileId = Guid.NewGuid();
        var patientProfileId = Guid.NewGuid();

        _db.Users.AddRange(
            new User
            {
                Id = _doctorUserId, FirstName = "Dr", LastName = "House",
                Email = "doc@test.com", UserName = "doc@test.com", Role = UserRole.Doctor
            },
            new User
            {
                Id = _patientUserId, FirstName = "Pat", LastName = "Ient",
                Email = "pat@test.com", UserName = "pat@test.com", Role = UserRole.Patient
            }
        );

        _db.DoctorProfiles.Add(new DoctorProfile
        {
            Id = doctorProfileId, UserId = _doctorUserId,
            Specialty = "General", LicenseNumber = "LIC-001",
            ConsultationFee = 100, YearsOfExperience = 5,
            KycStatus = KycStatus.Approved,
        });

        _db.PatientProfiles.Add(new PatientProfile { Id = patientProfileId, UserId = _patientUserId });

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

        _eventBus = new Mock<IEventBus>();
        _handler = new CancelPrescriptionCommandHandler(
            _db, _eventBus.Object, Mock.Of<ILogger<CancelPrescriptionCommandHandler>>());
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_OwnActivePrescription_CancelsAndPublishesEvent()
    {
        var command = new CancelPrescriptionCommand(_doctorUserId, _prescriptionId);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        var rx = await _db.Prescriptions.FindAsync(_prescriptionId);
        rx!.Status.Should().Be(PrescriptionStatus.Cancelled);

        _eventBus.Verify(e => e.PublishAsync(
            Topics.PrescriptionCancelled,
            It.Is<PrescriptionCancelledEvent>(evt =>
                evt.PrescriptionId == _prescriptionId &&
                evt.PatientUserId == _patientUserId &&
                evt.DrugName == "Amoxicillin"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_PrescriptionNotFound_ReturnsNotFoundErrorAndNoEvent()
    {
        var command = new CancelPrescriptionCommand(_doctorUserId, Guid.NewGuid());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.NotFound);

        _eventBus.Verify(e => e.PublishAsync(
            It.IsAny<string>(),
            It.IsAny<PrescriptionCancelledEvent>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_OtherDoctorsPrescription_ReturnsForbiddenErrorAndNoEvent()
    {
        var command = new CancelPrescriptionCommand(Guid.NewGuid(), _prescriptionId);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Forbidden);

        _eventBus.Verify(e => e.PublishAsync(
            It.IsAny<string>(),
            It.IsAny<PrescriptionCancelledEvent>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_AlreadyCancelledPrescription_ReturnsValidationErrorAndNoEvent()
    {
        var rx = await _db.Prescriptions.FindAsync(_prescriptionId);
        rx!.Status = PrescriptionStatus.Cancelled;
        await _db.SaveChangesAsync();

        var command = new CancelPrescriptionCommand(_doctorUserId, _prescriptionId);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Cancelled");

        _eventBus.Verify(e => e.PublishAsync(
            It.IsAny<string>(),
            It.IsAny<PrescriptionCancelledEvent>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_EventBusFailure_StillReturnsSuccess()
    {
        _eventBus.Setup(e => e.PublishAsync(
            It.IsAny<string>(),
            It.IsAny<PrescriptionCancelledEvent>(),
            It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Kafka down"));

        var command = new CancelPrescriptionCommand(_doctorUserId, _prescriptionId);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
    }
}

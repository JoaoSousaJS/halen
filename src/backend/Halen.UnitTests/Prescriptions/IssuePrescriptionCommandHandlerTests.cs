using Halen.UnitTests.Helpers;
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
public class IssuePrescriptionCommandHandlerTests
{
    private HalenDbContext _db = null!;
    private Mock<IEventBus> _eventBus = null!;
    private IssuePrescriptionCommandHandler _handler = null!;
    private Guid _doctorUserId;
    private Guid _doctorProfileId;
    private Guid _patientUserId;
    private Guid _patientProfileId;

    [TestInitialize]
    public async Task Initialize()
    {
        var options = new DbContextOptionsBuilder<HalenDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new HalenDbContext(options, new TestTenantContext());

        _doctorUserId = Guid.NewGuid();
        _doctorProfileId = Guid.NewGuid();
        _patientUserId = Guid.NewGuid();
        _patientProfileId = Guid.NewGuid();

        _db.Users.AddRange(
            new User { Id = _doctorUserId, FirstName = "Dr", LastName = "House", Email = "doc@test.com", UserName = "doc@test.com", Role = UserRole.Doctor },
            new User { Id = _patientUserId, FirstName = "Pat", LastName = "Ient", Email = "pat@test.com", UserName = "pat@test.com", Role = UserRole.Patient }
        );

        _db.DoctorProfiles.Add(new DoctorProfile
        {
            Id = _doctorProfileId, UserId = _doctorUserId,
            Specialty = "General", LicenseNumber = "LIC-001",
            ConsultationFee = 100, YearsOfExperience = 5,
            KycStatus = KycStatus.Approved,
        });

        _db.PatientProfiles.Add(new PatientProfile { Id = _patientProfileId, UserId = _patientUserId });
        await _db.SaveChangesAsync();

        _eventBus = new Mock<IEventBus>();
        _handler = new IssuePrescriptionCommandHandler(
            _db, new Helpers.TestTenantContext(), _eventBus.Object, Mock.Of<ILogger<IssuePrescriptionCommandHandler>>());
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_ValidCommand_CreatesActivePrescriptionAndPublishesEvent()
    {
        var command = new IssuePrescriptionCommand(
            _doctorUserId, _patientProfileId,
            "Amoxicillin", "500mg", "Twice daily", 3, "CVS");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.PrescriptionId.Should().NotBeNull();

        var rx = await _db.Prescriptions.FindAsync(result.PrescriptionId);
        rx.Should().NotBeNull();
        rx!.DrugName.Should().Be("Amoxicillin");
        rx.Status.Should().Be(PrescriptionStatus.Active);
        rx.DoctorId.Should().Be(_doctorProfileId);
        rx.PatientId.Should().Be(_patientProfileId);

        _eventBus.Verify(e => e.PublishAsync(
            Topics.PrescriptionIssued,
            It.Is<PrescriptionIssuedEvent>(evt =>
                evt.PrescriptionId == result.PrescriptionId &&
                evt.PatientUserId == _patientUserId &&
                evt.DrugName == "Amoxicillin"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_DoctorProfileNotFound_ReturnsNotFoundErrorAndNoEvent()
    {
        var command = new IssuePrescriptionCommand(
            Guid.NewGuid(), _patientProfileId,
            "Amoxicillin", "500mg", "Twice daily", 3, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.NotFound);
        result.Error.Should().Contain("Doctor");

        _eventBus.Verify(e => e.PublishAsync(
            It.IsAny<string>(),
            It.IsAny<PrescriptionIssuedEvent>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_PatientNotFound_ReturnsNotFoundErrorAndNoEvent()
    {
        var command = new IssuePrescriptionCommand(
            _doctorUserId, Guid.NewGuid(),
            "Amoxicillin", "500mg", "Twice daily", 3, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.NotFound);
        result.Error.Should().Contain("Patient");

        _eventBus.Verify(e => e.PublishAsync(
            It.IsAny<string>(),
            It.IsAny<PrescriptionIssuedEvent>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_EventBusFailure_StillReturnsSuccess()
    {
        _eventBus.Setup(e => e.PublishAsync(
            It.IsAny<string>(),
            It.IsAny<PrescriptionIssuedEvent>(),
            It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Kafka down"));

        var command = new IssuePrescriptionCommand(
            _doctorUserId, _patientProfileId,
            "Amoxicillin", "500mg", "Twice daily", 3, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.PrescriptionId.Should().NotBeNull();
    }

    [TestMethod]
    public async Task Handle_UnapprovedDoctor_ReturnsForbidden()
    {
        var doctor = await _db.DoctorProfiles.FirstAsync(d => d.UserId == _doctorUserId);
        doctor.KycStatus = KycStatus.NotSubmitted;
        await _db.SaveChangesAsync();

        var command = new IssuePrescriptionCommand(
            _doctorUserId, _patientProfileId,
            "Amoxicillin", "500mg", "Twice daily", 3, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Forbidden);
        result.Error.Should().Contain("not yet approved");
    }
}

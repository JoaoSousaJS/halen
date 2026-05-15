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
public class IssuePrescriptionCommandHandlerTests
{
    private HalenDbContext _db = null!;
    private IssuePrescriptionCommandHandler _handler = null!;
    private Guid _doctorUserId;
    private Guid _doctorProfileId;
    private Guid _patientProfileId;

    [TestInitialize]
    public async Task Initialize()
    {
        var options = new DbContextOptionsBuilder<HalenDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new HalenDbContext(options);

        _doctorUserId = Guid.NewGuid();
        _doctorProfileId = Guid.NewGuid();
        _patientProfileId = Guid.NewGuid();

        _db.Users.AddRange(
            new User { Id = _doctorUserId, FirstName = "Dr", LastName = "House", Email = "doc@test.com", UserName = "doc@test.com", Role = UserRole.Doctor },
            new User { Id = Guid.NewGuid(), FirstName = "Pat", LastName = "Ient", Email = "pat@test.com", UserName = "pat@test.com", Role = UserRole.Patient }
        );

        _db.DoctorProfiles.Add(new DoctorProfile
        {
            Id = _doctorProfileId, UserId = _doctorUserId,
            Specialty = "General", LicenseNumber = "LIC-001",
            ConsultationFee = 100, YearsOfExperience = 5
        });

        _db.PatientProfiles.Add(new PatientProfile { Id = _patientProfileId, UserId = Guid.NewGuid() });
        await _db.SaveChangesAsync();

        _handler = new IssuePrescriptionCommandHandler(
            _db, Mock.Of<ILogger<IssuePrescriptionCommandHandler>>());
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_ValidCommand_CreatesActivePrescription()
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
    }

    [TestMethod]
    public async Task Handle_DoctorProfileNotFound_ReturnsNotFoundError()
    {
        var command = new IssuePrescriptionCommand(
            Guid.NewGuid(), _patientProfileId,
            "Amoxicillin", "500mg", "Twice daily", 3, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.NotFound);
        result.Error.Should().Contain("Doctor");
    }

    [TestMethod]
    public async Task Handle_PatientNotFound_ReturnsNotFoundError()
    {
        var command = new IssuePrescriptionCommand(
            _doctorUserId, Guid.NewGuid(),
            "Amoxicillin", "500mg", "Twice daily", 3, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.NotFound);
        result.Error.Should().Contain("Patient");
    }
}

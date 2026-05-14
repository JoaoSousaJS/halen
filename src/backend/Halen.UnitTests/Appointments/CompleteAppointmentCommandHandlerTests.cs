using FluentAssertions;
using Halen.Application.Appointments.Commands;
using Halen.Application.Common;
using Halen.Application.Events;
using Halen.Application.Interfaces;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;

namespace Halen.UnitTests.Appointments;

[TestClass]
public class CompleteAppointmentCommandHandlerTests
{
    private HalenDbContext _db = null!;
    private Mock<IEventBus> _eventBus = null!;
    private CompleteAppointmentCommandHandler _handler = null!;
    private Guid _doctorUserId;
    private Guid _appointmentId;

    [TestInitialize]
    public async Task Initialize()
    {
        var options = new DbContextOptionsBuilder<HalenDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new HalenDbContext(options);

        _doctorUserId = Guid.NewGuid();
        var patientProfileId = Guid.NewGuid();
        var doctorProfileId = Guid.NewGuid();
        _appointmentId = Guid.NewGuid();

        var patientUserId = Guid.NewGuid();
        _db.Users.AddRange(
            new User { Id = patientUserId, FirstName = "Pat", LastName = "Ient", Email = "pat@test.com", UserName = "pat@test.com", Role = UserRole.Patient },
            new User { Id = _doctorUserId, FirstName = "Dr", LastName = "House", Email = "doc@test.com", UserName = "doc@test.com", Role = UserRole.Doctor }
        );

        _db.PatientProfiles.Add(new PatientProfile { Id = patientProfileId, UserId = patientUserId });
        _db.DoctorProfiles.Add(new DoctorProfile { Id = doctorProfileId, UserId = _doctorUserId, Specialty = "Diagnostics", LicenseNumber = "LIC-001", ConsultationFee = 100, YearsOfExperience = 5 });

        _db.Appointments.Add(new Appointment
        {
            Id = _appointmentId,
            PatientId = patientProfileId,
            DoctorId = doctorProfileId,
            ScheduledAt = DateTime.UtcNow.AddDays(1),
            Reason = "Checkup",
            Status = AppointmentStatus.Scheduled,
        });

        await _db.SaveChangesAsync();

        _eventBus = new Mock<IEventBus>();
        _handler = new CompleteAppointmentCommandHandler(
            _db,
            _eventBus.Object,
            Mock.Of<ILogger<CompleteAppointmentCommandHandler>>());
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_DoctorCompletesOwnAppointment_Succeeds()
    {
        var command = new CompleteAppointmentCommand(_doctorUserId, _appointmentId, "All good");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        var appointment = await _db.Appointments.FindAsync(_appointmentId);
        appointment!.Status.Should().Be(AppointmentStatus.Completed);
        appointment.Notes.Should().Be("All good");

        _eventBus.Verify(e => e.PublishAsync(
            Topics.AppointmentCompleted,
            It.Is<AppointmentCompletedEvent>(evt =>
                evt.AppointmentId == _appointmentId &&
                evt.DoctorUserId == _doctorUserId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_DoctorCompletesWithoutNotes_Succeeds()
    {
        var command = new CompleteAppointmentCommand(_doctorUserId, _appointmentId, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
    }

    [TestMethod]
    public async Task Handle_WrongDoctor_ReturnsForbidden()
    {
        var otherDoctorId = Guid.NewGuid();
        _db.DoctorProfiles.Add(new DoctorProfile { Id = Guid.NewGuid(), UserId = otherDoctorId, Specialty = "Cardiology", LicenseNumber = "LIC-002", ConsultationFee = 200, YearsOfExperience = 3 });
        await _db.SaveChangesAsync();

        var command = new CompleteAppointmentCommand(otherDoctorId, _appointmentId, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Forbidden);
    }

    [TestMethod]
    public async Task Handle_AppointmentNotFound_ReturnsNotFoundAndNoEvent()
    {
        var command = new CompleteAppointmentCommand(_doctorUserId, Guid.NewGuid(), null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.NotFound);

        _eventBus.Verify(e => e.PublishAsync(
            It.IsAny<string>(),
            It.IsAny<AppointmentCompletedEvent>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_AlreadyCompleted_ReturnsError()
    {
        var appt = await _db.Appointments.FindAsync(_appointmentId);
        appt!.Status = AppointmentStatus.Completed;
        await _db.SaveChangesAsync();

        var command = new CompleteAppointmentCommand(_doctorUserId, _appointmentId, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Only scheduled");
    }
}

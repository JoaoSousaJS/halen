using FluentAssertions;
using Halen.Application.Appointments.Commands;
using Halen.Application.Common;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;

namespace Halen.UnitTests.Appointments;

[TestClass]
public class CancelAppointmentCommandHandlerTests
{
    private HalenDbContext _db = null!;
    private CancelAppointmentCommandHandler _handler = null!;
    private Guid _patientUserId;
    private Guid _doctorUserId;
    private Guid _patientProfileId;
    private Guid _doctorProfileId;
    private Guid _appointmentId;

    [TestInitialize]
    public async Task Initialize()
    {
        var options = new DbContextOptionsBuilder<HalenDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new HalenDbContext(options);

        _patientUserId = Guid.NewGuid();
        _doctorUserId = Guid.NewGuid();
        _patientProfileId = Guid.NewGuid();
        _doctorProfileId = Guid.NewGuid();
        _appointmentId = Guid.NewGuid();

        _db.Users.AddRange(
            new User { Id = _patientUserId, FirstName = "Pat", LastName = "Ient", Email = "pat@test.com", UserName = "pat@test.com", Role = UserRole.Patient },
            new User { Id = _doctorUserId, FirstName = "Dr", LastName = "House", Email = "doc@test.com", UserName = "doc@test.com", Role = UserRole.Doctor }
        );

        _db.PatientProfiles.Add(new PatientProfile { Id = _patientProfileId, UserId = _patientUserId });
        _db.DoctorProfiles.Add(new DoctorProfile { Id = _doctorProfileId, UserId = _doctorUserId, Specialty = "Diagnostics", LicenseNumber = "LIC-001", ConsultationFee = 100, YearsOfExperience = 5 });

        _db.Appointments.Add(new Appointment
        {
            Id = _appointmentId,
            PatientId = _patientProfileId,
            DoctorId = _doctorProfileId,
            ScheduledAt = DateTime.UtcNow.AddDays(1),
            Reason = "Checkup",
            Status = AppointmentStatus.Scheduled,
        });

        await _db.SaveChangesAsync();

        _handler = new CancelAppointmentCommandHandler(
            _db,
            Mock.Of<ILogger<CancelAppointmentCommandHandler>>());
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_PatientCancelsOwnAppointment_Succeeds()
    {
        var command = new CancelAppointmentCommand(_patientUserId, UserRole.Patient, _appointmentId);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        var appointment = await _db.Appointments.FindAsync(_appointmentId);
        appointment!.Status.Should().Be(AppointmentStatus.Cancelled);
    }

    [TestMethod]
    public async Task Handle_DoctorCancelsOwnAppointment_Succeeds()
    {
        var command = new CancelAppointmentCommand(_doctorUserId, UserRole.Doctor, _appointmentId);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
    }

    [TestMethod]
    public async Task Handle_AdminCancelsAnyAppointment_Succeeds()
    {
        var adminUserId = Guid.NewGuid();
        var command = new CancelAppointmentCommand(adminUserId, UserRole.Admin, _appointmentId);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
    }

    [TestMethod]
    public async Task Handle_PatientCancelsOtherPatientsAppointment_ReturnsForbidden()
    {
        var otherPatientUserId = Guid.NewGuid();
        _db.PatientProfiles.Add(new PatientProfile { Id = Guid.NewGuid(), UserId = otherPatientUserId });
        await _db.SaveChangesAsync();

        var command = new CancelAppointmentCommand(otherPatientUserId, UserRole.Patient, _appointmentId);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Forbidden);
    }

    [TestMethod]
    public async Task Handle_AppointmentNotFound_ReturnsNotFound()
    {
        var command = new CancelAppointmentCommand(_patientUserId, UserRole.Patient, Guid.NewGuid());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.NotFound);
    }

    [TestMethod]
    public async Task Handle_AlreadyCancelledAppointment_ReturnsError()
    {
        var appt = await _db.Appointments.FindAsync(_appointmentId);
        appt!.Status = AppointmentStatus.Cancelled;
        await _db.SaveChangesAsync();

        var command = new CancelAppointmentCommand(_patientUserId, UserRole.Patient, _appointmentId);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Only scheduled");
    }
}

using Halen.UnitTests.Helpers;
using FluentAssertions;
using Halen.Application.Common;
using Halen.Application.Consultations.Commands;
using Halen.Application.Interfaces;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Halen.UnitTests.Consultations;

[TestClass]
public class StartConsultationCommandHandlerTests
{
    private HalenDbContext _db = null!;
    private StartConsultationCommandHandler _handler = null!;
    private Guid _doctorUserId;
    private Guid _patientUserId;
    private Guid _appointmentId;
    private Guid _patientProfileId;
    private Guid _doctorProfileId;

    [TestInitialize]
    public async Task Initialize()
    {
        _db = TestDbFactory.Create();

        _doctorUserId = Guid.NewGuid();
        _patientUserId = Guid.NewGuid();
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
            ScheduledAt = DateTime.UtcNow.AddMinutes(5),
            Reason = "Checkup",
            Status = AppointmentStatus.Scheduled,
        });

        await _db.SaveChangesAsync();

        _handler = new StartConsultationCommandHandler(
            _db,
            Mock.Of<ILogger<StartConsultationCommandHandler>>());
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_CreatesRoom_WhenNoneExists_ReturnsRoomCode()
    {
        var command = new StartConsultationCommand(_doctorUserId, _appointmentId);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.RoomCode.Should().NotBeNullOrEmpty();
        result.RoomCode.Should().StartWith("VC-");

        var room = await _db.ConsultationRooms.FirstOrDefaultAsync(r => r.AppointmentId == _appointmentId);
        room.Should().NotBeNull();
        room!.RoomCode.Should().Be(result.RoomCode);
        room.Status.Should().Be(ConsultationRoomStatus.Waiting);
    }

    [TestMethod]
    public async Task Handle_ReturnsExistingRoomCode_WhenRoomAlreadyExists()
    {
        var existingCode = "VC-ABCD";
        _db.ConsultationRooms.Add(new ConsultationRoom
        {
            AppointmentId = _appointmentId,
            ClinicId = TestTenantContext.DefaultClinicId,
            RoomCode = existingCode,
            Status = ConsultationRoomStatus.Waiting,
        });
        await _db.SaveChangesAsync();

        var command = new StartConsultationCommand(_doctorUserId, _appointmentId);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.RoomCode.Should().Be(existingCode);

        var rooms = await _db.ConsultationRooms
            .Where(r => r.AppointmentId == _appointmentId)
            .ToListAsync();
        rooms.Should().HaveCount(1, "should not create a duplicate room");
    }

    [TestMethod]
    public async Task Handle_AppointmentNotFound_ReturnsNotFound()
    {
        var command = new StartConsultationCommand(_doctorUserId, Guid.NewGuid());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.NotFound);
    }

    [TestMethod]
    public async Task Handle_CallerIsNotDoctorOrPatient_ReturnsForbidden()
    {
        var unrelatedUserId = Guid.NewGuid();
        _db.Users.Add(new User { Id = unrelatedUserId, FirstName = "Random", LastName = "User", Email = "rando@test.com", UserName = "rando@test.com", Role = UserRole.Patient });
        _db.PatientProfiles.Add(new PatientProfile { Id = Guid.NewGuid(), UserId = unrelatedUserId });
        await _db.SaveChangesAsync();

        var command = new StartConsultationCommand(unrelatedUserId, _appointmentId);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Forbidden);
    }

    [TestMethod]
    public async Task Handle_CompletedAppointment_ReturnsValidationError()
    {
        var appt = await _db.Appointments.FindAsync(_appointmentId);
        appt!.Status = AppointmentStatus.Completed;
        await _db.SaveChangesAsync();

        var command = new StartConsultationCommand(_doctorUserId, _appointmentId);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [TestMethod]
    public async Task Handle_CancelledAppointment_ReturnsValidationError()
    {
        var appt = await _db.Appointments.FindAsync(_appointmentId);
        appt!.Status = AppointmentStatus.Cancelled;
        await _db.SaveChangesAsync();

        var command = new StartConsultationCommand(_doctorUserId, _appointmentId);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [TestMethod]
    public async Task Handle_SetsVideoRoomId_OnAppointment()
    {
        var command = new StartConsultationCommand(_doctorUserId, _appointmentId);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        var appt = await _db.Appointments.FindAsync(_appointmentId);
        appt!.VideoRoomId.Should().Be(result.RoomCode);
    }

    [TestMethod]
    public async Task Handle_PatientCanStartConsultation_Succeeds()
    {
        var command = new StartConsultationCommand(_patientUserId, _appointmentId);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.RoomCode.Should().NotBeNullOrEmpty();
    }
}

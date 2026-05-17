using Halen.UnitTests.Helpers;
using FluentAssertions;
using Halen.Application.Appointments.Commands;
using Halen.Application.Common;
using Halen.Application.Consultations.Commands;
using Halen.Application.Interfaces;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Halen.UnitTests.Consultations;

[TestClass]
public class EndConsultationCommandHandlerTests
{
    private HalenDbContext _db = null!;
    private Mock<IMediator> _mediator = null!;
    private EndConsultationCommandHandler _handler = null!;
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

        _db.ConsultationRooms.Add(new ConsultationRoom
        {
            AppointmentId = _appointmentId,
            ClinicId = TestTenantContext.DefaultClinicId,
            RoomCode = "VC-TEST",
            Status = ConsultationRoomStatus.Waiting,
        });

        await _db.SaveChangesAsync();

        _mediator = new Mock<IMediator>();
        _mediator
            .Setup(m => m.Send(It.IsAny<CompleteAppointmentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompleteAppointmentResult(true));

        _handler = new EndConsultationCommandHandler(
            _db,
            _mediator.Object,
            Mock.Of<ILogger<EndConsultationCommandHandler>>());
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_TransitionsRoomToEnded_SetsEndedAt()
    {
        var command = new EndConsultationCommand(_doctorUserId, _appointmentId, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.EndedAt.Should().NotBeNull();
        result.EndedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));

        var room = await _db.ConsultationRooms.FirstOrDefaultAsync(r => r.AppointmentId == _appointmentId);
        room!.Status.Should().Be(ConsultationRoomStatus.Ended);
        room.EndedAt.Should().NotBeNull();
    }

    [TestMethod]
    public async Task Handle_CopiesNotesToRoom_WhenProvided()
    {
        var command = new EndConsultationCommand(_doctorUserId, _appointmentId, "Final consultation notes");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        var room = await _db.ConsultationRooms.FirstOrDefaultAsync(r => r.AppointmentId == _appointmentId);
        room!.Notes.Should().Be("Final consultation notes");
    }

    [TestMethod]
    public async Task Handle_CallerIsNotDoctor_ReturnsForbidden()
    {
        var command = new EndConsultationCommand(_patientUserId, _appointmentId, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Forbidden);
    }

    [TestMethod]
    public async Task Handle_RoomAlreadyEnded_ReturnsError()
    {
        var room = await _db.ConsultationRooms.FirstAsync(r => r.AppointmentId == _appointmentId);
        room.Status = ConsultationRoomStatus.Ended;
        room.EndedAt = DateTimeOffset.UtcNow.AddMinutes(-10);
        await _db.SaveChangesAsync();

        var command = new EndConsultationCommand(_doctorUserId, _appointmentId, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [TestMethod]
    public async Task Handle_NoRoomExists_ReturnsNotFound()
    {
        var otherAppointmentId = Guid.NewGuid();
        _db.Appointments.Add(new Appointment
        {
            Id = otherAppointmentId,
            PatientId = _patientProfileId,
            DoctorId = _doctorProfileId,
            ScheduledAt = DateTime.UtcNow.AddDays(1),
            Reason = "Follow-up",
            Status = AppointmentStatus.Scheduled,
        });
        await _db.SaveChangesAsync();

        var command = new EndConsultationCommand(_doctorUserId, otherAppointmentId, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.NotFound);
    }

    [TestMethod]
    public async Task Handle_DispatchesCompleteAppointmentCommand_ViaMediator()
    {
        var command = new EndConsultationCommand(_doctorUserId, _appointmentId, "Done");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();

        _mediator.Verify(m => m.Send(
            It.Is<CompleteAppointmentCommand>(c =>
                c.UserId == _doctorUserId &&
                c.AppointmentId == _appointmentId),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}

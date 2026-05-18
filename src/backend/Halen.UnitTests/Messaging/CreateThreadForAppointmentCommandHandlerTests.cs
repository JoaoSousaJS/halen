using Halen.Infrastructure.Persistence;
using FluentAssertions;
using Halen.Application.Common;
using Halen.Application.Messaging.Commands;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.UnitTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Halen.UnitTests.Messaging;

[TestClass]
public class CreateThreadForAppointmentCommandHandlerTests
{
    private HalenDbContext _db = null!;
    private CreateThreadForAppointmentCommandHandler _handler = null!;
    private Guid _appointmentId;
    private Guid _patientUserId;
    private Guid _doctorUserId;
    private Guid _clinicId;

    [TestInitialize]
    public async Task Initialize()
    {
        _db = TestDbFactory.Create();
        _clinicId = TestTenantContext.DefaultClinicId;

        _patientUserId = Guid.NewGuid();
        _doctorUserId = Guid.NewGuid();

        var clinic = new Clinic { Id = _clinicId, Name = "Test Clinic", Slug = "test" };
        _db.Clinics.Add(clinic);

        _db.Users.AddRange(
            new User { Id = _patientUserId, ClinicId = _clinicId, FirstName = "Maya", LastName = "Chen", Role = UserRole.Patient, Email = "maya@test.com", UserName = "maya@test.com" },
            new User { Id = _doctorUserId, ClinicId = _clinicId, FirstName = "Amelia", LastName = "Chen", Role = UserRole.Doctor, Email = "dr.chen@test.com", UserName = "dr.chen@test.com" });

        var patientProfile = new PatientProfile { Id = Guid.NewGuid(), UserId = _patientUserId, ClinicId = _clinicId };
        var doctorProfile = new DoctorProfile
        {
            Id = Guid.NewGuid(), UserId = _doctorUserId, ClinicId = _clinicId,
            Specialty = "Cardiology", LicenseNumber = "LIC-001",
            KycStatus = KycStatus.Approved
        };
        _db.PatientProfiles.Add(patientProfile);
        _db.DoctorProfiles.Add(doctorProfile);

        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            ClinicId = _clinicId,
            PatientId = patientProfile.Id,
            DoctorId = doctorProfile.Id,
            ScheduledAt = DateTime.UtcNow.AddDays(1),
            Reason = "Cardiology consult",
            Status = AppointmentStatus.Scheduled
        };
        _appointmentId = appointment.Id;
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        _handler = new CreateThreadForAppointmentCommandHandler(
            _db, Mock.Of<ILogger<CreateThreadForAppointmentCommandHandler>>());
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_ValidAppointment_CreatesThread()
    {
        var command = new CreateThreadForAppointmentCommand(_appointmentId);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ThreadId.Should().NotBeNull();

        var thread = await _db.ConversationThreads.FindAsync(result.ThreadId);
        thread.Should().NotBeNull();
        thread!.AppointmentId.Should().Be(_appointmentId);
        thread.PatientUserId.Should().Be(_patientUserId);
        thread.DoctorUserId.Should().Be(_doctorUserId);
        thread.ClinicId.Should().Be(_clinicId);
        thread.Status.Should().Be(ThreadStatus.Active);
        thread.Subject.Should().Contain("Cardiology");
    }

    [TestMethod]
    public async Task Handle_ValidAppointment_CreatesSystemEventMessage()
    {
        var command = new CreateThreadForAppointmentCommand(_appointmentId);

        var result = await _handler.Handle(command, CancellationToken.None);

        var systemMsg = await _db.ChatMessages
            .FirstOrDefaultAsync(m => m.ThreadId == result.ThreadId && m.MessageType == MessageType.SystemEvent);
        systemMsg.Should().NotBeNull();
        systemMsg!.Content.Should().Contain("Thread created");
    }

    [TestMethod]
    public async Task Handle_ExistingThread_ReturnsExistingIdempotent()
    {
        var firstResult = await _handler.Handle(new CreateThreadForAppointmentCommand(_appointmentId), CancellationToken.None);
        var secondResult = await _handler.Handle(new CreateThreadForAppointmentCommand(_appointmentId), CancellationToken.None);

        secondResult.Success.Should().BeTrue();
        secondResult.ThreadId.Should().Be(firstResult.ThreadId);

        var threadCount = await _db.ConversationThreads.CountAsync(t => t.AppointmentId == _appointmentId);
        threadCount.Should().Be(1);
    }

    [TestMethod]
    public async Task Handle_AppointmentNotFound_ReturnsNotFound()
    {
        var command = new CreateThreadForAppointmentCommand(Guid.NewGuid());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.NotFound);
    }
}

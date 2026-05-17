using Halen.UnitTests.Helpers;
using FluentAssertions;
using Halen.Application.Appointments.Commands;
using Halen.Application.Common;
using Halen.Application.Events;
using Halen.Application.Interfaces;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Halen.UnitTests.Appointments;

[TestClass]
public class CompleteAppointmentCommandHandlerTests
{
    private HalenDbContext _db = null!;
    private Mock<IEventBus> _eventBus = null!;
    private Mock<IPaymentService> _paymentService = null!;
    private CompleteAppointmentCommandHandler _handler = null!;
    private Guid _doctorUserId;
    private Guid _appointmentId;
    private Guid _patientProfileId;
    private Guid _doctorProfileId;

    [TestInitialize]
    public async Task Initialize()
    {
        _db = TestDbFactory.Create();

        _doctorUserId = Guid.NewGuid();
        _patientProfileId = Guid.NewGuid();
        _doctorProfileId = Guid.NewGuid();
        _appointmentId = Guid.NewGuid();

        var patientUserId = Guid.NewGuid();
        _db.Users.AddRange(
            new User { Id = patientUserId, FirstName = "Pat", LastName = "Ient", Email = "pat@test.com", UserName = "pat@test.com", Role = UserRole.Patient },
            new User { Id = _doctorUserId, FirstName = "Dr", LastName = "House", Email = "doc@test.com", UserName = "doc@test.com", Role = UserRole.Doctor }
        );

        _db.PatientProfiles.Add(new PatientProfile { Id = _patientProfileId, UserId = patientUserId });
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

        _eventBus = new Mock<IEventBus>();
        _paymentService = new Mock<IPaymentService>();
        _paymentService
            .Setup(p => p.CaptureIntentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentCaptureResult(true));

        _handler = new CompleteAppointmentCommandHandler(
            _db,
            _eventBus.Object,
            Mock.Of<ILogger<CompleteAppointmentCommandHandler>>(),
            _paymentService.Object);
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

    // ── Payment capture tests (TDD red phase) ───────────────────────────

    [TestMethod]
    public async Task Handle_WithAuthorizedPayment_CapturesPayment()
    {
        _db.Payments.Add(new Payment
        {
            Id = Guid.NewGuid(),
            ClinicId = TestTenantContext.DefaultClinicId,
            AppointmentId = _appointmentId,
            PatientProfileId = _patientProfileId,
            Amount = 100m,
            Currency = "USD",
            Status = PaymentStatus.Authorized,
            PaymentIntentId = "intent_123",
            IdempotencyKey = "key_123",
        });
        await _db.SaveChangesAsync();

        var command = new CompleteAppointmentCommand(_doctorUserId, _appointmentId, "All good");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();

        var payment = await _db.Payments
            .FirstOrDefaultAsync(p => p.AppointmentId == _appointmentId);

        payment.Should().NotBeNull();
        payment!.Status.Should().Be(PaymentStatus.Captured);
        payment.CapturedAt.Should().NotBeNull();
        payment.CapturedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        _paymentService.Verify(p => p.CaptureIntentAsync(
            "intent_123",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_CaptureFailure_StillCompletesAppointment()
    {
        _paymentService
            .Setup(p => p.CaptureIntentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentCaptureResult(false, "Gateway timeout"));

        _db.Payments.Add(new Payment
        {
            Id = Guid.NewGuid(),
            ClinicId = TestTenantContext.DefaultClinicId,
            AppointmentId = _appointmentId,
            PatientProfileId = _patientProfileId,
            Amount = 100m,
            Currency = "USD",
            Status = PaymentStatus.Authorized,
            PaymentIntentId = "intent_456",
            IdempotencyKey = "key_456",
        });
        await _db.SaveChangesAsync();

        var command = new CompleteAppointmentCommand(_doctorUserId, _appointmentId, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        var appointment = await _db.Appointments.FindAsync(_appointmentId);
        appointment!.Status.Should().Be(AppointmentStatus.Completed);

        var payment = await _db.Payments
            .FirstOrDefaultAsync(p => p.AppointmentId == _appointmentId);
        payment!.Status.Should().Be(PaymentStatus.Authorized);
    }

    [TestMethod]
    public async Task Handle_NoPayment_CompletesNormally()
    {
        // No payment seeded — backward compatibility
        var command = new CompleteAppointmentCommand(_doctorUserId, _appointmentId, "Notes");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        var appointment = await _db.Appointments.FindAsync(_appointmentId);
        appointment!.Status.Should().Be(AppointmentStatus.Completed);

        _paymentService.Verify(
            p => p.CaptureIntentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}

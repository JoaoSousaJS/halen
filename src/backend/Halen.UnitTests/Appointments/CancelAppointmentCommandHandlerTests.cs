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
public class CancelAppointmentCommandHandlerTests
{
    private HalenDbContext _db = null!;
    private Mock<IEventBus> _eventBus = null!;
    private Mock<IPaymentService> _paymentService = null!;
    private CancelAppointmentCommandHandler _handler = null!;
    private Guid _patientUserId;
    private Guid _doctorUserId;
    private Guid _patientProfileId;
    private Guid _doctorProfileId;
    private Guid _appointmentId;

    [TestInitialize]
    public async Task Initialize()
    {
        _db = TestDbFactory.Create();

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

        _eventBus = new Mock<IEventBus>();
        _paymentService = new Mock<IPaymentService>();
        _paymentService
            .Setup(p => p.RefundIntentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentRefundResult(true));

        _handler = new CancelAppointmentCommandHandler(
            _db,
            _eventBus.Object,
            Mock.Of<ILogger<CancelAppointmentCommandHandler>>(),
            _paymentService.Object);
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

        _eventBus.Verify(e => e.PublishAsync(
            Topics.AppointmentCancelled,
            It.Is<AppointmentCancelledEvent>(evt =>
                evt.CancelledByUserId == _patientUserId &&
                evt.DoctorUserId == _doctorUserId),
            It.IsAny<CancellationToken>()), Times.Once);
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
        var command = new CancelAppointmentCommand(adminUserId, UserRole.PlatformAdmin, _appointmentId);

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
    public async Task Handle_AppointmentNotFound_ReturnsNotFoundAndNoEvent()
    {
        var command = new CancelAppointmentCommand(_patientUserId, UserRole.Patient, Guid.NewGuid());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.NotFound);

        _eventBus.Verify(e => e.PublishAsync(
            It.IsAny<string>(),
            It.IsAny<AppointmentCancelledEvent>(),
            It.IsAny<CancellationToken>()), Times.Never);
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

    // ── Payment refund tests (TDD red phase) ────────────────────────────

    [TestMethod]
    public async Task Handle_WithAuthorizedPayment_RefundsPayment()
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
            PaymentIntentId = "intent_789",
            IdempotencyKey = "key_789",
        });
        await _db.SaveChangesAsync();

        var command = new CancelAppointmentCommand(_patientUserId, UserRole.Patient, _appointmentId);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();

        var payment = await _db.Payments
            .FirstOrDefaultAsync(p => p.AppointmentId == _appointmentId);

        payment.Should().NotBeNull();
        payment!.Status.Should().Be(PaymentStatus.Refunded);
        payment.RefundedAt.Should().NotBeNull();
        payment.RefundedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        _paymentService.Verify(p => p.RefundIntentAsync(
            "intent_789",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_RefundFailure_StillCancelsAppointment()
    {
        _paymentService
            .Setup(p => p.RefundIntentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentRefundResult(false, "Timeout"));

        _db.Payments.Add(new Payment
        {
            Id = Guid.NewGuid(),
            ClinicId = TestTenantContext.DefaultClinicId,
            AppointmentId = _appointmentId,
            PatientProfileId = _patientProfileId,
            Amount = 100m,
            Currency = "USD",
            Status = PaymentStatus.Authorized,
            PaymentIntentId = "intent_999",
            IdempotencyKey = "key_999",
        });
        await _db.SaveChangesAsync();

        var command = new CancelAppointmentCommand(_patientUserId, UserRole.Patient, _appointmentId);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        var appointment = await _db.Appointments.FindAsync(_appointmentId);
        appointment!.Status.Should().Be(AppointmentStatus.Cancelled);

        var payment = await _db.Payments
            .FirstOrDefaultAsync(p => p.AppointmentId == _appointmentId);
        payment!.Status.Should().Be(PaymentStatus.Authorized);
    }

    [TestMethod]
    public async Task Handle_NoPayment_CancelsNormally()
    {
        // No payment seeded — backward compatibility
        var command = new CancelAppointmentCommand(_patientUserId, UserRole.Patient, _appointmentId);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        var appointment = await _db.Appointments.FindAsync(_appointmentId);
        appointment!.Status.Should().Be(AppointmentStatus.Cancelled);

        _paymentService.Verify(
            p => p.RefundIntentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}

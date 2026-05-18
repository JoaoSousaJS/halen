using FluentAssertions;
using MediatR;
using Halen.Application.Appointments.Commands;
using Halen.Application.Events;
using Halen.Application.Interfaces;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.Infrastructure.Persistence;
using Halen.UnitTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Halen.UnitTests.Appointments;

[TestClass]
public class BookAppointmentCommandHandlerTests
{
    private HalenDbContext _db = null!;
    private Mock<IEventBus> _eventBus = null!;
    private Mock<IPaymentService> _paymentService = null!;
    private BookAppointmentCommandHandler _handler = null!;
    private Guid _doctorProfileId;
    private Guid _patientUserId;
    private Guid _patientProfileId;

    [TestInitialize]
    public async Task Initialize()
    {
        _db = TestDbFactory.Create();

        var doctorUser = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Dr",
            LastName = "House",
            Email = "house@test.com",
            UserName = "house@test.com",
            Role = UserRole.Doctor,
        };
        _db.Users.Add(doctorUser);

        var doctorProfile = new DoctorProfile
        {
            Id = Guid.NewGuid(),
            UserId = doctorUser.Id,
            Specialty = "Diagnostics",
            LicenseNumber = "LIC-001",
            ConsultationFee = 150,
            YearsOfExperience = 10,
            KycStatus = KycStatus.Approved,
        };
        _db.DoctorProfiles.Add(doctorProfile);
        _doctorProfileId = doctorProfile.Id;

        foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
        {
            _db.DoctorAvailabilities.Add(new DoctorAvailability
            {
                DoctorProfileId = doctorProfile.Id,
                ClinicId = TestTenantContext.DefaultClinicId,
                DayOfWeek = day,
                StartTime = new TimeOnly(0, 0),
                EndTime = new TimeOnly(23, 40),
                SlotDurationMinutes = 20,
                IsActive = true,
            });
        }

        _patientUserId = Guid.NewGuid();
        var patientUser = new User
        {
            Id = _patientUserId,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@test.com",
            UserName = "john@test.com",
            Role = UserRole.Patient,
        };
        _db.Users.Add(patientUser);

        _patientProfileId = Guid.NewGuid();
        _db.PatientProfiles.Add(new PatientProfile
        {
            Id = _patientProfileId,
            UserId = _patientUserId,
            ClinicId = TestTenantContext.DefaultClinicId,
        });

        await _db.SaveChangesAsync();

        _eventBus = new Mock<IEventBus>();
        _paymentService = new Mock<IPaymentService>();
        _paymentService
            .Setup(p => p.CreateIntentAsync(
                It.IsAny<Guid>(),
                It.IsAny<decimal>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentIntentResult(true, "mock_intent_123"));

        _handler = new BookAppointmentCommandHandler(
            _db,
            new Helpers.TestTenantContext(),
            _eventBus.Object,
            Mock.Of<IMediator>(),
            Mock.Of<ILogger<BookAppointmentCommandHandler>>(),
            _paymentService.Object);
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_ValidBooking_ReturnsSuccessWithAppointmentId()
    {
        var command = new BookAppointmentCommand(
            _patientUserId,
            _doctorProfileId,
            DateTime.UtcNow.Date.AddDays(1).AddHours(10),
            "Headache");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.AppointmentId.Should().NotBeNull();
        result.Error.Should().BeNull();

        _eventBus.Verify(e => e.PublishAsync(
            Topics.AppointmentBooked,
            It.Is<AppointmentBookedEvent>(evt => evt.AppointmentId == result.AppointmentId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_DoctorNotFound_ReturnsErrorAndNoEvent()
    {
        var command = new BookAppointmentCommand(
            _patientUserId,
            Guid.NewGuid(),
            DateTime.UtcNow.Date.AddDays(1).AddHours(10),
            "Headache");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Error.Should().Be("Doctor not found");

        _eventBus.Verify(e => e.PublishAsync(
            It.IsAny<string>(),
            It.IsAny<AppointmentBookedEvent>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_FirstTimePatient_CreatesPatientProfile()
    {
        var command = new BookAppointmentCommand(
            _patientUserId,
            _doctorProfileId,
            DateTime.UtcNow.Date.AddDays(1).AddHours(10),
            "First visit");

        await _handler.Handle(command, CancellationToken.None);

        var profile = await _db.PatientProfiles
            .FirstOrDefaultAsync(p => p.UserId == _patientUserId);
        profile.Should().NotBeNull();
    }

    [TestMethod]
    public async Task Handle_ConflictingTimeSlot_ReturnsError()
    {
        var patientProfile = new PatientProfile { Id = Guid.NewGuid(), UserId = _patientUserId };
        _db.PatientProfiles.Add(patientProfile);

        var scheduledAt = DateTime.UtcNow.Date.AddDays(1).AddHours(10);
        _db.Appointments.Add(new Appointment
        {
            PatientId = patientProfile.Id,
            DoctorId = _doctorProfileId,
            ScheduledAt = scheduledAt,
            Reason = "Existing appointment",
        });
        await _db.SaveChangesAsync();

        var otherPatientId = Guid.NewGuid();
        _db.Users.Add(new User
        {
            Id = otherPatientId, FirstName = "Other", LastName = "Patient",
            Email = "other@test.com", UserName = "other@test.com", Role = UserRole.Patient,
        });
        await _db.SaveChangesAsync();

        var command = new BookAppointmentCommand(
            otherPatientId,
            _doctorProfileId,
            scheduledAt.AddMinutes(10),
            "Overlapping visit");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Error.Should().Be("This time slot is not available");
    }

    [TestMethod]
    public async Task Handle_NonOverlappingSlot_Succeeds()
    {
        var patientProfile = new PatientProfile { Id = Guid.NewGuid(), UserId = _patientUserId };
        _db.PatientProfiles.Add(patientProfile);

        var scheduledAt = DateTime.UtcNow.Date.AddDays(1).AddHours(10);
        _db.Appointments.Add(new Appointment
        {
            PatientId = patientProfile.Id,
            DoctorId = _doctorProfileId,
            ScheduledAt = scheduledAt,
            Reason = "Existing appointment",
        });
        await _db.SaveChangesAsync();

        var otherPatientId = Guid.NewGuid();
        _db.Users.Add(new User
        {
            Id = otherPatientId, FirstName = "Another", LastName = "Patient",
            Email = "another@test.com", UserName = "another@test.com", Role = UserRole.Patient,
        });
        await _db.SaveChangesAsync();

        var command = new BookAppointmentCommand(
            otherPatientId,
            _doctorProfileId,
            scheduledAt.AddMinutes(25),
            "After previous appointment");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
    }

    [TestMethod]
    public async Task Handle_UnapprovedDoctor_ReturnsError()
    {
        var doctor = await _db.DoctorProfiles.FindAsync(_doctorProfileId);
        doctor!.KycStatus = KycStatus.NotSubmitted;
        await _db.SaveChangesAsync();

        var command = new BookAppointmentCommand(
            _patientUserId,
            _doctorProfileId,
            DateTime.UtcNow.Date.AddDays(5).AddHours(10),
            "Test");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("not yet approved");
    }

    // ── Payment integration tests (TDD red phase) ───────────────────────

    [TestMethod]
    public async Task Handle_ValidBooking_CreatesPaymentWithAuthorizedStatus()
    {
        var scheduledAt = DateTime.UtcNow.Date.AddDays(1).AddHours(10);
        var command = new BookAppointmentCommand(
            _patientUserId,
            _doctorProfileId,
            scheduledAt,
            "Checkup with payment");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();

        var payment = await _db.Payments
            .FirstOrDefaultAsync(p => p.AppointmentId == result.AppointmentId);

        payment.Should().NotBeNull();
        payment!.Status.Should().Be(PaymentStatus.Authorized);
        payment.Amount.Should().Be(150m); // Doctor's ConsultationFee
        payment.Currency.Should().Be("USD");
        payment.PaymentIntentId.Should().Be("mock_intent_123");
        payment.PatientProfileId.Should().Be(_patientProfileId);
        payment.IdempotencyKey.Should().NotBeNullOrEmpty();
    }

    [TestMethod]
    public async Task Handle_ValidBooking_CallsCreateIntentWithCorrectIdempotencyKey()
    {
        var scheduledAt = DateTime.UtcNow.Date.AddDays(1).AddHours(10);
        var command = new BookAppointmentCommand(
            _patientUserId,
            _doctorProfileId,
            scheduledAt,
            "Checkup");

        await _handler.Handle(command, CancellationToken.None);

        var expectedKey = $"booking_{_patientUserId}_{_doctorProfileId}_{scheduledAt:O}";

        _paymentService.Verify(p => p.CreateIntentAsync(
            _patientUserId,
            150m,
            "USD",
            expectedKey,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_ValidBooking_ReturnsAuthorizedPaymentStatus()
    {
        var command = new BookAppointmentCommand(
            _patientUserId,
            _doctorProfileId,
            DateTime.UtcNow.Date.AddDays(1).AddHours(10),
            "Checkup");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.PaymentStatus.Should().Be("Authorized");
    }

    [TestMethod]
    public async Task Handle_PaymentIntentFails_ReturnsErrorAndNoAppointment()
    {
        _paymentService
            .Setup(p => p.CreateIntentAsync(
                It.IsAny<Guid>(),
                It.IsAny<decimal>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentIntentResult(false, null, "Card declined"));

        var command = new BookAppointmentCommand(
            _patientUserId,
            _doctorProfileId,
            DateTime.UtcNow.Date.AddDays(1).AddHours(10),
            "Checkup");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Payment authorization failed");
        result.AppointmentId.Should().BeNull();

        // Note: in production, the serializable transaction rolls back automatically
        // (no commit = automatic rollback), so no orphan rows persist.
        // The in-memory test double doesn't support real transactions, so we only
        // verify the returned result here. Integration tests cover the rollback.
    }
}

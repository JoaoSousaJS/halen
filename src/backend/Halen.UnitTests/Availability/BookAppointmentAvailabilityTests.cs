using FluentAssertions;
using MediatR;
using Halen.Application.Appointments.Commands;
using Halen.Application.Events;
using Halen.Application.Interfaces;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.Infrastructure.Persistence;
using Halen.UnitTests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Halen.UnitTests.Availability;

[TestClass]
public class BookAppointmentAvailabilityTests
{
    private HalenDbContext _db = null!;
    private Mock<IEventBus> _eventBus = null!;
    private BookAppointmentCommandHandler _handler = null!;
    private Guid _doctorProfileId;
    private Guid _patientUserId;

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
        await _db.SaveChangesAsync();

        _eventBus = new Mock<IEventBus>();
        var paymentService = new Mock<IPaymentService>();
        paymentService
            .Setup(p => p.CreateIntentAsync(
                It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentIntentResult(true, "mock_intent"));
        _handler = new BookAppointmentCommandHandler(
            _db,
            new TestTenantContext(),
            _eventBus.Object,
            Mock.Of<IMediator>(),
            Mock.Of<ILogger<BookAppointmentCommandHandler>>(),
            paymentService.Object);
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_BookingWithinAvailabilityWindow_Succeeds()
    {
        // Doctor is available Monday 09:00-12:00
        // Book at Monday 09:00 — should succeed
        var nextMonday = GetNextDateForDayOfWeek(DayOfWeek.Monday);
        var scheduledAt = nextMonday.ToDateTime(new TimeOnly(9, 0), DateTimeKind.Utc);

        _db.DoctorAvailabilities.Add(new DoctorAvailability
        {
            DoctorProfileId = _doctorProfileId,
            ClinicId = TestTenantContext.DefaultClinicId,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(12, 0),
            SlotDurationMinutes = 20,
            IsActive = true,
        });
        await _db.SaveChangesAsync();

        var command = new BookAppointmentCommand(_patientUserId, _doctorProfileId, scheduledAt, "Check-up");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.AppointmentId.Should().NotBeNull();
    }

    [TestMethod]
    public async Task Handle_BookingOutsideAvailabilityWindow_ReturnsError()
    {
        // Doctor is available Monday 09:00-12:00
        // Book at Monday 14:00 — should fail
        var nextMonday = GetNextDateForDayOfWeek(DayOfWeek.Monday);
        var scheduledAt = nextMonday.ToDateTime(new TimeOnly(14, 0), DateTimeKind.Utc);

        _db.DoctorAvailabilities.Add(new DoctorAvailability
        {
            DoctorProfileId = _doctorProfileId,
            ClinicId = TestTenantContext.DefaultClinicId,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(12, 0),
            SlotDurationMinutes = 20,
            IsActive = true,
        });
        await _db.SaveChangesAsync();

        var command = new BookAppointmentCommand(_patientUserId, _doctorProfileId, scheduledAt, "Check-up");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Error.Should().Be("Doctor is not available at the requested time.");
    }

    [TestMethod]
    public async Task Handle_BookingOnDayWithNoAvailability_ReturnsError()
    {
        // Doctor is available Monday 09:00-12:00, but booking is on Tuesday
        var nextTuesday = GetNextDateForDayOfWeek(DayOfWeek.Tuesday);
        var scheduledAt = nextTuesday.ToDateTime(new TimeOnly(9, 0), DateTimeKind.Utc);

        _db.DoctorAvailabilities.Add(new DoctorAvailability
        {
            DoctorProfileId = _doctorProfileId,
            ClinicId = TestTenantContext.DefaultClinicId,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(12, 0),
            SlotDurationMinutes = 20,
            IsActive = true,
        });
        await _db.SaveChangesAsync();

        var command = new BookAppointmentCommand(_patientUserId, _doctorProfileId, scheduledAt, "Check-up");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Error.Should().Be("Doctor is not available at the requested time.");
    }

    private static DateOnly GetNextDateForDayOfWeek(DayOfWeek target)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var candidate = today.AddDays(7);
        while (candidate.DayOfWeek != target)
            candidate = candidate.AddDays(1);
        return candidate;
    }
}

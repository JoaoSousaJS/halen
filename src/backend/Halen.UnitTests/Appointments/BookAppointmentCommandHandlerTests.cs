using FluentAssertions;
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
        _handler = new BookAppointmentCommandHandler(
            _db,
            new Helpers.TestTenantContext(),
            _eventBus.Object,
            Mock.Of<ILogger<BookAppointmentCommandHandler>>());
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_ValidBooking_ReturnsSuccessWithAppointmentId()
    {
        var command = new BookAppointmentCommand(
            _patientUserId,
            _doctorProfileId,
            DateTime.UtcNow.AddDays(1),
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
            DateTime.UtcNow.AddDays(1),
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
            DateTime.UtcNow.AddDays(1),
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

        var scheduledAt = DateTime.UtcNow.AddDays(1);
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

        var scheduledAt = DateTime.UtcNow.AddDays(1);
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
            DateTime.UtcNow.AddDays(5),
            "Test");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("not yet approved");
    }
}

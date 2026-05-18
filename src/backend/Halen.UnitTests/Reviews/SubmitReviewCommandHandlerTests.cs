using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using Halen.Application.Reviews.Commands;
using Halen.Application.Events;
using Halen.Application.Common;
using Halen.Application.Interfaces;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.Infrastructure.Persistence;
using Halen.UnitTests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Halen.UnitTests.Reviews;

[TestClass]
public class SubmitReviewCommandHandlerTests
{
    private HalenDbContext _db = null!;
    private Mock<IEventBus> _eventBus = null!;
    private SubmitReviewCommandHandler _handler = null!;

    private Guid _doctorUserId;
    private Guid _doctorProfileId;
    private Guid _patientUserId;
    private Guid _patientProfileId;
    private Guid _appointmentId;

    [TestInitialize]
    public async Task Initialize()
    {
        _db = TestDbFactory.Create();
        _eventBus = new Mock<IEventBus>();

        _doctorUserId = Guid.NewGuid();
        _doctorProfileId = Guid.NewGuid();
        _patientUserId = Guid.NewGuid();
        _patientProfileId = Guid.NewGuid();
        _appointmentId = Guid.NewGuid();

        var doctorUser = new User
        {
            Id = _doctorUserId, FirstName = "Dr", LastName = "House",
            Email = "house@test.com", UserName = "house@test.com", Role = UserRole.Doctor
        };
        var doctorProfile = new DoctorProfile
        {
            Id = _doctorProfileId, UserId = _doctorUserId,
            Specialty = "Diagnostics", LicenseNumber = "LIC-001",
            ConsultationFee = 150, YearsOfExperience = 10,
            KycStatus = KycStatus.Approved
        };

        var patientUser = new User
        {
            Id = _patientUserId, FirstName = "Maya", LastName = "Carter",
            Email = "maya@test.com", UserName = "maya@test.com", Role = UserRole.Patient
        };
        var patientProfile = new PatientProfile
        {
            Id = _patientProfileId, UserId = _patientUserId
        };

        var appointment = new Appointment
        {
            Id = _appointmentId, PatientId = _patientProfileId, DoctorId = _doctorProfileId,
            ScheduledAt = DateTime.UtcNow.Date.AddDays(-1).AddHours(10),
            DurationMinutes = 20, Reason = "Check-up",
            Status = AppointmentStatus.Completed,
            ClinicId = TestTenantContext.DefaultClinicId
        };

        _db.Users.AddRange(doctorUser, patientUser);
        _db.DoctorProfiles.Add(doctorProfile);
        _db.PatientProfiles.Add(patientProfile);
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        _handler = new SubmitReviewCommandHandler(
            _db, new TestTenantContext(), _eventBus.Object,
            Mock.Of<ILogger<SubmitReviewCommandHandler>>());
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    private SubmitReviewCommand ValidCommand(int rating = 5) =>
        new(_patientUserId, _appointmentId, rating, "Great doctor", "Very thorough", ["listens", "thorough"]);

    // ── Success path ────────────────────────────────────────────────

    [TestMethod]
    public async Task Handle_ValidCommand_CreatesReviewAndUpdatesAggregates()
    {
        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ReviewId.Should().NotBeNull();

        var review = await _db.Reviews.FindAsync(result.ReviewId);
        review.Should().NotBeNull();
        review!.Rating.Should().Be(5);
        review.Title.Should().Be("Great doctor");
        review.Body.Should().Be("Very thorough");
        review.Tags.Should().BeEquivalentTo(["listens", "thorough"]);
        review.PatientProfileId.Should().Be(_patientProfileId);
        review.DoctorProfileId.Should().Be(_doctorProfileId);
        review.AppointmentId.Should().Be(_appointmentId);
        review.ClinicId.Should().Be(TestTenantContext.DefaultClinicId);

        var doctor = await _db.DoctorProfiles.FindAsync(_doctorProfileId);
        doctor!.AverageRating.Should().Be(5);
        doctor.ReviewCount.Should().Be(1);
    }

    [TestMethod]
    public async Task Handle_ValidCommand_PublishesReviewSubmittedEvent()
    {
        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.Success.Should().BeTrue();

        _eventBus.Verify(e => e.PublishAsync(
            Topics.ReviewSubmitted,
            It.Is<ReviewSubmittedEvent>(evt =>
                evt.ReviewId == result.ReviewId &&
                evt.Rating == 5 &&
                evt.DoctorUserId == _doctorUserId &&
                evt.PatientUserId == _patientUserId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_LowStarRating_PublishesBothEvents()
    {
        var result = await _handler.Handle(ValidCommand(rating: 2), CancellationToken.None);

        result.Success.Should().BeTrue();

        _eventBus.Verify(e => e.PublishAsync(
            Topics.ReviewSubmitted,
            It.Is<ReviewSubmittedEvent>(evt => evt.ReviewId == result.ReviewId && evt.Rating == 2),
            It.IsAny<CancellationToken>()), Times.Once);

        _eventBus.Verify(e => e.PublishAsync(
            Topics.ReviewLowStar,
            It.Is<ReviewLowStarEvent>(evt =>
                evt.ReviewId == result.ReviewId &&
                evt.Rating == 2 &&
                evt.DoctorUserId == _doctorUserId &&
                evt.DoctorProfileId == _doctorProfileId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_HighStarRating_DoesNotPublishLowStarEvent()
    {
        var result = await _handler.Handle(ValidCommand(rating: 4), CancellationToken.None);

        result.Success.Should().BeTrue();

        _eventBus.Verify(e => e.PublishAsync(
            Topics.ReviewSubmitted,
            It.IsAny<ReviewSubmittedEvent>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _eventBus.Verify(e => e.PublishAsync(
            Topics.ReviewLowStar,
            It.IsAny<ReviewLowStarEvent>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Error paths ─────────────────────────────────────────────────

    [TestMethod]
    public async Task Handle_PatientNotFound_ReturnsNotFound()
    {
        var command = new SubmitReviewCommand(
            Guid.NewGuid(), _appointmentId, 5, "Great", "Body", ["listens"]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.NotFound);

        _eventBus.Verify(e => e.PublishAsync(
            It.IsAny<string>(), It.IsAny<ReviewSubmittedEvent>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_AppointmentNotFound_ReturnsNotFound()
    {
        var command = new SubmitReviewCommand(
            _patientUserId, Guid.NewGuid(), 5, "Great", "Body", ["listens"]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.NotFound);

        _eventBus.Verify(e => e.PublishAsync(
            It.IsAny<string>(), It.IsAny<ReviewSubmittedEvent>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_PatientDoesNotOwnAppointment_ReturnsForbidden()
    {
        // Create a second patient who did NOT own the appointment
        var otherPatientUserId = Guid.NewGuid();
        var otherPatientProfileId = Guid.NewGuid();
        _db.Users.Add(new User
        {
            Id = otherPatientUserId, FirstName = "Other", LastName = "Patient",
            Email = "other@test.com", UserName = "other@test.com", Role = UserRole.Patient
        });
        _db.PatientProfiles.Add(new PatientProfile
        {
            Id = otherPatientProfileId, UserId = otherPatientUserId
        });
        await _db.SaveChangesAsync();

        var command = new SubmitReviewCommand(
            otherPatientUserId, _appointmentId, 5, "Great", "Body", ["listens"]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Forbidden);

        _eventBus.Verify(e => e.PublishAsync(
            It.IsAny<string>(), It.IsAny<ReviewSubmittedEvent>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_AppointmentNotCompleted_ReturnsValidation()
    {
        var scheduledAppointmentId = Guid.NewGuid();
        _db.Appointments.Add(new Appointment
        {
            Id = scheduledAppointmentId, PatientId = _patientProfileId, DoctorId = _doctorProfileId,
            ScheduledAt = DateTime.UtcNow.Date.AddDays(1).AddHours(10),
            DurationMinutes = 20, Reason = "Future visit",
            Status = AppointmentStatus.Scheduled,
            ClinicId = TestTenantContext.DefaultClinicId
        });
        await _db.SaveChangesAsync();

        var command = new SubmitReviewCommand(
            _patientUserId, scheduledAppointmentId, 5, "Great", "Body", ["listens"]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Validation);

        _eventBus.Verify(e => e.PublishAsync(
            It.IsAny<string>(), It.IsAny<ReviewSubmittedEvent>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_DuplicateReview_ReturnsValidation()
    {
        // Submit the first review
        var firstResult = await _handler.Handle(ValidCommand(), CancellationToken.None);
        firstResult.Success.Should().BeTrue();

        // Attempt a duplicate review for the same appointment
        var result = await _handler.Handle(ValidCommand(rating: 4), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Validation);
    }

    // ── Computed fields ─────────────────────────────────────────────

    [TestMethod]
    public async Task Handle_SetsPostedAsCorrectly()
    {
        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.Success.Should().BeTrue();

        var review = await _db.Reviews.FindAsync(result.ReviewId);
        review.Should().NotBeNull();
        review!.PostedAs.Should().Be("Maya C.");
    }

    [TestMethod]
    public async Task Handle_SetsIsVerifiedTrue()
    {
        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.Success.Should().BeTrue();

        var review = await _db.Reviews.FindAsync(result.ReviewId);
        review.Should().NotBeNull();
        review!.IsVerified.Should().BeTrue();
    }

    // ── Resilience ──────────────────────────────────────────────────

    [TestMethod]
    public async Task Handle_EventBusFailure_StillReturnsSuccess()
    {
        _eventBus.Setup(e => e.PublishAsync(
            It.IsAny<string>(),
            It.IsAny<ReviewSubmittedEvent>(),
            It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Kafka down"));

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ReviewId.Should().NotBeNull();
    }
}

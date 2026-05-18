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
public class ModerateReviewCommandHandlerTests
{
    private HalenDbContext _db = null!;
    private ModerateReviewCommandHandler _handler = null!;

    private Guid _adminUserId;
    private Guid _doctorUserId;
    private Guid _doctorProfileId;
    private Guid _patientUserId;
    private Guid _patientProfileId;
    private Guid _appointmentId;
    private Guid _reviewId;

    [TestInitialize]
    public async Task Initialize()
    {
        _db = TestDbFactory.Create();

        _adminUserId = Guid.NewGuid();
        _doctorUserId = Guid.NewGuid();
        _doctorProfileId = Guid.NewGuid();
        _patientUserId = Guid.NewGuid();
        _patientProfileId = Guid.NewGuid();
        _appointmentId = Guid.NewGuid();
        _reviewId = Guid.NewGuid();

        _db.Users.AddRange(
            new User
            {
                Id = _adminUserId, FirstName = "Admin", LastName = "User",
                Email = "admin@test.com", UserName = "admin@test.com", Role = UserRole.PlatformAdmin
            },
            new User
            {
                Id = _doctorUserId, FirstName = "Dr", LastName = "House",
                Email = "house@test.com", UserName = "house@test.com", Role = UserRole.Doctor
            },
            new User
            {
                Id = _patientUserId, FirstName = "Maya", LastName = "Carter",
                Email = "maya@test.com", UserName = "maya@test.com", Role = UserRole.Patient
            }
        );

        // Doctor profile starts with one existing review baked into aggregates
        _db.DoctorProfiles.Add(new DoctorProfile
        {
            Id = _doctorProfileId, UserId = _doctorUserId,
            Specialty = "Diagnostics", LicenseNumber = "LIC-001",
            ConsultationFee = 150, YearsOfExperience = 10,
            KycStatus = KycStatus.Approved,
            AverageRating = 5m,
            ReviewCount = 1
        });

        _db.PatientProfiles.Add(new PatientProfile
        {
            Id = _patientProfileId, UserId = _patientUserId
        });

        _db.Appointments.Add(new Appointment
        {
            Id = _appointmentId, PatientId = _patientProfileId, DoctorId = _doctorProfileId,
            ScheduledAt = DateTime.UtcNow.Date.AddDays(-1).AddHours(10),
            DurationMinutes = 20, Reason = "Check-up",
            Status = AppointmentStatus.Completed,
            ClinicId = TestTenantContext.DefaultClinicId
        });

        // The review under moderation — starts as Pending for the "approve" test,
        // individual tests change status as needed via helper.
        _db.Reviews.Add(new Review
        {
            Id = _reviewId,
            AppointmentId = _appointmentId,
            PatientProfileId = _patientProfileId,
            DoctorProfileId = _doctorProfileId,
            Rating = 5,
            Title = "Great doctor",
            Body = "Very thorough",
            Tags = ["listens"],
            IsVerified = true,
            PostedAs = "Maya C.",
            ModerationStatus = ReviewModerationStatus.Pending,
            ClinicId = TestTenantContext.DefaultClinicId
        });

        await _db.SaveChangesAsync();

        _handler = new ModerateReviewCommandHandler(_db);
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    // ── Success paths ───────────────────────────────────────────────

    [TestMethod]
    public async Task Handle_ApproveReview_UpdatesStatus()
    {
        var command = new ModerateReviewCommand(
            _adminUserId, _reviewId, ReviewModerationStatus.Approved);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();

        var review = await _db.Reviews.FindAsync(_reviewId);
        review!.ModerationStatus.Should().Be(ReviewModerationStatus.Approved);

        // After approval the doctor aggregates should be recalculated to include this review
        var doctor = await _db.DoctorProfiles.FindAsync(_doctorProfileId);
        doctor!.ReviewCount.Should().BeGreaterThanOrEqualTo(1);
        doctor.AverageRating.Should().NotBeNull();
    }

    [TestMethod]
    public async Task Handle_HideReview_UpdatesStatusAndRecalcAggregates()
    {
        // Start the review as Approved so hiding it is a status change
        var review = await _db.Reviews.FindAsync(_reviewId);
        review!.ModerationStatus = ReviewModerationStatus.Approved;
        await _db.SaveChangesAsync();

        var command = new ModerateReviewCommand(
            _adminUserId, _reviewId, ReviewModerationStatus.Hidden);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();

        review = await _db.Reviews.FindAsync(_reviewId);
        review!.ModerationStatus.Should().Be(ReviewModerationStatus.Hidden);

        // With the only approved review now hidden, the doctor's aggregates should reflect zero visible reviews
        var doctor = await _db.DoctorProfiles.FindAsync(_doctorProfileId);
        doctor!.ReviewCount.Should().Be(0);
        doctor.AverageRating.Should().BeNull();
    }

    [TestMethod]
    public async Task Handle_RemoveReview_UpdatesStatusAndRecalcAggregates()
    {
        // Start the review as Approved so removing it is a status change
        var review = await _db.Reviews.FindAsync(_reviewId);
        review!.ModerationStatus = ReviewModerationStatus.Approved;
        await _db.SaveChangesAsync();

        var command = new ModerateReviewCommand(
            _adminUserId, _reviewId, ReviewModerationStatus.Removed);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();

        review = await _db.Reviews.FindAsync(_reviewId);
        review!.ModerationStatus.Should().Be(ReviewModerationStatus.Removed);

        // With the only approved review now removed, the doctor's aggregates should reflect zero visible reviews
        var doctor = await _db.DoctorProfiles.FindAsync(_doctorProfileId);
        doctor!.ReviewCount.Should().Be(0);
        doctor.AverageRating.Should().BeNull();
    }

    // ── Error paths ─────────────────────────────────────────────────

    [TestMethod]
    public async Task Handle_ReviewNotFound_ReturnsNotFound()
    {
        var command = new ModerateReviewCommand(
            _adminUserId, Guid.NewGuid(), ReviewModerationStatus.Approved);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.NotFound);
    }

    [TestMethod]
    public async Task Handle_SetToPending_ReturnsValidation()
    {
        var command = new ModerateReviewCommand(
            _adminUserId, _reviewId, ReviewModerationStatus.Pending);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Validation);
    }
}

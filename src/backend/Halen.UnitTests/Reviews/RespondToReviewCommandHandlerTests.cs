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
public class RespondToReviewCommandHandlerTests
{
    private HalenDbContext _db = null!;
    private RespondToReviewCommandHandler _handler = null!;

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

        _doctorUserId = Guid.NewGuid();
        _doctorProfileId = Guid.NewGuid();
        _patientUserId = Guid.NewGuid();
        _patientProfileId = Guid.NewGuid();
        _appointmentId = Guid.NewGuid();
        _reviewId = Guid.NewGuid();

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

        var review = new Review
        {
            Id = _reviewId,
            AppointmentId = _appointmentId,
            PatientProfileId = _patientProfileId,
            DoctorProfileId = _doctorProfileId,
            Rating = 5,
            Title = "Great doctor",
            Body = "Very thorough",
            Tags = ["listens", "thorough"],
            IsVerified = true,
            PostedAs = "Maya C.",
            ModerationStatus = ReviewModerationStatus.Approved,
            ClinicId = TestTenantContext.DefaultClinicId
        };

        _db.Users.AddRange(doctorUser, patientUser);
        _db.DoctorProfiles.Add(doctorProfile);
        _db.PatientProfiles.Add(patientProfile);
        _db.Appointments.Add(appointment);
        _db.Reviews.Add(review);
        await _db.SaveChangesAsync();

        _handler = new RespondToReviewCommandHandler(_db);
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    // ── Success path ────────────────────────────────────────────────

    [TestMethod]
    public async Task Handle_ValidCommand_SetsResponseAndTimestamp()
    {
        var command = new RespondToReviewCommand(
            _doctorUserId, _reviewId, "Thank you for your kind words!");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();

        var review = await _db.Reviews.FindAsync(_reviewId);
        review.Should().NotBeNull();
        review!.DoctorResponse.Should().Be("Thank you for your kind words!");
        review.DoctorRespondedAt.Should().NotBeNull();
        review.DoctorRespondedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    // ── Error paths ─────────────────────────────────────────────────

    [TestMethod]
    public async Task Handle_DoctorNotFound_ReturnsNotFound()
    {
        var command = new RespondToReviewCommand(
            Guid.NewGuid(), _reviewId, "Thanks!");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.NotFound);
    }

    [TestMethod]
    public async Task Handle_ReviewNotFound_ReturnsNotFound()
    {
        var command = new RespondToReviewCommand(
            _doctorUserId, Guid.NewGuid(), "Thanks!");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.NotFound);
    }

    [TestMethod]
    public async Task Handle_NotDoctorsReview_ReturnsForbidden()
    {
        // Create another doctor who did NOT receive this review
        var otherDoctorUserId = Guid.NewGuid();
        var otherDoctorProfileId = Guid.NewGuid();
        _db.Users.Add(new User
        {
            Id = otherDoctorUserId, FirstName = "Dr", LastName = "Wilson",
            Email = "wilson@test.com", UserName = "wilson@test.com", Role = UserRole.Doctor
        });
        _db.DoctorProfiles.Add(new DoctorProfile
        {
            Id = otherDoctorProfileId, UserId = otherDoctorUserId,
            Specialty = "Oncology", LicenseNumber = "LIC-002",
            ConsultationFee = 200, YearsOfExperience = 15,
            KycStatus = KycStatus.Approved
        });
        await _db.SaveChangesAsync();

        var command = new RespondToReviewCommand(
            otherDoctorUserId, _reviewId, "Thanks!");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Forbidden);
    }

    [TestMethod]
    public async Task Handle_AlreadyResponded_ReturnsValidation()
    {
        // Pre-set a doctor response on the review
        var review = await _db.Reviews.FindAsync(_reviewId);
        review!.DoctorResponse = "Already responded";
        review.DoctorRespondedAt = DateTime.UtcNow.AddHours(-1);
        await _db.SaveChangesAsync();

        var command = new RespondToReviewCommand(
            _doctorUserId, _reviewId, "Trying to respond again");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Validation);
    }
}

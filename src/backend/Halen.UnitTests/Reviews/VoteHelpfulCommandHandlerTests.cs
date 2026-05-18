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
public class VoteHelpfulCommandHandlerTests
{
    private HalenDbContext _db = null!;
    private VoteHelpfulCommandHandler _handler = null!;

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

        _db.Users.AddRange(
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

        _db.DoctorProfiles.Add(new DoctorProfile
        {
            Id = _doctorProfileId, UserId = _doctorUserId,
            Specialty = "Diagnostics", LicenseNumber = "LIC-001",
            ConsultationFee = 150, YearsOfExperience = 10,
            KycStatus = KycStatus.Approved
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
            HelpfulCount = 0,
            PostedAs = "Maya C.",
            ModerationStatus = ReviewModerationStatus.Approved,
            ClinicId = TestTenantContext.DefaultClinicId
        });

        await _db.SaveChangesAsync();

        _handler = new VoteHelpfulCommandHandler(_db);
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    // ── Success path ────────────────────────────────────────────────

    [TestMethod]
    public async Task Handle_ValidVote_IncrementsCount()
    {
        var command = new VoteHelpfulCommand(_reviewId);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.NewCount.Should().Be(1);

        var review = await _db.Reviews.FindAsync(_reviewId);
        review!.HelpfulCount.Should().Be(1);
    }

    // ── Error paths ─────────────────────────────────────────────────

    [TestMethod]
    public async Task Handle_ReviewNotFound_ReturnsNotFound()
    {
        var command = new VoteHelpfulCommand(Guid.NewGuid());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.NotFound);
    }

    [TestMethod]
    public async Task Handle_NonApprovedReview_ReturnsNotFound()
    {
        // Hide the review — voting on hidden reviews should return NotFound (don't reveal existence)
        var review = await _db.Reviews.FindAsync(_reviewId);
        review!.ModerationStatus = ReviewModerationStatus.Hidden;
        await _db.SaveChangesAsync();

        var command = new VoteHelpfulCommand(_reviewId);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.NotFound);
    }
}

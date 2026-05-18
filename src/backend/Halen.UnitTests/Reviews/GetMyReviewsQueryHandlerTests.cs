using FluentAssertions;
using Halen.Application.Reviews.Queries;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.Infrastructure.Persistence;
using Halen.UnitTests.Helpers;

namespace Halen.UnitTests.Reviews;

[TestClass]
public class GetMyReviewsQueryHandlerTests
{
    private HalenDbContext _db = null!;
    private GetMyReviewsQueryHandler _handler = null!;

    private Guid _doctorUserId;
    private Guid _doctorProfileId;
    private Guid _patientProfileId;

    [TestInitialize]
    public async Task Initialize()
    {
        _db = TestDbFactory.Create();
        _handler = new GetMyReviewsQueryHandler(_db);

        // Seed doctor user + profile
        _doctorUserId = Guid.NewGuid();
        var doctorUser = new User
        {
            Id = _doctorUserId,
            FirstName = "Carlos",
            LastName = "Mendes",
            Email = "carlos.mendes@test.com",
            UserName = "carlos.mendes@test.com",
            Role = UserRole.Doctor,
            ClinicId = TestTenantContext.DefaultClinicId,
        };
        _db.Users.Add(doctorUser);

        var doctorProfile = new DoctorProfile
        {
            Id = Guid.NewGuid(),
            UserId = _doctorUserId,
            ClinicId = TestTenantContext.DefaultClinicId,
            Specialty = "Cardiology",
            LicenseNumber = "LIC-001",
            ConsultationFee = 150,
            YearsOfExperience = 10,
            KycStatus = KycStatus.Approved,
        };
        _db.DoctorProfiles.Add(doctorProfile);
        _doctorProfileId = doctorProfile.Id;

        // Seed patient user + profile
        var patientUser = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Ana",
            LastName = "Costa",
            Email = "ana.costa@test.com",
            UserName = "ana.costa@test.com",
            Role = UserRole.Patient,
            ClinicId = TestTenantContext.DefaultClinicId,
        };
        _db.Users.Add(patientUser);

        var patientProfile = new PatientProfile
        {
            Id = Guid.NewGuid(),
            UserId = patientUser.Id,
            ClinicId = TestTenantContext.DefaultClinicId,
            DateOfBirth = new DateOnly(1990, 5, 15),
            City = "Lisbon",
        };
        _db.PatientProfiles.Add(patientProfile);
        _patientProfileId = patientProfile.Id;

        // Seed reviews with various statuses and doctor responses
        var reviews = new[]
        {
            CreateReview(5, ReviewModerationStatus.Approved, "Thank you!"),     // replied
            CreateReview(4, ReviewModerationStatus.Pending, null),              // awaiting reply
            CreateReview(2, ReviewModerationStatus.Hidden, null),               // hidden, no reply
            CreateReview(1, ReviewModerationStatus.Approved, null),             // approved, no reply (low star)
            CreateReview(3, ReviewModerationStatus.Removed, null),              // removed
        };

        _db.Reviews.AddRange(reviews);
        await _db.SaveChangesAsync();
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    #region Helpers

    private Review CreateReview(
        int rating,
        ReviewModerationStatus status,
        string? doctorResponse)
    {
        var appointmentId = Guid.NewGuid();
        _db.Appointments.Add(new Appointment
        {
            Id = appointmentId,
            ClinicId = TestTenantContext.DefaultClinicId,
            PatientId = _patientProfileId,
            DoctorId = _doctorProfileId,
            ScheduledAt = DateTime.UtcNow.AddDays(-10),
            Reason = "Checkup",
            Status = AppointmentStatus.Completed,
        });

        return new Review
        {
            Id = Guid.NewGuid(),
            ClinicId = TestTenantContext.DefaultClinicId,
            AppointmentId = appointmentId,
            PatientProfileId = _patientProfileId,
            DoctorProfileId = _doctorProfileId,
            Rating = rating,
            Title = $"Review {rating} stars",
            Body = $"Body for {rating} star review",
            Tags = [],
            HelpfulCount = 0,
            ModerationStatus = status,
            DoctorResponse = doctorResponse,
            DoctorRespondedAt = doctorResponse != null ? DateTime.UtcNow : null,
            PostedAs = "Ana C.",
        };
    }

    #endregion

    [TestMethod]
    public async Task Handle_ReturnsAllNonRemovedReviews()
    {
        var query = new GetMyReviewsQuery(_doctorUserId);

        var result = await _handler.Handle(query, CancellationToken.None);

        // Approved (2) + Pending (1) + Hidden (1) = 4, Removed excluded
        result.Reviews.Should().HaveCount(4);
        result.TotalCount.Should().Be(4);
    }

    [TestMethod]
    public async Task Handle_FilterAwaitingReply_ReturnsOnlyUnreplied()
    {
        var query = new GetMyReviewsQuery(_doctorUserId, Filter: "awaiting-reply");

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Reviews.Should().AllSatisfy(r => r.DoctorResponse.Should().BeNull());
        // Pending (1) + Hidden (1) + Approved-no-reply (1) = 3 unreplied non-removed
        result.Reviews.Should().HaveCount(3);
    }

    [TestMethod]
    public async Task Handle_FilterLowStar_ReturnsOnlyLowRated()
    {
        var query = new GetMyReviewsQuery(_doctorUserId, Filter: "low-star");

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Reviews.Should().AllSatisfy(r => r.Rating.Should().BeLessThanOrEqualTo(2));
        // Rating 2 (Hidden) + Rating 1 (Approved) = 2 low-star non-removed
        result.Reviews.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task Handle_DoctorNotFound_ReturnsEmpty()
    {
        var nonexistentDoctorUserId = Guid.NewGuid();
        var query = new GetMyReviewsQuery(nonexistentDoctorUserId);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Reviews.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.ReviewCount.Should().Be(0);
    }
}

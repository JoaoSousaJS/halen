using FluentAssertions;
using Halen.Application.Reviews.Queries;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.Infrastructure.Persistence;
using Halen.UnitTests.Helpers;

namespace Halen.UnitTests.Reviews;

[TestClass]
public class GetDoctorReviewsQueryHandlerTests
{
    private HalenDbContext _db = null!;
    private GetDoctorReviewsQueryHandler _handler = null!;

    private Guid _doctorProfileId;
    private Guid _patientProfileId;

    [TestInitialize]
    public async Task Initialize()
    {
        _db = TestDbFactory.Create();
        _handler = new GetDoctorReviewsQueryHandler(_db);

        // Seed doctor user + profile
        var doctorUser = new User
        {
            Id = Guid.NewGuid(),
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
            UserId = doctorUser.Id,
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

        // Seed 5 reviews with varying ratings, statuses, tags, and helpful counts
        var reviews = new[]
        {
            CreateReview(5, ReviewModerationStatus.Approved, ["listens", "thorough"], 3,
                "Excellent care", "Very thorough", DateTime.UtcNow.AddDays(-1)),
            CreateReview(4, ReviewModerationStatus.Approved, ["on time", "listens"], 1,
                "Good visit", "On time and professional", DateTime.UtcNow.AddDays(-3)),
            CreateReview(1, ReviewModerationStatus.Approved, [], 0,
                "Not great", "Disappointing experience", DateTime.UtcNow.AddDays(-5)),
            CreateReview(3, ReviewModerationStatus.Hidden, ["thorough"], 2,
                "Average", "Nothing special", DateTime.UtcNow.AddDays(-2)),
            CreateReview(2, ReviewModerationStatus.Removed, [], 0,
                "Bad", "Terrible", DateTime.UtcNow.AddDays(-4)),
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
        string[] tags,
        int helpfulCount,
        string title,
        string body,
        DateTime createdAt)
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
            CreatedAt = createdAt,
            ClinicId = TestTenantContext.DefaultClinicId,
            AppointmentId = appointmentId,
            PatientProfileId = _patientProfileId,
            DoctorProfileId = _doctorProfileId,
            Rating = rating,
            Title = title,
            Body = body,
            Tags = tags,
            HelpfulCount = helpfulCount,
            ModerationStatus = status,
            PostedAs = "Ana C.",
        };
    }

    #endregion

    [TestMethod]
    public async Task Handle_ReturnsOnlyApprovedReviews()
    {
        var query = new GetDoctorReviewsQuery(_doctorProfileId);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Reviews.Should().HaveCount(3);
        result.TotalCount.Should().Be(3);
    }

    [TestMethod]
    public async Task Handle_ReturnsPaginatedResults()
    {
        var query = new GetDoctorReviewsQuery(_doctorProfileId, Page: 1, PageSize: 2);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Reviews.Should().HaveCount(2);
        result.TotalCount.Should().Be(3);
    }

    [TestMethod]
    public async Task Handle_SortByNewest_OrdersByCreatedAtDesc()
    {
        var query = new GetDoctorReviewsQuery(_doctorProfileId, SortBy: "newest");

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Reviews.Select(r => r.CreatedAt)
            .Should().BeInDescendingOrder();
    }

    [TestMethod]
    public async Task Handle_SortByHighest_OrdersByRatingDesc()
    {
        var query = new GetDoctorReviewsQuery(_doctorProfileId, SortBy: "highest");

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Reviews.Select(r => r.Rating)
            .Should().BeInDescendingOrder();
    }

    [TestMethod]
    public async Task Handle_SortByHelpful_OrdersByHelpfulCountDesc()
    {
        var query = new GetDoctorReviewsQuery(_doctorProfileId, SortBy: "helpful");

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Reviews.Select(r => r.HelpfulCount)
            .Should().BeInDescendingOrder();
    }

    [TestMethod]
    public async Task Handle_ReturnsRatingBreakdown()
    {
        var query = new GetDoctorReviewsQuery(_doctorProfileId);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.RatingBreakdown.Should().Contain(b => b.Stars == 5 && b.Count == 1);
        result.RatingBreakdown.Should().Contain(b => b.Stars == 4 && b.Count == 1);
        result.RatingBreakdown.Should().Contain(b => b.Stars == 1 && b.Count == 1);
    }

    [TestMethod]
    public async Task Handle_ReturnsTopTags()
    {
        var query = new GetDoctorReviewsQuery(_doctorProfileId);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.TopTags.Should().Contain(t => t.Tag == "listens" && t.Count == 2);
        result.TopTags.Should().Contain(t => t.Tag == "thorough" && t.Count == 1);
        result.TopTags.Should().Contain(t => t.Tag == "on time" && t.Count == 1);
    }

    [TestMethod]
    public async Task Handle_NoDoctorReviews_ReturnsEmptyWithNullAverage()
    {
        var nonexistentDoctorId = Guid.NewGuid();
        var query = new GetDoctorReviewsQuery(nonexistentDoctorId);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Reviews.Should().BeEmpty();
        result.AverageRating.Should().BeNull();
        result.ReviewCount.Should().Be(0);
        result.TotalCount.Should().Be(0);
    }
}

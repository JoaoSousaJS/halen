using FluentAssertions;
using Halen.Application.Doctors.Queries;
using Halen.Domain.Constants;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.Infrastructure.Persistence;
using Halen.UnitTests.Helpers;

namespace Halen.UnitTests.Doctors;

[TestClass]
public class GetDoctorProfileQueryHandlerTests
{
    private HalenDbContext _db = null!;
    private GetDoctorProfileQueryHandler _handler = null!;

    private Guid _doctorProfileId;
    private Guid _clinicId;

    [TestInitialize]
    public async Task Initialize()
    {
        _db = TestDbFactory.Create();
        _handler = new GetDoctorProfileQueryHandler(_db);
        _clinicId = TestTenantContext.DefaultClinicId;

        var doctorUser = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Ana",
            LastName = "Costa",
            Email = "ana.costa@test.com",
            UserName = "ana.costa@test.com",
            Role = UserRole.Doctor,
            ClinicId = _clinicId,
            Status = AccountStatus.Active,
        };
        _db.Users.Add(doctorUser);

        var doctorProfile = new DoctorProfile
        {
            Id = Guid.NewGuid(),
            UserId = doctorUser.Id,
            ClinicId = _clinicId,
            Specialty = "Cardiology",
            LicenseNumber = "LIC-001",
            ConsultationFee = 150,
            YearsOfExperience = 10,
            Languages = ["English", "Portuguese"],
            KycStatus = KycStatus.Approved,
            AverageRating = 4.5m,
            ReviewCount = 3,
        };
        _db.DoctorProfiles.Add(doctorProfile);
        _doctorProfileId = doctorProfile.Id;

        _db.ClinicFeatureFlags.Add(new ClinicFeatureFlag
        {
            Id = Guid.NewGuid(),
            ClinicId = _clinicId,
            FeatureKey = FeatureKeys.DoctorReviews,
            IsEnabled = true,
        });

        var patientUser = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Maria",
            LastName = "Silva",
            Email = "maria.silva@test.com",
            UserName = "maria.silva@test.com",
            Role = UserRole.Patient,
            ClinicId = _clinicId,
        };
        _db.Users.Add(patientUser);

        var patientProfile = new PatientProfile
        {
            Id = Guid.NewGuid(),
            UserId = patientUser.Id,
            ClinicId = _clinicId,
            DateOfBirth = new DateOnly(1990, 5, 15),
            City = "Lisbon",
        };
        _db.PatientProfiles.Add(patientProfile);

        SeedAvailability(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(12, 0));
        SeedAvailability(DayOfWeek.Wednesday, new TimeOnly(14, 0), new TimeOnly(17, 0));
        SeedAvailability(DayOfWeek.Monday, new TimeOnly(14, 0), new TimeOnly(16, 0));
        SeedAvailability(DayOfWeek.Friday, new TimeOnly(9, 0), new TimeOnly(12, 0), isActive: false);

        SeedReview(patientProfile.Id, 5, ReviewModerationStatus.Approved, ["thorough", "kind"],
            3, "Excellent", "Very detailed", DateTime.UtcNow.AddDays(-1));
        SeedReview(patientProfile.Id, 4, ReviewModerationStatus.Approved, ["thorough"],
            1, "Good visit", "Helpful doctor", DateTime.UtcNow.AddDays(-2));
        SeedReview(patientProfile.Id, 3, ReviewModerationStatus.Approved, ["punctual"],
            5, "Average", "OK experience", DateTime.UtcNow.AddDays(-3));
        SeedReview(patientProfile.Id, 2, ReviewModerationStatus.Removed, [],
            0, "Bad", "Rejected review", DateTime.UtcNow.AddDays(-4));
        SeedReview(patientProfile.Id, 1, ReviewModerationStatus.Pending, [],
            0, "Pending", "Pending review", DateTime.UtcNow.AddDays(-5));

        await _db.SaveChangesAsync();
    }

    [TestMethod]
    public async Task Returns_full_profile_for_approved_active_doctor()
    {
        var result = await _handler.Handle(new GetDoctorProfileQuery(_doctorProfileId), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Doctor.Should().NotBeNull();
        result.Doctor!.Name.Should().Be("Ana Costa");
        result.Doctor.Specialty.Should().Be("Cardiology");
        result.Doctor.ConsultationFee.Should().Be(150);
        result.Doctor.YearsOfExperience.Should().Be(10);
        result.Doctor.Languages.Should().BeEquivalentTo(["English", "Portuguese"]);
        result.Doctor.AverageRating.Should().Be(4.5m);
        result.Doctor.ReviewCount.Should().Be(3);
    }

    [TestMethod]
    public async Task Returns_NotFound_for_nonexistent_doctor()
    {
        var result = await _handler.Handle(
            new GetDoctorProfileQuery(Guid.NewGuid()), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(Application.Common.ErrorKind.NotFound);
    }

    [TestMethod]
    public async Task Returns_NotFound_for_unapproved_kyc()
    {
        var user = new User
        {
            Id = Guid.NewGuid(), FirstName = "Bob", LastName = "Test",
            Email = "bob@test.com", UserName = "bob@test.com",
            Role = UserRole.Doctor, ClinicId = _clinicId, Status = AccountStatus.Active,
        };
        _db.Users.Add(user);
        var profile = new DoctorProfile
        {
            Id = Guid.NewGuid(), UserId = user.Id, ClinicId = _clinicId,
            Specialty = "Dermatology", LicenseNumber = "LIC-002",
            ConsultationFee = 100, YearsOfExperience = 5,
            KycStatus = KycStatus.Submitted,
        };
        _db.DoctorProfiles.Add(profile);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetDoctorProfileQuery(profile.Id), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(Application.Common.ErrorKind.NotFound);
    }

    [TestMethod]
    public async Task Returns_NotFound_for_inactive_account()
    {
        var user = new User
        {
            Id = Guid.NewGuid(), FirstName = "Sam", LastName = "Inactive",
            Email = "sam@test.com", UserName = "sam@test.com",
            Role = UserRole.Doctor, ClinicId = _clinicId, Status = AccountStatus.Suspended,
        };
        _db.Users.Add(user);
        var profile = new DoctorProfile
        {
            Id = Guid.NewGuid(), UserId = user.Id, ClinicId = _clinicId,
            Specialty = "Pediatrics", LicenseNumber = "LIC-003",
            ConsultationFee = 120, YearsOfExperience = 8,
            KycStatus = KycStatus.Approved,
        };
        _db.DoctorProfiles.Add(profile);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetDoctorProfileQuery(profile.Id), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(Application.Common.ErrorKind.NotFound);
    }

    [TestMethod]
    public async Task Returns_availability_grouped_by_day_only_active_windows()
    {
        var result = await _handler.Handle(new GetDoctorProfileQuery(_doctorProfileId), CancellationToken.None);

        result.Availability.Should().HaveCount(2);
        result.Availability![0].DayOfWeek.Should().Be("Monday");
        result.Availability[0].Windows.Should().HaveCount(2);
        result.Availability[1].DayOfWeek.Should().Be("Wednesday");
        result.Availability[1].Windows.Should().HaveCount(1);
    }

    [TestMethod]
    public async Task Availability_ordered_monday_first()
    {
        SeedAvailability(DayOfWeek.Sunday, new TimeOnly(10, 0), new TimeOnly(12, 0));
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetDoctorProfileQuery(_doctorProfileId), CancellationToken.None);

        var days = result.Availability!.Select(a => a.DayOfWeek).ToList();
        days.Should().BeEquivalentTo(["Monday", "Wednesday", "Sunday"], o => o.WithStrictOrdering());
    }

    [TestMethod]
    public async Task Returns_only_approved_reviews()
    {
        var result = await _handler.Handle(new GetDoctorProfileQuery(_doctorProfileId), CancellationToken.None);

        result.Reviews.Should().HaveCount(3);
        result.ReviewTotalCount.Should().Be(3);
    }

    [TestMethod]
    public async Task Reviews_sorted_by_newest_by_default()
    {
        var result = await _handler.Handle(new GetDoctorProfileQuery(_doctorProfileId), CancellationToken.None);

        result.Reviews![0].Title.Should().Be("Excellent");
        result.Reviews[1].Title.Should().Be("Good visit");
        result.Reviews[2].Title.Should().Be("Average");
    }

    [TestMethod]
    public async Task Reviews_sorted_by_highest_rating()
    {
        var result = await _handler.Handle(
            new GetDoctorProfileQuery(_doctorProfileId, ReviewSortBy: "highest"), CancellationToken.None);

        result.Reviews![0].Rating.Should().Be(5);
        result.Reviews[1].Rating.Should().Be(4);
        result.Reviews[2].Rating.Should().Be(3);
    }

    [TestMethod]
    public async Task Reviews_sorted_by_helpful_count()
    {
        var result = await _handler.Handle(
            new GetDoctorProfileQuery(_doctorProfileId, ReviewSortBy: "helpful"), CancellationToken.None);

        result.Reviews![0].HelpfulCount.Should().Be(5);
        result.Reviews[1].HelpfulCount.Should().Be(3);
        result.Reviews[2].HelpfulCount.Should().Be(1);
    }

    [TestMethod]
    public async Task Paginates_reviews_correctly()
    {
        var page1 = await _handler.Handle(
            new GetDoctorProfileQuery(_doctorProfileId, ReviewPage: 1, ReviewPageSize: 2), CancellationToken.None);

        page1.Reviews.Should().HaveCount(2);
        page1.ReviewTotalCount.Should().Be(3);

        var page2 = await _handler.Handle(
            new GetDoctorProfileQuery(_doctorProfileId, ReviewPage: 2, ReviewPageSize: 2), CancellationToken.None);

        page2.Reviews.Should().HaveCount(1);
        page2.ReviewTotalCount.Should().Be(3);
    }

    [TestMethod]
    public async Task Out_of_range_page_returns_empty_reviews()
    {
        var result = await _handler.Handle(
            new GetDoctorProfileQuery(_doctorProfileId, ReviewPage: 100), CancellationToken.None);

        result.Reviews.Should().BeEmpty();
        result.ReviewTotalCount.Should().Be(3);
    }

    [TestMethod]
    public async Task Rating_breakdown_fills_all_five_stars()
    {
        var result = await _handler.Handle(new GetDoctorProfileQuery(_doctorProfileId), CancellationToken.None);

        result.ReviewsSummary!.RatingBreakdown.Should().HaveCount(5);
        result.ReviewsSummary.RatingBreakdown.Select(r => r.Stars).Should()
            .BeEquivalentTo([1, 2, 3, 4, 5], o => o.WithStrictOrdering());
        result.ReviewsSummary.RatingBreakdown.First(r => r.Stars == 1).Count.Should().Be(0);
        result.ReviewsSummary.RatingBreakdown.First(r => r.Stars == 2).Count.Should().Be(0);
    }

    [TestMethod]
    public async Task Returns_top_tags()
    {
        var result = await _handler.Handle(new GetDoctorProfileQuery(_doctorProfileId), CancellationToken.None);

        result.ReviewsSummary!.TopTags.Should().NotBeEmpty();
        result.ReviewsSummary.TopTags[0].Tag.Should().Be("thorough");
        result.ReviewsSummary.TopTags[0].Count.Should().Be(2);
    }

    [TestMethod]
    public async Task Empty_reviews_when_doctor_has_no_reviews()
    {
        var user = new User
        {
            Id = Guid.NewGuid(), FirstName = "New", LastName = "Doctor",
            Email = "new@test.com", UserName = "new@test.com",
            Role = UserRole.Doctor, ClinicId = _clinicId, Status = AccountStatus.Active,
        };
        _db.Users.Add(user);
        var profile = new DoctorProfile
        {
            Id = Guid.NewGuid(), UserId = user.Id, ClinicId = _clinicId,
            Specialty = "GP", LicenseNumber = "LIC-NEW",
            ConsultationFee = 80, YearsOfExperience = 2,
            KycStatus = KycStatus.Approved,
        };
        _db.DoctorProfiles.Add(profile);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetDoctorProfileQuery(profile.Id), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Reviews.Should().BeEmpty();
        result.ReviewTotalCount.Should().Be(0);
        result.ReviewsSummary!.RatingBreakdown.Should().HaveCount(5);
        result.ReviewsSummary.RatingBreakdown.Should().OnlyContain(r => r.Count == 0);
    }

    [TestMethod]
    public async Task Empty_availability_when_doctor_has_no_active_windows()
    {
        var user = new User
        {
            Id = Guid.NewGuid(), FirstName = "No", LastName = "Avail",
            Email = "noavail@test.com", UserName = "noavail@test.com",
            Role = UserRole.Doctor, ClinicId = _clinicId, Status = AccountStatus.Active,
        };
        _db.Users.Add(user);
        var profile = new DoctorProfile
        {
            Id = Guid.NewGuid(), UserId = user.Id, ClinicId = _clinicId,
            Specialty = "ENT", LicenseNumber = "LIC-NA",
            ConsultationFee = 100, YearsOfExperience = 5,
            KycStatus = KycStatus.Approved,
        };
        _db.DoctorProfiles.Add(profile);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetDoctorProfileQuery(profile.Id), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Availability.Should().BeEmpty();
    }

    [TestMethod]
    public async Task Omits_reviews_when_feature_flag_disabled()
    {
        var flag = _db.ClinicFeatureFlags
            .First(f => f.ClinicId == _clinicId && f.FeatureKey == FeatureKeys.DoctorReviews);
        flag.IsEnabled = false;
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetDoctorProfileQuery(_doctorProfileId), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Doctor.Should().NotBeNull();
        result.ReviewsSummary.Should().BeNull();
        result.Reviews.Should().BeEmpty();
        result.ReviewTotalCount.Should().Be(0);
    }

    private void SeedAvailability(DayOfWeek day, TimeOnly start, TimeOnly end, bool isActive = true)
    {
        _db.DoctorAvailabilities.Add(new DoctorAvailability
        {
            Id = Guid.NewGuid(),
            DoctorProfileId = _doctorProfileId,
            ClinicId = _clinicId,
            DayOfWeek = day,
            StartTime = start,
            EndTime = end,
            SlotDurationMinutes = 20,
            IsActive = isActive,
        });
    }

    private void SeedReview(Guid patientProfileId, int rating, ReviewModerationStatus status,
        string[] tags, int helpfulCount, string title, string body, DateTime createdAt)
    {
        _db.Reviews.Add(new Review
        {
            Id = Guid.NewGuid(),
            ClinicId = _clinicId,
            AppointmentId = Guid.NewGuid(),
            PatientProfileId = patientProfileId,
            DoctorProfileId = _doctorProfileId,
            Rating = rating,
            Title = title,
            Body = body,
            Tags = tags,
            PostedAs = "Anonymous",
            HelpfulCount = helpfulCount,
            ModerationStatus = status,
            CreatedAt = createdAt,
        });
    }
}

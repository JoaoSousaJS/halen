using FluentAssertions;
using Halen.Application.Reviews.Queries;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.Infrastructure.Persistence;
using Halen.UnitTests.Helpers;

namespace Halen.UnitTests.Reviews;

[TestClass]
public class GetModerationQueueQueryHandlerTests
{
    private HalenDbContext _db = null!;
    private GetModerationQueueQueryHandler _handler = null!;

    [TestInitialize]
    public async Task Initialize()
    {
        _db = TestDbFactory.Create();
        _handler = new GetModerationQueueQueryHandler(_db);

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

        // Seed reviews with different moderation statuses
        var statuses = new[]
        {
            ReviewModerationStatus.Pending,
            ReviewModerationStatus.Pending,
            ReviewModerationStatus.Approved,
            ReviewModerationStatus.Hidden,
            ReviewModerationStatus.Removed,
        };

        foreach (var status in statuses)
        {
            var appointmentId = Guid.NewGuid();
            _db.Appointments.Add(new Appointment
            {
                Id = appointmentId,
                ClinicId = TestTenantContext.DefaultClinicId,
                PatientId = patientProfile.Id,
                DoctorId = doctorProfile.Id,
                ScheduledAt = DateTime.UtcNow.AddDays(-10),
                Reason = "Checkup",
                Status = AppointmentStatus.Completed,
            });

            _db.Reviews.Add(new Review
            {
                Id = Guid.NewGuid(),
                ClinicId = TestTenantContext.DefaultClinicId,
                AppointmentId = appointmentId,
                PatientProfileId = patientProfile.Id,
                DoctorProfileId = doctorProfile.Id,
                Rating = 4,
                Title = $"Review ({status})",
                Body = "Review body",
                Tags = [],
                HelpfulCount = 0,
                ModerationStatus = status,
                PostedAs = "Ana C.",
            });
        }

        await _db.SaveChangesAsync();
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_FilterPending_ReturnsOnlyPendingReviews()
    {
        var query = new GetModerationQueueQuery(Filter: "pending");

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Reviews.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Reviews.Should().AllSatisfy(r =>
            r.ModerationStatus.Should().Be(nameof(ReviewModerationStatus.Pending)));
    }

    [TestMethod]
    public async Task Handle_FilterAll_ReturnsNonRemovedReviews()
    {
        var query = new GetModerationQueueQuery(Filter: "all");

        var result = await _handler.Handle(query, CancellationToken.None);

        // Pending (2) + Approved (1) + Hidden (1) = 4, Removed excluded
        result.Reviews.Should().HaveCount(4);
        result.TotalCount.Should().Be(4);
    }

    [TestMethod]
    public async Task Handle_IncludesPatientAndDoctorNames()
    {
        var query = new GetModerationQueueQuery(Filter: "pending");

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Reviews.Should().AllSatisfy(r =>
        {
            r.PatientName.Should().Be("Ana Costa");
            r.DoctorName.Should().Be("Dr. Mendes");
        });
    }
}

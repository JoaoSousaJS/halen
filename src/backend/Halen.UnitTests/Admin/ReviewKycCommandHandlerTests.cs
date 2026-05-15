using FluentAssertions;
using Halen.Application.Admin.Commands;
using Halen.Application.Common;
using Halen.Application.Events;
using Halen.Application.Interfaces;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;

namespace Halen.UnitTests.Admin;

[TestClass]
public class ReviewKycCommandHandlerTests
{
    private HalenDbContext _db = null!;
    private Mock<IEventBus> _eventBus = null!;
    private ReviewKycCommandHandler _handler = null!;
    private Guid _adminUserId;
    private Guid _doctorProfileId;
    private Guid _doctorUserId;

    [TestInitialize]
    public async Task Initialize()
    {
        var options = new DbContextOptionsBuilder<HalenDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new HalenDbContext(options);
        _adminUserId = Guid.NewGuid();
        _doctorUserId = Guid.NewGuid();
        _doctorProfileId = Guid.NewGuid();

        _db.Users.AddRange(
            new User
            {
                Id = _adminUserId, FirstName = "Admin", LastName = "User",
                Email = "admin@test.com", UserName = "admin@test.com", Role = UserRole.Admin,
            },
            new User
            {
                Id = _doctorUserId, FirstName = "Dr", LastName = "Test",
                Email = "doc@test.com", UserName = "doc@test.com", Role = UserRole.Doctor,
                Status = AccountStatus.PendingReview,
            }
        );

        _db.DoctorProfiles.Add(new DoctorProfile
        {
            Id = _doctorProfileId, UserId = _doctorUserId,
            Specialty = "General", LicenseNumber = "LIC-001",
            ConsultationFee = 100, YearsOfExperience = 5,
            KycStatus = KycStatus.Submitted,
        });

        await _db.SaveChangesAsync();

        _eventBus = new Mock<IEventBus>();
        _handler = new ReviewKycCommandHandler(
            _db, _eventBus.Object, Mock.Of<ILogger<ReviewKycCommandHandler>>());
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_Approve_SetsApprovedAndActivatesUser()
    {
        var command = new ReviewKycCommand(_adminUserId, _doctorProfileId, KycDecision.Approved, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();

        var doctor = await _db.DoctorProfiles.Include(d => d.User).FirstAsync(d => d.Id == _doctorProfileId);
        doctor.KycStatus.Should().Be(KycStatus.Approved);
        doctor.User.Status.Should().Be(AccountStatus.Active);

        var reviews = await _db.KycReviews.Where(r => r.DoctorProfileId == _doctorProfileId).ToListAsync();
        reviews.Should().HaveCount(1);
        reviews[0].Decision.Should().Be(KycDecision.Approved);

        _eventBus.Verify(e => e.PublishAsync(
            Topics.KycReviewed,
            It.Is<KycReviewedEvent>(evt =>
                evt.DoctorProfileId == _doctorProfileId &&
                evt.Decision == KycDecision.Approved),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_Reject_SetsRejectedAndKeepsPendingReview()
    {
        var command = new ReviewKycCommand(_adminUserId, _doctorProfileId, KycDecision.Rejected, "Blurry photo");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();

        var doctor = await _db.DoctorProfiles.Include(d => d.User).FirstAsync(d => d.Id == _doctorProfileId);
        doctor.KycStatus.Should().Be(KycStatus.Rejected);
        doctor.User.Status.Should().Be(AccountStatus.PendingReview);

        var reviews = await _db.KycReviews.Where(r => r.DoctorProfileId == _doctorProfileId).ToListAsync();
        reviews.Should().HaveCount(1);
        reviews[0].Decision.Should().Be(KycDecision.Rejected);
        reviews[0].RejectionReason.Should().Be("Blurry photo");
    }

    [TestMethod]
    public async Task Handle_DoctorNotSubmitted_ReturnsValidationError()
    {
        var doctor = await _db.DoctorProfiles.FirstAsync(d => d.Id == _doctorProfileId);
        doctor.KycStatus = KycStatus.NotSubmitted;
        await _db.SaveChangesAsync();

        var command = new ReviewKycCommand(_adminUserId, _doctorProfileId, KycDecision.Approved, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Validation);
    }

    [TestMethod]
    public async Task Handle_DoctorNotFound_ReturnsNotFound()
    {
        var command = new ReviewKycCommand(_adminUserId, Guid.NewGuid(), KycDecision.Approved, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.NotFound);
    }
}

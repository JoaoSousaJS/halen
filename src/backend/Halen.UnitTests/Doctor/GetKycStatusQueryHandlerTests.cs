using FluentAssertions;
using Halen.Application.Doctor.Queries;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.Infrastructure.Persistence;
using Halen.UnitTests.Helpers;

namespace Halen.UnitTests.Doctor;

[TestClass]
public class GetKycStatusQueryHandlerTests
{
    private HalenDbContext _db = null!;
    private GetKycStatusQueryHandler _handler = null!;

    [TestInitialize]
    public void Initialize()
    {
        _db = TestDbFactory.Create();
        _handler = new GetKycStatusQueryHandler(_db);
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_DoctorNotFound_ReturnsNotSubmittedDefault()
    {
        var query = new GetKycStatusQuery(Guid.NewGuid());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Status.Should().Be(KycStatus.NotSubmitted);
        result.SubmittedAt.Should().BeNull();
        result.LastRejectionReason.Should().BeNull();
        result.Documents.Should().BeEmpty();
    }

    [TestMethod]
    public async Task Handle_SubmittedDoctor_ReturnsStatusAndDocuments()
    {
        var userId = Guid.NewGuid();
        var doctorProfileId = Guid.NewGuid();
        var submittedAt = DateTime.UtcNow.AddDays(-1);

        _db.Users.Add(new User
        {
            Id = userId, FirstName = "Dr", LastName = "Test",
            Email = "doc@test.com", UserName = "doc@test.com", Role = UserRole.Doctor,
        });
        _db.DoctorProfiles.Add(new DoctorProfile
        {
            Id = doctorProfileId, UserId = userId,
            Specialty = "General", LicenseNumber = "LIC-001",
            ConsultationFee = 100, YearsOfExperience = 5,
            KycStatus = KycStatus.Submitted, KycSubmittedAt = submittedAt,
        });
        _db.KycDocuments.AddRange(
            new KycDocument
            {
                DoctorProfileId = doctorProfileId, DocumentType = KycDocumentType.LicensePhoto,
                FileName = "license.jpg", FilePath = "/uploads/license.jpg",
                ContentType = "image/jpeg", FileSizeBytes = 1024,
                UploadedAt = submittedAt,
            },
            new KycDocument
            {
                DoctorProfileId = doctorProfileId, DocumentType = KycDocumentType.IdentityProof,
                FileName = "id.png", FilePath = "/uploads/id.png",
                ContentType = "image/png", FileSizeBytes = 512,
                UploadedAt = submittedAt,
            }
        );
        await _db.SaveChangesAsync();

        var query = new GetKycStatusQuery(userId);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Status.Should().Be(KycStatus.Submitted);
        result.SubmittedAt.Should().Be(submittedAt);
        result.LastRejectionReason.Should().BeNull();
        result.Documents.Should().HaveCount(2);
        result.Documents.Should().Contain(d => d.FileName == "license.jpg");
        result.Documents.Should().Contain(d => d.FileName == "id.png");
    }

    [TestMethod]
    public async Task Handle_RejectedDoctor_IncludesRejectionReason()
    {
        var userId = Guid.NewGuid();
        var doctorProfileId = Guid.NewGuid();
        var reviewerUserId = Guid.NewGuid();

        _db.Users.AddRange(
            new User
            {
                Id = userId, FirstName = "Dr", LastName = "Rejected",
                Email = "rejected@test.com", UserName = "rejected@test.com", Role = UserRole.Doctor,
            },
            new User
            {
                Id = reviewerUserId, FirstName = "Admin", LastName = "Reviewer",
                Email = "admin@test.com", UserName = "admin@test.com", Role = UserRole.PlatformAdmin,
            }
        );
        _db.DoctorProfiles.Add(new DoctorProfile
        {
            Id = doctorProfileId, UserId = userId,
            Specialty = "General", LicenseNumber = "LIC-002",
            ConsultationFee = 100, YearsOfExperience = 3,
            KycStatus = KycStatus.Rejected, KycSubmittedAt = DateTime.UtcNow.AddDays(-3),
        });
        _db.KycReviews.Add(new KycReview
        {
            DoctorProfileId = doctorProfileId, ReviewedByUserId = reviewerUserId,
            Decision = KycDecision.Rejected, RejectionReason = "Blurry license photo",
            ReviewedAt = DateTime.UtcNow.AddDays(-1),
        });
        await _db.SaveChangesAsync();

        var query = new GetKycStatusQuery(userId);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Status.Should().Be(KycStatus.Rejected);
        result.LastRejectionReason.Should().Be("Blurry license photo");
    }
}

using Halen.UnitTests.Helpers;
using FluentAssertions;
using Halen.Application.Common;
using Halen.Application.Doctor.Commands;
using Halen.Application.Events;
using Halen.Application.Interfaces;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;

namespace Halen.UnitTests.Doctor;

[TestClass]
public class SubmitKycDocumentsCommandHandlerTests
{
    private HalenDbContext _db = null!;
    private Mock<IEventBus> _eventBus = null!;
    private SubmitKycDocumentsCommandHandler _handler = null!;
    private Guid _doctorUserId;

    [TestInitialize]
    public async Task Initialize()
    {
        var options = new DbContextOptionsBuilder<HalenDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new HalenDbContext(options, new TestTenantContext());
        _doctorUserId = Guid.NewGuid();

        _db.Users.Add(new User
        {
            Id = _doctorUserId, FirstName = "Dr", LastName = "Test",
            Email = "doc@test.com", UserName = "doc@test.com", Role = UserRole.Doctor,
            Status = AccountStatus.PendingReview,
        });

        _db.DoctorProfiles.Add(new DoctorProfile
        {
            UserId = _doctorUserId,
            Specialty = "General", LicenseNumber = "LIC-001",
            ConsultationFee = 100, YearsOfExperience = 5,
            KycStatus = KycStatus.NotSubmitted,
        });

        await _db.SaveChangesAsync();

        _eventBus = new Mock<IEventBus>();
        _handler = new SubmitKycDocumentsCommandHandler(
            _db, new Helpers.TestTenantContext(), _eventBus.Object, Mock.Of<ILogger<SubmitKycDocumentsCommandHandler>>());
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    private static List<KycDocumentUpload> ValidDocuments() =>
    [
        new(KycDocumentType.LicensePhoto, "license.jpg", "image/jpeg", 1024, "/uploads/license.jpg"),
        new(KycDocumentType.MedicalCertificate, "cert.pdf", "application/pdf", 2048, "/uploads/cert.pdf"),
        new(KycDocumentType.IdentityProof, "id.png", "image/png", 512, "/uploads/id.png"),
    ];

    [TestMethod]
    public async Task Handle_ValidSubmission_SetsStatusAndPublishesEvent()
    {
        var command = new SubmitKycDocumentsCommand(_doctorUserId, ValidDocuments());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();

        var doctor = await _db.DoctorProfiles.FirstAsync(d => d.UserId == _doctorUserId);
        doctor.KycStatus.Should().Be(KycStatus.Submitted);
        doctor.KycSubmittedAt.Should().NotBeNull();

        var docs = await _db.KycDocuments.Where(d => d.DoctorProfileId == doctor.Id).ToListAsync();
        docs.Should().HaveCount(3);

        _eventBus.Verify(e => e.PublishAsync(
            Topics.KycSubmitted,
            It.IsAny<KycDocumentsSubmittedEvent>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_RejectedDoctor_CanResubmit()
    {
        var doctor = await _db.DoctorProfiles.FirstAsync(d => d.UserId == _doctorUserId);
        doctor.KycStatus = KycStatus.Rejected;
        await _db.SaveChangesAsync();

        var command = new SubmitKycDocumentsCommand(_doctorUserId, ValidDocuments());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        var updated = await _db.DoctorProfiles.FirstAsync(d => d.UserId == _doctorUserId);
        updated.KycStatus.Should().Be(KycStatus.Submitted);
    }

    [TestMethod]
    public async Task Handle_AlreadySubmitted_ReturnsValidationError()
    {
        var doctor = await _db.DoctorProfiles.FirstAsync(d => d.UserId == _doctorUserId);
        doctor.KycStatus = KycStatus.Submitted;
        await _db.SaveChangesAsync();

        var command = new SubmitKycDocumentsCommand(_doctorUserId, ValidDocuments());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Validation);
        result.Error.Should().Contain("already submitted");
    }

    [TestMethod]
    public async Task Handle_AlreadyApproved_ReturnsValidationError()
    {
        var doctor = await _db.DoctorProfiles.FirstAsync(d => d.UserId == _doctorUserId);
        doctor.KycStatus = KycStatus.Approved;
        await _db.SaveChangesAsync();

        var command = new SubmitKycDocumentsCommand(_doctorUserId, ValidDocuments());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Validation);
    }

    [TestMethod]
    public async Task Handle_DoctorNotFound_ReturnsNotFound()
    {
        var command = new SubmitKycDocumentsCommand(Guid.NewGuid(), ValidDocuments());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.NotFound);
    }

    [TestMethod]
    public async Task Handle_MissingDocumentType_ReturnsValidationError()
    {
        var docs = new List<KycDocumentUpload>
        {
            new(KycDocumentType.LicensePhoto, "license.jpg", "image/jpeg", 1024, "/uploads/license.jpg"),
            new(KycDocumentType.IdentityProof, "id.png", "image/png", 512, "/uploads/id.png"),
        };

        var command = new SubmitKycDocumentsCommand(_doctorUserId, docs);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Validation);
        result.Error.Should().Contain("MedicalCertificate");
    }
}

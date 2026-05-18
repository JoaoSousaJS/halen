using FluentAssertions;
using Halen.Application.Common;
using Halen.Application.Interfaces;
using Halen.Application.MedicalRecords.Commands;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.Infrastructure.Persistence;
using Halen.UnitTests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Halen.UnitTests.MedicalRecords;

[TestClass]
public class DeleteMedicalDocumentCommandHandlerTests
{
    private HalenDbContext _db = null!;
    private Mock<IFileStorage> _fileStorage = null!;
    private DeleteMedicalDocumentCommandHandler _handler = null!;
    private Guid _uploaderUserId;
    private Guid _documentId;

    [TestInitialize]
    public async Task Initialize()
    {
        _db = TestDbFactory.Create();

        _uploaderUserId = Guid.NewGuid();
        _documentId = Guid.NewGuid();
        var patientProfileId = Guid.NewGuid();
        var patientUserId = Guid.NewGuid();

        _db.Users.AddRange(
            new User { Id = _uploaderUserId, FirstName = "Dr", LastName = "House", Email = "doc@test.com", UserName = "doc@test.com", Role = UserRole.Doctor },
            new User { Id = patientUserId, FirstName = "Pat", LastName = "Ient", Email = "pat@test.com", UserName = "pat@test.com", Role = UserRole.Patient }
        );

        _db.PatientProfiles.Add(new PatientProfile { Id = patientProfileId, UserId = patientUserId });

        _db.MedicalDocuments.Add(new MedicalDocument
        {
            Id = _documentId,
            PatientProfileId = patientProfileId,
            DocumentType = MedicalDocumentType.LabResult,
            Title = "Blood Test",
            FileName = "blood-test.pdf",
            FilePath = "medical-documents/test/blood-test.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 1024,
            UploadedByUserId = _uploaderUserId,
        });

        await _db.SaveChangesAsync();

        _fileStorage = new Mock<IFileStorage>();
        _handler = new DeleteMedicalDocumentCommandHandler(
            _db, _fileStorage.Object,
            Mock.Of<ILogger<DeleteMedicalDocumentCommandHandler>>());
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_UploaderDeletes_Success()
    {
        var command = new DeleteMedicalDocumentCommand(
            _uploaderUserId, UserRole.Doctor, _documentId);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();

        var doc = await _db.MedicalDocuments.FindAsync(_documentId);
        doc.Should().BeNull();

        _fileStorage.Verify(x => x.DeleteAsync(
            "medical-documents/test/blood-test.pdf",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_DocumentNotFound_ReturnsNotFound()
    {
        var command = new DeleteMedicalDocumentCommand(
            _uploaderUserId, UserRole.Doctor, Guid.NewGuid());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.NotFound);
        result.Error.Should().Contain("not found");
    }

    [TestMethod]
    public async Task Handle_NotUploaderAndNotAdmin_ReturnsForbidden()
    {
        var otherUserId = Guid.NewGuid();
        _db.Users.Add(new User { Id = otherUserId, FirstName = "Other", LastName = "User", Email = "other@test.com", UserName = "other@test.com", Role = UserRole.Doctor });
        await _db.SaveChangesAsync();

        var command = new DeleteMedicalDocumentCommand(
            otherUserId, UserRole.Doctor, _documentId);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Forbidden);
        result.Error.Should().Contain("uploader");

        _fileStorage.Verify(x => x.DeleteAsync(
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_PlatformAdminCanDelete_Success()
    {
        var adminUserId = Guid.NewGuid();
        _db.Users.Add(new User { Id = adminUserId, FirstName = "Admin", LastName = "User", Email = "admin@test.com", UserName = "admin@test.com", Role = UserRole.PlatformAdmin });
        await _db.SaveChangesAsync();

        var command = new DeleteMedicalDocumentCommand(
            adminUserId, UserRole.PlatformAdmin, _documentId);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();

        var doc = await _db.MedicalDocuments.FindAsync(_documentId);
        doc.Should().BeNull();

        _fileStorage.Verify(x => x.DeleteAsync(
            "medical-documents/test/blood-test.pdf",
            It.IsAny<CancellationToken>()), Times.Once);
    }
}

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
public class UploadMedicalDocumentCommandHandlerTests
{
    private HalenDbContext _db = null!;
    private Mock<IRecordAccessChecker> _accessChecker = null!;
    private Mock<IFileStorage> _fileStorage = null!;
    private UploadMedicalDocumentCommandHandler _handler = null!;
    private Guid _doctorUserId;
    private Guid _patientProfileId;

    [TestInitialize]
    public async Task Initialize()
    {
        _db = TestDbFactory.Create();

        _doctorUserId = Guid.NewGuid();
        _patientProfileId = Guid.NewGuid();
        var patientUserId = Guid.NewGuid();

        _db.Users.AddRange(
            new User { Id = _doctorUserId, FirstName = "Dr", LastName = "House", Email = "doc@test.com", UserName = "doc@test.com", Role = UserRole.Doctor },
            new User { Id = patientUserId, FirstName = "Pat", LastName = "Ient", Email = "pat@test.com", UserName = "pat@test.com", Role = UserRole.Patient }
        );

        _db.PatientProfiles.Add(new PatientProfile { Id = _patientProfileId, UserId = patientUserId });
        await _db.SaveChangesAsync();

        _accessChecker = new Mock<IRecordAccessChecker>();
        _accessChecker
            .Setup(x => x.CanAccessPatientRecord(It.IsAny<Guid>(), It.IsAny<UserRole>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _fileStorage = new Mock<IFileStorage>();
        _fileStorage
            .Setup(x => x.SaveAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("medical-documents/test/blood-test.pdf");

        _handler = new UploadMedicalDocumentCommandHandler(
            _db, new TestTenantContext(), _accessChecker.Object, _fileStorage.Object,
            Mock.Of<ILogger<UploadMedicalDocumentCommandHandler>>());
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_ValidCommand_CreatesDocumentAndSavesFile()
    {
        var command = new UploadMedicalDocumentCommand(
            _doctorUserId, UserRole.Doctor, _patientProfileId,
            MedicalDocumentType.LabResult, "Blood Test", "Annual checkup",
            "blood-test.pdf", "application/pdf", 1024, Stream.Null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.DocumentId.Should().NotBeNull();

        var doc = await _db.MedicalDocuments.FindAsync(result.DocumentId);
        doc.Should().NotBeNull();
        doc!.Title.Should().Be("Blood Test");
        doc.FileName.Should().Be("blood-test.pdf");
        doc.ContentType.Should().Be("application/pdf");
        doc.FileSizeBytes.Should().Be(1024);
        doc.UploadedByUserId.Should().Be(_doctorUserId);
        doc.FilePath.Should().Be("medical-documents/test/blood-test.pdf");

        _fileStorage.Verify(x => x.SaveAsync(
            It.IsAny<string>(), "blood-test.pdf", Stream.Null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_AccessDenied_ReturnsForbidden()
    {
        _accessChecker
            .Setup(x => x.CanAccessPatientRecord(It.IsAny<Guid>(), It.IsAny<UserRole>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new UploadMedicalDocumentCommand(
            _doctorUserId, UserRole.Doctor, _patientProfileId,
            MedicalDocumentType.LabResult, "Blood Test", null,
            "blood-test.pdf", "application/pdf", 1024, Stream.Null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Forbidden);

        _fileStorage.Verify(x => x.SaveAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_InvalidContentType_ReturnsValidationError()
    {
        var command = new UploadMedicalDocumentCommand(
            _doctorUserId, UserRole.Doctor, _patientProfileId,
            MedicalDocumentType.LabResult, "Blood Test", null,
            "document.txt", "text/plain", 1024, Stream.Null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Validation);
        result.Error.Should().Contain("content type");

        _fileStorage.Verify(x => x.SaveAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_FileTooLarge_ReturnsValidationError()
    {
        var command = new UploadMedicalDocumentCommand(
            _doctorUserId, UserRole.Doctor, _patientProfileId,
            MedicalDocumentType.LabResult, "Blood Test", null,
            "large-file.pdf", "application/pdf", 11 * 1024 * 1024, Stream.Null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Validation);
        result.Error.Should().Contain("10 MB");

        _fileStorage.Verify(x => x.SaveAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}

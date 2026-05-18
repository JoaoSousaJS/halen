using Halen.UnitTests.Helpers;
using FluentAssertions;
using Halen.Application.Common;
using Halen.Application.Interfaces;
using Halen.Application.MedicalRecords.Queries;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.Infrastructure.Persistence;
using Moq;

namespace Halen.UnitTests.MedicalRecords;

[TestClass]
public class DownloadMedicalDocumentQueryHandlerTests
{
    private HalenDbContext _db = null!;
    private Mock<IRecordAccessChecker> _accessChecker = null!;
    private Mock<IFileStorage> _fileStorage = null!;
    private DownloadMedicalDocumentQueryHandler _handler = null!;
    private Guid _callerUserId;
    private Guid _patientProfileId;
    private Guid _documentId;

    [TestInitialize]
    public async Task Initialize()
    {
        _db = TestDbFactory.Create();
        _accessChecker = new Mock<IRecordAccessChecker>();
        _fileStorage = new Mock<IFileStorage>();
        _callerUserId = Guid.NewGuid();
        _patientProfileId = Guid.NewGuid();
        _documentId = Guid.NewGuid();

        var patientUserId = Guid.NewGuid();
        var uploadedByUserId = Guid.NewGuid();

        _db.Users.AddRange(
            new User { Id = patientUserId, FirstName = "Jane", LastName = "Doe", Email = "jane@test.com", UserName = "jane@test.com", Role = UserRole.Patient },
            new User { Id = uploadedByUserId, FirstName = "Dr", LastName = "Smith", Email = "dr@test.com", UserName = "dr@test.com", Role = UserRole.Doctor }
        );

        _db.PatientProfiles.Add(new PatientProfile { Id = _patientProfileId, UserId = patientUserId });

        _db.MedicalDocuments.Add(new MedicalDocument
        {
            Id = _documentId,
            PatientProfileId = _patientProfileId,
            DocumentType = MedicalDocumentType.LabResult,
            Title = "Blood Work",
            FileName = "bloodwork.pdf",
            FilePath = "/docs/bloodwork.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 102400,
            UploadedByUserId = uploadedByUserId
        });

        await _db.SaveChangesAsync();

        _accessChecker.Setup(x => x.CanAccessPatientRecord(
            It.IsAny<Guid>(), It.IsAny<UserRole>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var fakeStream = new MemoryStream(new byte[] { 1, 2, 3 });
        _fileStorage.Setup(x => x.ReadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeStream);

        _handler = new DownloadMedicalDocumentQueryHandler(_db, _accessChecker.Object, _fileStorage.Object);
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_Success_ReturnsFileStream()
    {
        var query = new DownloadMedicalDocumentQuery(_callerUserId, UserRole.Doctor, _documentId);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.FileStream.Should().NotBeNull();
        result.FileName.Should().Be("bloodwork.pdf");
        result.ContentType.Should().Be("application/pdf");

        _fileStorage.Verify(x => x.ReadAsync("/docs/bloodwork.pdf", It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_DocumentNotFound_ReturnsNotFound()
    {
        var query = new DownloadMedicalDocumentQuery(_callerUserId, UserRole.Doctor, Guid.NewGuid());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.NotFound);
    }

    [TestMethod]
    public async Task Handle_AccessDenied_ReturnsForbidden()
    {
        _accessChecker.Setup(x => x.CanAccessPatientRecord(
            It.IsAny<Guid>(), It.IsAny<UserRole>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var query = new DownloadMedicalDocumentQuery(_callerUserId, UserRole.Patient, _documentId);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Forbidden);
    }
}

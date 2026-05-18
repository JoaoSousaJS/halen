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
public class GetPatientDocumentsQueryHandlerTests
{
    private HalenDbContext _db = null!;
    private Mock<IRecordAccessChecker> _accessChecker = null!;
    private GetPatientDocumentsQueryHandler _handler = null!;
    private Guid _callerUserId;
    private Guid _patientProfileId;

    [TestInitialize]
    public async Task Initialize()
    {
        _db = TestDbFactory.Create();
        _accessChecker = new Mock<IRecordAccessChecker>();
        _callerUserId = Guid.NewGuid();
        _patientProfileId = Guid.NewGuid();

        var patientUserId = Guid.NewGuid();
        var uploadedByUserId = Guid.NewGuid();

        _db.Users.AddRange(
            new User { Id = patientUserId, FirstName = "Jane", LastName = "Doe", Email = "jane@test.com", UserName = "jane@test.com", Role = UserRole.Patient },
            new User { Id = uploadedByUserId, FirstName = "Dr", LastName = "Smith", Email = "dr@test.com", UserName = "dr@test.com", Role = UserRole.Doctor }
        );

        _db.PatientProfiles.Add(new PatientProfile { Id = _patientProfileId, UserId = patientUserId });

        _db.MedicalDocuments.AddRange(
            new MedicalDocument
            {
                Id = Guid.NewGuid(),
                PatientProfileId = _patientProfileId,
                DocumentType = MedicalDocumentType.LabResult,
                Title = "Blood Work Results",
                Description = "Annual blood work",
                FileName = "bloodwork.pdf",
                FilePath = "/docs/bloodwork.pdf",
                ContentType = "application/pdf",
                FileSizeBytes = 102400,
                UploadedByUserId = uploadedByUserId
            },
            new MedicalDocument
            {
                Id = Guid.NewGuid(),
                PatientProfileId = _patientProfileId,
                DocumentType = MedicalDocumentType.Imaging,
                Title = "Chest X-Ray",
                Description = null,
                FileName = "xray.png",
                FilePath = "/docs/xray.png",
                ContentType = "image/png",
                FileSizeBytes = 2048000,
                UploadedByUserId = uploadedByUserId
            }
        );

        await _db.SaveChangesAsync();

        _accessChecker.Setup(x => x.CanAccessPatientRecord(
            It.IsAny<Guid>(), It.IsAny<UserRole>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _handler = new GetPatientDocumentsQueryHandler(_db, _accessChecker.Object);
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_Success_ReturnsAllDocuments()
    {
        var query = new GetPatientDocumentsQuery(_callerUserId, UserRole.Doctor, _patientProfileId, null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Documents.Should().HaveCount(2);

        var lab = result.Documents.First(d => d.Title == "Blood Work Results");
        lab.DocumentType.Should().Be("LabResult");
        lab.Description.Should().Be("Annual blood work");
        lab.FileName.Should().Be("bloodwork.pdf");
        lab.ContentType.Should().Be("application/pdf");
        lab.FileSizeBytes.Should().Be(102400);
        lab.UploadedBy.Should().Be("Dr Smith");
    }

    [TestMethod]
    public async Task Handle_FilterByType_ReturnsFilteredDocuments()
    {
        var query = new GetPatientDocumentsQuery(
            _callerUserId, UserRole.Doctor, _patientProfileId, MedicalDocumentType.LabResult);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Documents.Should().HaveCount(1);
        result.Documents[0].Title.Should().Be("Blood Work Results");
    }

    [TestMethod]
    public async Task Handle_AccessDenied_ReturnsForbidden()
    {
        _accessChecker.Setup(x => x.CanAccessPatientRecord(
            It.IsAny<Guid>(), It.IsAny<UserRole>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var query = new GetPatientDocumentsQuery(_callerUserId, UserRole.Patient, _patientProfileId, null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Forbidden);
    }
}

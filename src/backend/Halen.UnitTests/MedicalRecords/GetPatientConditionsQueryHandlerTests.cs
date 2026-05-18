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
public class GetPatientConditionsQueryHandlerTests
{
    private HalenDbContext _db = null!;
    private Mock<IRecordAccessChecker> _accessChecker = null!;
    private GetPatientConditionsQueryHandler _handler = null!;
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
        var addedByUserId = Guid.NewGuid();

        _db.Users.AddRange(
            new User { Id = patientUserId, FirstName = "Jane", LastName = "Doe", Email = "jane@test.com", UserName = "jane@test.com", Role = UserRole.Patient },
            new User { Id = addedByUserId, FirstName = "Dr", LastName = "Smith", Email = "dr@test.com", UserName = "dr@test.com", Role = UserRole.Doctor }
        );

        _db.PatientProfiles.Add(new PatientProfile { Id = _patientProfileId, UserId = patientUserId });

        _db.PatientConditions.AddRange(
            new PatientCondition
            {
                Id = Guid.NewGuid(),
                PatientProfileId = _patientProfileId,
                IcdCode = "E11",
                IcdDescription = "Type 2 diabetes mellitus",
                DateOfOnset = new DateOnly(2019, 6, 1),
                Severity = ConditionSeverity.Moderate,
                Status = ConditionStatus.Active,
                ClinicalNotes = "Managed with metformin",
                AddedByUserId = addedByUserId
            },
            new PatientCondition
            {
                Id = Guid.NewGuid(),
                PatientProfileId = _patientProfileId,
                IcdCode = "J45",
                IcdDescription = "Asthma",
                DateOfOnset = null,
                Severity = ConditionSeverity.Mild,
                Status = ConditionStatus.InRemission,
                ClinicalNotes = null,
                AddedByUserId = addedByUserId
            }
        );

        await _db.SaveChangesAsync();

        _accessChecker.Setup(x => x.CanAccessPatientRecord(
            It.IsAny<Guid>(), It.IsAny<UserRole>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _handler = new GetPatientConditionsQueryHandler(_db, _accessChecker.Object);
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_Success_ReturnsAllConditions()
    {
        var query = new GetPatientConditionsQuery(_callerUserId, UserRole.Doctor, _patientProfileId);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Conditions.Should().HaveCount(2);

        var diabetes = result.Conditions.First(c => c.IcdCode == "E11");
        diabetes.IcdDescription.Should().Be("Type 2 diabetes mellitus");
        diabetes.DateOfOnset.Should().Be("2019-06-01");
        diabetes.Severity.Should().Be("Moderate");
        diabetes.Status.Should().Be("Active");
        diabetes.ClinicalNotes.Should().Be("Managed with metformin");
        diabetes.AddedBy.Should().Be("Dr Smith");

        var asthma = result.Conditions.First(c => c.IcdCode == "J45");
        asthma.Status.Should().Be("InRemission");
        asthma.DateOfOnset.Should().BeNull();
    }

    [TestMethod]
    public async Task Handle_AccessDenied_ReturnsForbidden()
    {
        _accessChecker.Setup(x => x.CanAccessPatientRecord(
            It.IsAny<Guid>(), It.IsAny<UserRole>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var query = new GetPatientConditionsQuery(_callerUserId, UserRole.Patient, _patientProfileId);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Forbidden);
    }
}

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
public class GetPatientFamilyHistoryQueryHandlerTests
{
    private HalenDbContext _db = null!;
    private Mock<IRecordAccessChecker> _accessChecker = null!;
    private GetPatientFamilyHistoryQueryHandler _handler = null!;
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

        _db.PatientFamilyHistories.AddRange(
            new PatientFamilyHistory
            {
                Id = Guid.NewGuid(),
                PatientProfileId = _patientProfileId,
                Relationship = "Father",
                ConditionName = "Heart Disease",
                AgeAtOnset = 55,
                Notes = "Had bypass surgery",
                AddedByUserId = addedByUserId
            },
            new PatientFamilyHistory
            {
                Id = Guid.NewGuid(),
                PatientProfileId = _patientProfileId,
                Relationship = "Mother",
                ConditionName = "Breast Cancer",
                AgeAtOnset = null,
                Notes = null,
                AddedByUserId = addedByUserId
            }
        );

        await _db.SaveChangesAsync();

        _accessChecker.Setup(x => x.CanAccessPatientRecord(
            It.IsAny<Guid>(), It.IsAny<UserRole>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _handler = new GetPatientFamilyHistoryQueryHandler(_db, _accessChecker.Object);
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_Success_ReturnsAllFamilyHistory()
    {
        var query = new GetPatientFamilyHistoryQuery(_callerUserId, UserRole.Doctor, _patientProfileId);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Entries.Should().HaveCount(2);

        var father = result.Entries.First(e => e.Relationship == "Father");
        father.ConditionName.Should().Be("Heart Disease");
        father.AgeAtOnset.Should().Be(55);
        father.Notes.Should().Be("Had bypass surgery");
        father.AddedBy.Should().Be("Dr Smith");

        var mother = result.Entries.First(e => e.Relationship == "Mother");
        mother.AgeAtOnset.Should().BeNull();
        mother.Notes.Should().BeNull();
    }

    [TestMethod]
    public async Task Handle_AccessDenied_ReturnsForbidden()
    {
        _accessChecker.Setup(x => x.CanAccessPatientRecord(
            It.IsAny<Guid>(), It.IsAny<UserRole>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var query = new GetPatientFamilyHistoryQuery(_callerUserId, UserRole.Patient, _patientProfileId);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Forbidden);
    }
}

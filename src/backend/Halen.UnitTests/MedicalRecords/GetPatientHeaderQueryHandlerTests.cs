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
public class GetPatientHeaderQueryHandlerTests
{
    private HalenDbContext _db = null!;
    private Mock<IRecordAccessChecker> _accessChecker = null!;
    private GetPatientHeaderQueryHandler _handler = null!;
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

        _db.PatientProfiles.Add(new PatientProfile
        {
            Id = _patientProfileId,
            UserId = patientUserId,
            City = "Porto"
        });

        _db.PatientAllergies.AddRange(
            new PatientAllergy
            {
                PatientProfileId = _patientProfileId,
                AllergenName = "Peanuts",
                Severity = ConditionSeverity.Severe,
                IsActive = true,
                AddedByUserId = addedByUserId
            },
            new PatientAllergy
            {
                PatientProfileId = _patientProfileId,
                AllergenName = "Dust",
                Severity = ConditionSeverity.Mild,
                IsActive = false, // inactive, should NOT show
                AddedByUserId = addedByUserId
            }
        );

        _db.PatientConditions.AddRange(
            new PatientCondition
            {
                PatientProfileId = _patientProfileId,
                IcdCode = "E11",
                IcdDescription = "Type 2 Diabetes",
                Severity = ConditionSeverity.Moderate,
                Status = ConditionStatus.Active,
                AddedByUserId = addedByUserId
            },
            new PatientCondition
            {
                PatientProfileId = _patientProfileId,
                IcdCode = "J45",
                IcdDescription = "Asthma",
                Severity = ConditionSeverity.Mild,
                Status = ConditionStatus.Resolved, // not active, should NOT show
                AddedByUserId = addedByUserId
            }
        );

        await _db.SaveChangesAsync();

        _accessChecker.Setup(x => x.CanAccessPatientRecord(
            It.IsAny<Guid>(), It.IsAny<UserRole>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _handler = new GetPatientHeaderQueryHandler(_db, _accessChecker.Object);
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_Success_ReturnsPatientHeader()
    {
        var query = new GetPatientHeaderQuery(_callerUserId, UserRole.Doctor, _patientProfileId);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Header.Should().NotBeNull();
        result.Header!.PatientProfileId.Should().Be(_patientProfileId);
        result.Header.PatientName.Should().Be("Jane Doe");
        result.Header.City.Should().Be("Porto");
        result.Header.AllergyChips.Should().ContainSingle("Peanuts");
        result.Header.ConditionChips.Should().ContainSingle("Type 2 Diabetes");
    }

    [TestMethod]
    public async Task Handle_AccessDenied_ReturnsForbidden()
    {
        _accessChecker.Setup(x => x.CanAccessPatientRecord(
            It.IsAny<Guid>(), It.IsAny<UserRole>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var query = new GetPatientHeaderQuery(_callerUserId, UserRole.Patient, _patientProfileId);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Forbidden);
    }

    [TestMethod]
    public async Task Handle_PatientNotFound_ReturnsNotFound()
    {
        var query = new GetPatientHeaderQuery(_callerUserId, UserRole.Doctor, Guid.NewGuid());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.NotFound);
    }
}

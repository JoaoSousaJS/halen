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
public class GetPatientAllergiesQueryHandlerTests
{
    private HalenDbContext _db = null!;
    private Mock<IRecordAccessChecker> _accessChecker = null!;
    private GetPatientAllergiesQueryHandler _handler = null!;
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

        _db.PatientAllergies.AddRange(
            new PatientAllergy
            {
                Id = Guid.NewGuid(),
                PatientProfileId = _patientProfileId,
                AllergenName = "Peanuts",
                Reaction = "Anaphylaxis",
                Severity = ConditionSeverity.Severe,
                DateIdentified = new DateOnly(2020, 3, 15),
                IsActive = true,
                AddedByUserId = addedByUserId
            },
            new PatientAllergy
            {
                Id = Guid.NewGuid(),
                PatientProfileId = _patientProfileId,
                AllergenName = "Penicillin",
                Reaction = "Rash",
                Severity = ConditionSeverity.Moderate,
                DateIdentified = null,
                IsActive = false,
                AddedByUserId = addedByUserId
            }
        );

        await _db.SaveChangesAsync();

        _accessChecker.Setup(x => x.CanAccessPatientRecord(
            It.IsAny<Guid>(), It.IsAny<UserRole>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _handler = new GetPatientAllergiesQueryHandler(_db, _accessChecker.Object);
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_Success_ReturnsAllAllergies()
    {
        var query = new GetPatientAllergiesQuery(_callerUserId, UserRole.Doctor, _patientProfileId);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Allergies.Should().HaveCount(2);

        var peanut = result.Allergies.First(a => a.AllergenName == "Peanuts");
        peanut.Reaction.Should().Be("Anaphylaxis");
        peanut.Severity.Should().Be("Severe");
        peanut.DateIdentified.Should().Be("2020-03-15");
        peanut.IsActive.Should().BeTrue();
        peanut.AddedBy.Should().Be("Dr Smith");

        var penicillin = result.Allergies.First(a => a.AllergenName == "Penicillin");
        penicillin.IsActive.Should().BeFalse();
        penicillin.DateIdentified.Should().BeNull();
    }

    [TestMethod]
    public async Task Handle_AccessDenied_ReturnsForbidden()
    {
        _accessChecker.Setup(x => x.CanAccessPatientRecord(
            It.IsAny<Guid>(), It.IsAny<UserRole>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var query = new GetPatientAllergiesQuery(_callerUserId, UserRole.Patient, _patientProfileId);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Forbidden);
    }
}

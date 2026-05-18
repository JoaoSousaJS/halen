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
public class GetPatientVitalsHistoryQueryHandlerTests
{
    private HalenDbContext _db = null!;
    private Mock<IRecordAccessChecker> _accessChecker = null!;
    private GetPatientVitalsHistoryQueryHandler _handler = null!;
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

        _db.PatientVitals.AddRange(
            new PatientVital
            {
                Id = Guid.NewGuid(),
                PatientProfileId = _patientProfileId,
                VitalType = VitalType.BloodPressure,
                Value = 120,
                SecondaryValue = 80,
                Unit = "mmHg",
                MeasuredAt = DateTime.UtcNow.AddDays(-10),
                Source = VitalSource.Manual,
                Notes = "Normal reading",
                AddedByUserId = addedByUserId
            },
            new PatientVital
            {
                Id = Guid.NewGuid(),
                PatientProfileId = _patientProfileId,
                VitalType = VitalType.BloodPressure,
                Value = 130,
                SecondaryValue = 85,
                Unit = "mmHg",
                MeasuredAt = DateTime.UtcNow.AddDays(-5),
                Source = VitalSource.Device,
                Notes = null,
                AddedByUserId = addedByUserId
            },
            new PatientVital
            {
                Id = Guid.NewGuid(),
                PatientProfileId = _patientProfileId,
                VitalType = VitalType.HeartRate,
                Value = 72,
                SecondaryValue = null,
                Unit = "bpm",
                MeasuredAt = DateTime.UtcNow.AddDays(-3),
                Source = VitalSource.Device,
                Notes = null,
                AddedByUserId = addedByUserId
            },
            // Old vital outside 90-day window
            new PatientVital
            {
                Id = Guid.NewGuid(),
                PatientProfileId = _patientProfileId,
                VitalType = VitalType.BloodPressure,
                Value = 115,
                SecondaryValue = 75,
                Unit = "mmHg",
                MeasuredAt = DateTime.UtcNow.AddDays(-100),
                Source = VitalSource.ClinicalEntry,
                Notes = null,
                AddedByUserId = addedByUserId
            }
        );

        await _db.SaveChangesAsync();

        _accessChecker.Setup(x => x.CanAccessPatientRecord(
            It.IsAny<Guid>(), It.IsAny<UserRole>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _handler = new GetPatientVitalsHistoryQueryHandler(_db, _accessChecker.Object);
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_Success_ReturnsFilteredByTypeAndDateRange()
    {
        var query = new GetPatientVitalsHistoryQuery(
            _callerUserId, UserRole.Doctor, _patientProfileId, VitalType.BloodPressure, 90);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Readings.Should().HaveCount(2); // Only BloodPressure within 90 days
        result.Readings.Should().AllSatisfy(r =>
        {
            r.Unit.Should().Be("mmHg");
            r.AddedBy.Should().Be("Dr Smith");
        });
    }

    [TestMethod]
    public async Task Handle_AccessDenied_ReturnsForbidden()
    {
        _accessChecker.Setup(x => x.CanAccessPatientRecord(
            It.IsAny<Guid>(), It.IsAny<UserRole>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var query = new GetPatientVitalsHistoryQuery(
            _callerUserId, UserRole.Patient, _patientProfileId, VitalType.BloodPressure, 90);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Forbidden);
    }

    [TestMethod]
    public async Task Handle_NoReadings_ReturnsEmptyList()
    {
        var query = new GetPatientVitalsHistoryQuery(
            _callerUserId, UserRole.Doctor, _patientProfileId, VitalType.Weight, 90);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Readings.Should().BeEmpty();
    }
}

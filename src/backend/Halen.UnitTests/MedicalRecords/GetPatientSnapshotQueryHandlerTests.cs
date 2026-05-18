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
public class GetPatientSnapshotQueryHandlerTests
{
    private HalenDbContext _db = null!;
    private Mock<IRecordAccessChecker> _accessChecker = null!;
    private GetPatientSnapshotQueryHandler _handler = null!;
    private Guid _callerUserId;
    private Guid _patientProfileId;
    private Guid _addedByUserId;

    [TestInitialize]
    public async Task Initialize()
    {
        _db = TestDbFactory.Create();
        _accessChecker = new Mock<IRecordAccessChecker>();
        _callerUserId = Guid.NewGuid();
        _patientProfileId = Guid.NewGuid();
        _addedByUserId = Guid.NewGuid();

        var patientUserId = Guid.NewGuid();

        _db.Users.AddRange(
            new User { Id = patientUserId, FirstName = "Jane", LastName = "Doe", Email = "jane@test.com", UserName = "jane@test.com", Role = UserRole.Patient },
            new User { Id = _addedByUserId, FirstName = "Dr", LastName = "Smith", Email = "dr@test.com", UserName = "dr@test.com", Role = UserRole.Doctor }
        );

        _db.PatientProfiles.Add(new PatientProfile { Id = _patientProfileId, UserId = patientUserId });

        _accessChecker.Setup(x => x.CanAccessPatientRecord(
            It.IsAny<Guid>(), It.IsAny<UserRole>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _handler = new GetPatientSnapshotQueryHandler(_db, _accessChecker.Object);
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_FullSnapshot_ReturnsAllCategories()
    {
        // Seed all categories
        _db.PatientAllergies.Add(new PatientAllergy
        {
            PatientProfileId = _patientProfileId,
            AllergenName = "Peanuts",
            Reaction = "Anaphylaxis",
            Severity = ConditionSeverity.Severe,
            IsActive = true,
            AddedByUserId = _addedByUserId
        });

        _db.PatientConditions.Add(new PatientCondition
        {
            PatientProfileId = _patientProfileId,
            IcdCode = "E11",
            IcdDescription = "Type 2 Diabetes",
            Severity = ConditionSeverity.Moderate,
            Status = ConditionStatus.Active,
            AddedByUserId = _addedByUserId
        });

        _db.PatientMedications.Add(new PatientMedication
        {
            PatientProfileId = _patientProfileId,
            MedicationName = "Metformin",
            Dosage = "500mg",
            Frequency = "Twice daily",
            StartDate = new DateOnly(2020, 1, 1),
            IsActive = true,
            AddedByUserId = _addedByUserId
        });

        _db.PatientFamilyHistories.Add(new PatientFamilyHistory
        {
            PatientProfileId = _patientProfileId,
            Relationship = "Father",
            ConditionName = "Heart Disease",
            AddedByUserId = _addedByUserId
        });

        _db.PatientVitals.AddRange(
            new PatientVital
            {
                PatientProfileId = _patientProfileId,
                VitalType = VitalType.BloodPressure,
                Value = 120,
                SecondaryValue = 80,
                Unit = "mmHg",
                MeasuredAt = DateTime.UtcNow.AddDays(-2),
                Source = VitalSource.Manual,
                AddedByUserId = _addedByUserId
            },
            new PatientVital
            {
                PatientProfileId = _patientProfileId,
                VitalType = VitalType.HeartRate,
                Value = 72,
                Unit = "bpm",
                MeasuredAt = DateTime.UtcNow.AddDays(-1),
                Source = VitalSource.Device,
                AddedByUserId = _addedByUserId
            }
        );

        _db.MedicalDocuments.Add(new MedicalDocument
        {
            PatientProfileId = _patientProfileId,
            DocumentType = MedicalDocumentType.LabResult,
            Title = "Blood Work",
            FileName = "blood.pdf",
            FilePath = "/docs/blood.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 1024,
            UploadedByUserId = _addedByUserId
        });

        await _db.SaveChangesAsync();

        var query = new GetPatientSnapshotQuery(_callerUserId, UserRole.Doctor, _patientProfileId);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Snapshot.Should().NotBeNull();

        var snap = result.Snapshot!;
        snap.Allergies.Should().HaveCount(1);
        snap.Allergies[0].AllergenName.Should().Be("Peanuts");
        snap.Allergies[0].Severity.Should().Be("Severe");

        snap.ActiveConditions.Should().HaveCount(1);
        snap.ActiveConditions[0].IcdDescription.Should().Be("Type 2 Diabetes");

        snap.ActiveMedications.Should().HaveCount(1);
        snap.ActiveMedications[0].MedicationName.Should().Be("Metformin");
        snap.ActiveMedications[0].StartDate.Should().Be("2020-01-01");

        snap.FamilyHistory.Should().HaveCount(1);
        snap.FamilyHistory[0].Relationship.Should().Be("Father");

        snap.LatestVitals.Should().NotBeNull();
        snap.LatestVitals!.BloodPressure.Should().NotBeNull();
        snap.LatestVitals.BloodPressure!.Value.Should().Be(120);
        snap.LatestVitals.BloodPressure.SecondaryValue.Should().Be(80);
        snap.LatestVitals.HeartRate.Should().NotBeNull();
        snap.LatestVitals.HeartRate!.Value.Should().Be(72);

        snap.OnboardingProgress.Should().Be(6); // all 6 categories present
    }

    [TestMethod]
    public async Task Handle_EmptyState_ReturnsEmptySnapshot()
    {
        await _db.SaveChangesAsync();

        var query = new GetPatientSnapshotQuery(_callerUserId, UserRole.Doctor, _patientProfileId);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Snapshot.Should().NotBeNull();

        var snap = result.Snapshot!;
        snap.Allergies.Should().BeEmpty();
        snap.ActiveConditions.Should().BeEmpty();
        snap.ActiveMedications.Should().BeEmpty();
        snap.FamilyHistory.Should().BeEmpty();
        snap.LatestVitals.Should().NotBeNull();
        snap.LatestVitals!.BloodPressure.Should().BeNull();
        snap.LatestVitals.HeartRate.Should().BeNull();
        snap.LatestVitals.Weight.Should().BeNull();
        snap.LatestVitals.SpO2.Should().BeNull();
        snap.OnboardingProgress.Should().Be(0);
    }

    [TestMethod]
    public async Task Handle_PartialOnboarding_ReturnsCorrectProgress()
    {
        // Only allergies and vitals seeded -> 2/6
        _db.PatientAllergies.Add(new PatientAllergy
        {
            PatientProfileId = _patientProfileId,
            AllergenName = "Dust",
            Severity = ConditionSeverity.Mild,
            IsActive = true,
            AddedByUserId = _addedByUserId
        });

        _db.PatientVitals.Add(new PatientVital
        {
            PatientProfileId = _patientProfileId,
            VitalType = VitalType.Weight,
            Value = 70,
            Unit = "kg",
            MeasuredAt = DateTime.UtcNow,
            Source = VitalSource.Manual,
            AddedByUserId = _addedByUserId
        });

        await _db.SaveChangesAsync();

        var query = new GetPatientSnapshotQuery(_callerUserId, UserRole.Doctor, _patientProfileId);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Snapshot!.OnboardingProgress.Should().Be(2);
    }

    [TestMethod]
    public async Task Handle_AccessDenied_ReturnsForbidden()
    {
        _accessChecker.Setup(x => x.CanAccessPatientRecord(
            It.IsAny<Guid>(), It.IsAny<UserRole>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var query = new GetPatientSnapshotQuery(_callerUserId, UserRole.Patient, _patientProfileId);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Forbidden);
    }
}

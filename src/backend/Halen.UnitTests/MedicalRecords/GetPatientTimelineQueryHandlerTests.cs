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
public class GetPatientTimelineQueryHandlerTests
{
    private HalenDbContext _db = null!;
    private Mock<IRecordAccessChecker> _accessChecker = null!;
    private GetPatientTimelineQueryHandler _handler = null!;
    private Guid _callerUserId;
    private Guid _patientProfileId;
    private Guid _doctorProfileId;
    private Guid _addedByUserId;

    [TestInitialize]
    public async Task Initialize()
    {
        _db = TestDbFactory.Create();
        _accessChecker = new Mock<IRecordAccessChecker>();
        _callerUserId = Guid.NewGuid();
        _patientProfileId = Guid.NewGuid();
        _doctorProfileId = Guid.NewGuid();
        _addedByUserId = Guid.NewGuid();

        var patientUserId = Guid.NewGuid();

        _db.Users.AddRange(
            new User { Id = patientUserId, FirstName = "Jane", LastName = "Doe", Email = "jane@test.com", UserName = "jane@test.com", Role = UserRole.Patient },
            new User { Id = _addedByUserId, FirstName = "Dr", LastName = "Smith", Email = "dr@test.com", UserName = "dr@test.com", Role = UserRole.Doctor }
        );

        _db.PatientProfiles.Add(new PatientProfile { Id = _patientProfileId, UserId = patientUserId });
        _db.DoctorProfiles.Add(new DoctorProfile
        {
            Id = _doctorProfileId,
            UserId = _addedByUserId,
            Specialty = "Internal Medicine",
            LicenseNumber = "LIC-001",
            ConsultationFee = 100,
            YearsOfExperience = 10
        });

        // Appointment
        _db.Appointments.Add(new Appointment
        {
            Id = Guid.NewGuid(),
            PatientId = _patientProfileId,
            DoctorId = _doctorProfileId,
            ScheduledAt = DateTime.UtcNow.AddDays(-5),
            Reason = "Annual checkup",
            Status = AppointmentStatus.Completed
        });

        // Prescription
        _db.Prescriptions.Add(new Prescription
        {
            Id = Guid.NewGuid(),
            PatientId = _patientProfileId,
            DoctorId = _doctorProfileId,
            DrugName = "Metformin",
            Dosage = "500mg",
            Frequency = "Twice daily",
            Status = PrescriptionStatus.Active,
            CreatedAt = DateTime.UtcNow.AddDays(-4)
        });

        // Condition
        _db.PatientConditions.Add(new PatientCondition
        {
            Id = Guid.NewGuid(),
            PatientProfileId = _patientProfileId,
            IcdCode = "E11",
            IcdDescription = "Type 2 Diabetes",
            Severity = ConditionSeverity.Moderate,
            Status = ConditionStatus.Active,
            AddedByUserId = _addedByUserId,
            CreatedAt = DateTime.UtcNow.AddDays(-3)
        });

        // Allergy
        _db.PatientAllergies.Add(new PatientAllergy
        {
            Id = Guid.NewGuid(),
            PatientProfileId = _patientProfileId,
            AllergenName = "Peanuts",
            Severity = ConditionSeverity.Severe,
            IsActive = true,
            AddedByUserId = _addedByUserId,
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        });

        // Vital
        _db.PatientVitals.Add(new PatientVital
        {
            Id = Guid.NewGuid(),
            PatientProfileId = _patientProfileId,
            VitalType = VitalType.BloodPressure,
            Value = 120,
            SecondaryValue = 80,
            Unit = "mmHg",
            MeasuredAt = DateTime.UtcNow.AddDays(-1),
            Source = VitalSource.Manual,
            AddedByUserId = _addedByUserId
        });

        // Document
        _db.MedicalDocuments.Add(new MedicalDocument
        {
            Id = Guid.NewGuid(),
            PatientProfileId = _patientProfileId,
            DocumentType = MedicalDocumentType.LabResult,
            Title = "Blood Work",
            FileName = "blood.pdf",
            FilePath = "/docs/blood.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 1024,
            UploadedByUserId = _addedByUserId,
            CreatedAt = DateTime.UtcNow.AddDays(-6)
        });

        // Medication
        _db.PatientMedications.Add(new PatientMedication
        {
            Id = Guid.NewGuid(),
            PatientProfileId = _patientProfileId,
            MedicationName = "Metformin",
            Dosage = "500mg",
            Frequency = "Twice daily",
            IsActive = true,
            AddedByUserId = _addedByUserId,
            CreatedAt = DateTime.UtcNow.AddDays(-7)
        });

        // Family History
        _db.PatientFamilyHistories.Add(new PatientFamilyHistory
        {
            Id = Guid.NewGuid(),
            PatientProfileId = _patientProfileId,
            Relationship = "Father",
            ConditionName = "Heart Disease",
            AddedByUserId = _addedByUserId,
            CreatedAt = DateTime.UtcNow.AddDays(-8)
        });

        await _db.SaveChangesAsync();

        _accessChecker.Setup(x => x.CanAccessPatientRecord(
            It.IsAny<Guid>(), It.IsAny<UserRole>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _handler = new GetPatientTimelineQueryHandler(_db, _accessChecker.Object);
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_Success_ReturnsMixedTimelineEntriesSortedByDate()
    {
        var query = new GetPatientTimelineQuery(
            _callerUserId, UserRole.Doctor, _patientProfileId,
            null, null, null, null, 1, 50);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.TotalCount.Should().Be(8);
        result.Entries.Should().HaveCount(8);

        // Should be sorted by OccurredAt descending (most recent first)
        result.Entries.Should().BeInDescendingOrder(e => e.OccurredAt);

        // Verify types are present
        var types = result.Entries.Select(e => e.Type).Distinct().ToList();
        types.Should().Contain("Appointment");
        types.Should().Contain("Prescription");
        types.Should().Contain("Condition");
        types.Should().Contain("Allergy");
        types.Should().Contain("Vital");
        types.Should().Contain("Document");
        types.Should().Contain("Medication");
        types.Should().Contain("FamilyHistory");
    }

    [TestMethod]
    public async Task Handle_FilterByType_ReturnsOnlyMatchingTypes()
    {
        var query = new GetPatientTimelineQuery(
            _callerUserId, UserRole.Doctor, _patientProfileId,
            new[] { "Appointment", "Prescription" }, null, null, null, 1, 50);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.TotalCount.Should().Be(2);
        result.Entries.Should().AllSatisfy(e =>
        {
            e.Type.Should().BeOneOf("Appointment", "Prescription");
        });
    }

    [TestMethod]
    public async Task Handle_Pagination_RespectsPageAndPageSize()
    {
        var query = new GetPatientTimelineQuery(
            _callerUserId, UserRole.Doctor, _patientProfileId,
            null, null, null, null, 1, 3);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.TotalCount.Should().Be(8);
        result.Entries.Should().HaveCount(3);

        // Second page
        var query2 = new GetPatientTimelineQuery(
            _callerUserId, UserRole.Doctor, _patientProfileId,
            null, null, null, null, 2, 3);

        var result2 = await _handler.Handle(query2, CancellationToken.None);

        result2.Entries.Should().HaveCount(3);
        result2.TotalCount.Should().Be(8);

        // No overlap
        result.Entries.Select(e => e.Id).Should().NotIntersectWith(result2.Entries.Select(e => e.Id));
    }

    [TestMethod]
    public async Task Handle_AccessDenied_ReturnsForbidden()
    {
        _accessChecker.Setup(x => x.CanAccessPatientRecord(
            It.IsAny<Guid>(), It.IsAny<UserRole>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var query = new GetPatientTimelineQuery(
            _callerUserId, UserRole.Patient, _patientProfileId,
            null, null, null, null, 1, 50);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Forbidden);
    }

    [TestMethod]
    public async Task Handle_EmptyResult_ReturnsEmptyEntries()
    {
        var emptyPatientProfileId = Guid.NewGuid();
        var emptyUserId = Guid.NewGuid();
        _db.Users.Add(new User { Id = emptyUserId, FirstName = "Empty", LastName = "Pat", Email = "empty@test.com", UserName = "empty@test.com", Role = UserRole.Patient });
        _db.PatientProfiles.Add(new PatientProfile { Id = emptyPatientProfileId, UserId = emptyUserId });
        await _db.SaveChangesAsync();

        var query = new GetPatientTimelineQuery(
            _callerUserId, UserRole.Doctor, emptyPatientProfileId,
            null, null, null, null, 1, 50);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.TotalCount.Should().Be(0);
        result.Entries.Should().BeEmpty();
    }
}

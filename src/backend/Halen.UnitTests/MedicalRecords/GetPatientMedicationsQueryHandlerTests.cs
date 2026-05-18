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
public class GetPatientMedicationsQueryHandlerTests
{
    private HalenDbContext _db = null!;
    private Mock<IRecordAccessChecker> _accessChecker = null!;
    private GetPatientMedicationsQueryHandler _handler = null!;
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
        var prescriptionId = Guid.NewGuid();

        _db.Users.AddRange(
            new User { Id = patientUserId, FirstName = "Jane", LastName = "Doe", Email = "jane@test.com", UserName = "jane@test.com", Role = UserRole.Patient },
            new User { Id = addedByUserId, FirstName = "Dr", LastName = "Smith", Email = "dr@test.com", UserName = "dr@test.com", Role = UserRole.Doctor }
        );

        _db.PatientProfiles.Add(new PatientProfile { Id = _patientProfileId, UserId = patientUserId });

        _db.PatientMedications.AddRange(
            new PatientMedication
            {
                Id = Guid.NewGuid(),
                PatientProfileId = _patientProfileId,
                MedicationName = "Metformin",
                Dosage = "500mg",
                Frequency = "Twice daily",
                StartDate = new DateOnly(2020, 1, 1),
                EndDate = null,
                IsActive = true,
                PrescribedByName = "Dr Smith",
                LinkedPrescriptionId = prescriptionId,
                AddedByUserId = addedByUserId
            },
            new PatientMedication
            {
                Id = Guid.NewGuid(),
                PatientProfileId = _patientProfileId,
                MedicationName = "Amoxicillin",
                Dosage = "250mg",
                Frequency = "Three times daily",
                StartDate = new DateOnly(2024, 6, 1),
                EndDate = new DateOnly(2024, 6, 14),
                IsActive = false,
                PrescribedByName = null,
                LinkedPrescriptionId = null,
                AddedByUserId = addedByUserId
            }
        );

        await _db.SaveChangesAsync();

        _accessChecker.Setup(x => x.CanAccessPatientRecord(
            It.IsAny<Guid>(), It.IsAny<UserRole>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _handler = new GetPatientMedicationsQueryHandler(_db, _accessChecker.Object);
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_Success_ReturnsAllMedications()
    {
        var query = new GetPatientMedicationsQuery(_callerUserId, UserRole.Doctor, _patientProfileId);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Medications.Should().HaveCount(2);

        var metformin = result.Medications.First(m => m.MedicationName == "Metformin");
        metformin.Dosage.Should().Be("500mg");
        metformin.Frequency.Should().Be("Twice daily");
        metformin.StartDate.Should().Be("2020-01-01");
        metformin.EndDate.Should().BeNull();
        metformin.IsActive.Should().BeTrue();
        metformin.PrescribedByName.Should().Be("Dr Smith");
        metformin.LinkedPrescriptionId.Should().Be(result.Medications.First(m => m.MedicationName == "Metformin").LinkedPrescriptionId);
        metformin.AddedBy.Should().Be("Dr Smith");

        var amox = result.Medications.First(m => m.MedicationName == "Amoxicillin");
        amox.IsActive.Should().BeFalse();
        amox.EndDate.Should().Be("2024-06-14");
    }

    [TestMethod]
    public async Task Handle_AccessDenied_ReturnsForbidden()
    {
        _accessChecker.Setup(x => x.CanAccessPatientRecord(
            It.IsAny<Guid>(), It.IsAny<UserRole>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var query = new GetPatientMedicationsQuery(_callerUserId, UserRole.Patient, _patientProfileId);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Forbidden);
    }
}

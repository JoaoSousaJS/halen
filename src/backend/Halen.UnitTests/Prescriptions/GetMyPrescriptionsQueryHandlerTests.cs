using FluentAssertions;
using Halen.Application.Prescriptions.Queries;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.Infrastructure.Persistence;
using Halen.UnitTests.Helpers;

namespace Halen.UnitTests.Prescriptions;

[TestClass]
public class GetMyPrescriptionsQueryHandlerTests
{
    private HalenDbContext _db = null!;
    private GetMyPrescriptionsQueryHandler _handler = null!;
    private Guid _doctorUserId;
    private Guid _patientUserId;
    private Guid _doctorProfileId;
    private Guid _patientProfileId;

    [TestInitialize]
    public async Task Initialize()
    {
        _db = TestDbFactory.Create();

        _doctorUserId = Guid.NewGuid();
        _patientUserId = Guid.NewGuid();
        _doctorProfileId = Guid.NewGuid();
        _patientProfileId = Guid.NewGuid();

        _db.Users.AddRange(
            new User
            {
                Id = _doctorUserId, FirstName = "Dr", LastName = "House",
                Email = "house@test.com", UserName = "house@test.com", Role = UserRole.Doctor,
            },
            new User
            {
                Id = _patientUserId, FirstName = "John", LastName = "Doe",
                Email = "john@test.com", UserName = "john@test.com", Role = UserRole.Patient,
            }
        );

        _db.DoctorProfiles.Add(new DoctorProfile
        {
            Id = _doctorProfileId, UserId = _doctorUserId,
            Specialty = "General", LicenseNumber = "LIC-001",
            ConsultationFee = 100, YearsOfExperience = 5,
            KycStatus = KycStatus.Approved,
        });

        _db.PatientProfiles.Add(new PatientProfile
        {
            Id = _patientProfileId, UserId = _patientUserId,
        });

        await _db.SaveChangesAsync();

        _handler = new GetMyPrescriptionsQueryHandler(_db);
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_AsDoctor_ReturnsIssuedPrescriptions()
    {
        // Create a second doctor with their own prescriptions
        var otherDoctorUserId = Guid.NewGuid();
        var otherDoctorProfileId = Guid.NewGuid();
        _db.Users.Add(new User
        {
            Id = otherDoctorUserId, FirstName = "Dr", LastName = "Wilson",
            Email = "wilson@test.com", UserName = "wilson@test.com", Role = UserRole.Doctor,
        });
        _db.DoctorProfiles.Add(new DoctorProfile
        {
            Id = otherDoctorProfileId, UserId = otherDoctorUserId,
            Specialty = "Oncology", LicenseNumber = "LIC-002",
            ConsultationFee = 200, YearsOfExperience = 8,
            KycStatus = KycStatus.Approved,
        });

        _db.Prescriptions.AddRange(
            new Prescription
            {
                DoctorId = _doctorProfileId, PatientId = _patientProfileId,
                DrugName = "Amoxicillin", Dosage = "500mg", Frequency = "3x daily",
                RefillsRemaining = 2,
            },
            new Prescription
            {
                DoctorId = otherDoctorProfileId, PatientId = _patientProfileId,
                DrugName = "Ibuprofen", Dosage = "200mg", Frequency = "2x daily",
                RefillsRemaining = 0,
            }
        );
        await _db.SaveChangesAsync();

        var query = new GetMyPrescriptionsQuery(_doctorUserId, UserRole.Doctor);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Prescriptions.Should().HaveCount(1);
        result.Prescriptions[0].DrugName.Should().Be("Amoxicillin");
        result.Prescriptions[0].DoctorName.Should().Be("Dr. House");
    }

    [TestMethod]
    public async Task Handle_AsPatient_ReturnsOnlyOwnPrescriptions()
    {
        // Create a second patient
        var otherPatientUserId = Guid.NewGuid();
        var otherPatientProfileId = Guid.NewGuid();
        _db.Users.Add(new User
        {
            Id = otherPatientUserId, FirstName = "Other", LastName = "Patient",
            Email = "other@test.com", UserName = "other@test.com", Role = UserRole.Patient,
        });
        _db.PatientProfiles.Add(new PatientProfile { Id = otherPatientProfileId, UserId = otherPatientUserId });

        _db.Prescriptions.AddRange(
            new Prescription
            {
                DoctorId = _doctorProfileId, PatientId = _patientProfileId,
                DrugName = "Amoxicillin", Dosage = "500mg", Frequency = "3x daily",
                RefillsRemaining = 2,
            },
            new Prescription
            {
                DoctorId = _doctorProfileId, PatientId = otherPatientProfileId,
                DrugName = "Aspirin", Dosage = "100mg", Frequency = "1x daily",
                RefillsRemaining = 5,
            }
        );
        await _db.SaveChangesAsync();

        var query = new GetMyPrescriptionsQuery(_patientUserId, UserRole.Patient);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Prescriptions.Should().HaveCount(1);
        result.Prescriptions[0].DrugName.Should().Be("Amoxicillin");
        result.Prescriptions[0].PatientName.Should().Be("John Doe");
    }

    [TestMethod]
    public async Task Handle_NoProfile_ReturnsEmpty()
    {
        // User with no doctor or patient profile
        var orphanUserId = Guid.NewGuid();
        _db.Users.Add(new User
        {
            Id = orphanUserId, FirstName = "Orphan", LastName = "User",
            Email = "orphan@test.com", UserName = "orphan@test.com", Role = UserRole.Patient,
        });

        _db.Prescriptions.Add(new Prescription
        {
            DoctorId = _doctorProfileId, PatientId = _patientProfileId,
            DrugName = "Amoxicillin", Dosage = "500mg", Frequency = "3x daily",
            RefillsRemaining = 2,
        });
        await _db.SaveChangesAsync();

        var query = new GetMyPrescriptionsQuery(orphanUserId, UserRole.Patient);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Prescriptions.Should().BeEmpty();
    }
}

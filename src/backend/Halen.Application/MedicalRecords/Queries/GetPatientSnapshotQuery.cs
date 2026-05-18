using Halen.Application.Common;
using Halen.Domain.Enums;
using MediatR;

namespace Halen.Application.MedicalRecords.Queries;

public record GetPatientSnapshotQuery(
    Guid CallerUserId,
    UserRole CallerRole,
    Guid PatientProfileId
) : IRequest<GetPatientSnapshotResult>;

public record GetPatientSnapshotResult(
    bool Success,
    PatientSnapshotDto? Snapshot = null,
    string? Error = null,
    ErrorKind? Kind = null);

public record PatientSnapshotDto(
    AllergySnapshotDto[] Allergies,
    ConditionSnapshotDto[] ActiveConditions,
    MedicationSnapshotDto[] ActiveMedications,
    FamilyHistorySnapshotDto[] FamilyHistory,
    LatestVitalsDto? LatestVitals,
    int OnboardingProgress);

public record AllergySnapshotDto(
    Guid Id,
    string AllergenName,
    string? Reaction,
    string Severity);

public record ConditionSnapshotDto(
    Guid Id,
    string IcdDescription,
    string Severity);

public record MedicationSnapshotDto(
    Guid Id,
    string MedicationName,
    string Dosage,
    string Frequency,
    string? StartDate);

public record FamilyHistorySnapshotDto(
    Guid Id,
    string Relationship,
    string ConditionName);

public record LatestVitalsDto(
    VitalReadingDto? BloodPressure,
    VitalReadingDto? HeartRate,
    VitalReadingDto? Weight,
    VitalReadingDto? SpO2);

public record VitalReadingDto(
    decimal Value,
    decimal? SecondaryValue,
    string Unit,
    DateTime MeasuredAt);

using Halen.Application.Common;
using Halen.Domain.Enums;
using MediatR;

namespace Halen.Application.MedicalRecords.Queries;

public record GetPatientHeaderQuery(
    Guid CallerUserId,
    UserRole CallerRole,
    Guid PatientProfileId
) : IRequest<GetPatientHeaderResult>;

public record GetPatientHeaderResult(
    bool Success,
    PatientHeaderDto? Header = null,
    string? Error = null,
    ErrorKind? Kind = null);

public record PatientHeaderDto(
    Guid PatientProfileId,
    string PatientName,
    string? City,
    string[] AllergyChips,
    string[] ConditionChips);

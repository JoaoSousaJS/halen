using Halen.Domain.Enums;
using MediatR;

namespace Halen.Application.Prescriptions.Queries;

public record GetMyPrescriptionsQuery(
    Guid UserId,
    UserRole UserRole
) : IRequest<GetMyPrescriptionsResult>;

public record PrescriptionDto(
    Guid Id,
    string DrugName,
    string Dosage,
    string Frequency,
    int RefillsRemaining,
    string Status,
    string? PharmacyName,
    string DoctorName,
    string PatientName,
    DateTime CreatedAt);

public record GetMyPrescriptionsResult(IReadOnlyList<PrescriptionDto> Prescriptions);

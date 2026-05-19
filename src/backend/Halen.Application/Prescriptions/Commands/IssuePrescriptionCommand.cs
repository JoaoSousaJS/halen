using Halen.Application.Common;
using Halen.Application.Interfaces;
using MediatR;

namespace Halen.Application.Prescriptions.Commands;

public record IssuePrescriptionCommand(
    Guid DoctorUserId,
    Guid PatientId,
    string DrugName,
    string Dosage,
    string Frequency,
    int RefillsRemaining,
    string? PharmacyName
) : IRequest<IssuePrescriptionResult>, IAuditableCommand
{
    Guid IAuditableCommand.ActorId => DoctorUserId;
}


public record IssuePrescriptionResult(
    bool Success,
    Guid? PrescriptionId = null,
    string? Error = null,
    ErrorKind? Kind = null);

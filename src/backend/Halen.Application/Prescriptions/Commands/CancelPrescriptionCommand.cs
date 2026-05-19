using Halen.Application.Common;
using Halen.Application.Interfaces;
using MediatR;

namespace Halen.Application.Prescriptions.Commands;

public record CancelPrescriptionCommand(
    Guid DoctorUserId,
    Guid PrescriptionId
) : IRequest<CancelPrescriptionResult>, IAuditableCommand
{
    Guid IAuditableCommand.ActorId => DoctorUserId;
    string? IAuditableCommand.AuditTargetId => PrescriptionId.ToString();
}


public record CancelPrescriptionResult(
    bool Success,
    string? Error = null,
    ErrorKind? Kind = null);

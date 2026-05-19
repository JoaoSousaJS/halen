using Halen.Application.Common;
using Halen.Application.Interfaces;
using MediatR;

namespace Halen.Application.Clinics.Commands;

public record UpdateClinicCommand(Guid ClinicId, string Name, bool IsActive)
    : IRequest<UpdateClinicResult>, IAuditableCommand
{
    string? IAuditableCommand.AuditTargetId => ClinicId.ToString();
}


public record UpdateClinicResult(bool Success, string? Error = null, ErrorKind? Kind = null);

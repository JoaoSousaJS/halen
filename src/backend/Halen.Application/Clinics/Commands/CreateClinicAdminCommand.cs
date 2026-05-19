using Halen.Application.Common;
using Halen.Application.Interfaces;
using MediatR;

namespace Halen.Application.Clinics.Commands;

public record CreateClinicAdminCommand(
    Guid ClinicId,
    string Email,
    string FirstName,
    string LastName,
    [property: AuditRedact] string TemporaryPassword
) : IRequest<CreateClinicAdminResult>, IAuditableCommand
{
    string? IAuditableCommand.AuditTargetId => ClinicId.ToString();
}

public record CreateClinicAdminResult(bool Success, Guid? UserId = null, string? Error = null, ErrorKind? Kind = null);

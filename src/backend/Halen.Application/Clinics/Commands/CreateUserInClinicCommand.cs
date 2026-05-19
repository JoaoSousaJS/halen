using Halen.Application.Common;
using Halen.Application.Interfaces;
using Halen.Domain.Enums;
using MediatR;

namespace Halen.Application.Clinics.Commands;

public record CreateUserInClinicCommand(
    string Email,
    string FirstName,
    string LastName,
    [property: AuditRedact] string TemporaryPassword,
    UserRole Role
) : IRequest<CreateUserInClinicResult>, IAuditableCommand;


public record CreateUserInClinicResult(bool Success, Guid? UserId = null, string? Error = null, ErrorKind? Kind = null);

using Halen.Application.Common;
using Halen.Domain.Enums;
using MediatR;

namespace Halen.Application.Clinics.Commands;

public record CreateUserInClinicCommand(
    string Email,
    string FirstName,
    string LastName,
    string TemporaryPassword,
    UserRole Role
) : IRequest<CreateUserInClinicResult>;

public record CreateUserInClinicResult(bool Success, Guid? UserId = null, string? Error = null, ErrorKind? Kind = null);

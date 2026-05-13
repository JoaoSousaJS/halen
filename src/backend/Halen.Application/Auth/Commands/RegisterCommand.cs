using Halen.Domain.Enums;
using MediatR;

namespace Halen.Application.Auth.Commands;

public record RegisterCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    UserRole Role
) : IRequest<RegisterResult>;

public record RegisterResult(bool Success, string? Token, string? Error = null);

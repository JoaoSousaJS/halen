using MediatR;

namespace Halen.Application.Auth.Commands;

public record LoginCommand(string Email, string Password) : IRequest<LoginResult>;

public record LoginResult(bool Success, string? Token, string? Error = null);

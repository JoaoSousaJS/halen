using Halen.Application.Common;
using MediatR;

namespace Halen.Application.Profile.Commands;

public record ChangePasswordCommand(
    Guid UserId,
    string CurrentPassword,
    string NewPassword
) : IRequest<ChangePasswordResult>;

public record ChangePasswordResult(bool Success, string? Error = null, ErrorKind? Kind = null);

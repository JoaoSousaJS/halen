using Halen.Application.Common;
using MediatR;

namespace Halen.Application.Messaging.Commands;

public record CloseThreadCommand(
    Guid UserId,
    Guid ThreadId,
    string? Reason = null) : IRequest<CloseThreadResult>;

public record CloseThreadResult(
    bool Success,
    string? Error = null,
    ErrorKind? Kind = null);

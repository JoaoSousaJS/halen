using Halen.Application.Common;
using MediatR;

namespace Halen.Application.Messaging.Commands;

public record SendMessageCommand(
    Guid UserId,
    Guid ThreadId,
    string Content) : IRequest<SendMessageResult>;

public record SendMessageResult(
    bool Success,
    Guid? MessageId = null,
    string? Error = null,
    ErrorKind? Kind = null);

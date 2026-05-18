using Halen.Application.Common;
using MediatR;

namespace Halen.Application.Messaging.Commands;

public record MarkMessagesReadCommand(
    Guid UserId,
    Guid ThreadId) : IRequest<MarkMessagesReadResult>;

public record MarkMessagesReadResult(
    bool Success,
    string? Error = null,
    ErrorKind? Kind = null);

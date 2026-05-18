namespace Halen.Application.Events;

public record MessagesReadEvent(
    Guid ThreadId,
    Guid ReadByUserId);

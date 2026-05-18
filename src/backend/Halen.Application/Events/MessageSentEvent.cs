namespace Halen.Application.Events;

public record MessageSentEvent(
    Guid MessageId,
    Guid ThreadId,
    Guid SenderUserId,
    Guid RecipientUserId,
    string SenderName,
    string Preview);

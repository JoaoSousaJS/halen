namespace Halen.Application.Events;

public record ThreadClosedEvent(
    Guid ThreadId,
    Guid ClosedByUserId,
    string ClosedByName);

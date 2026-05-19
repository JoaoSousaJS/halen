namespace Halen.Application.Interfaces;

public interface IAuditContextProvider
{
    Guid ActorId { get; }
    string ActorName { get; }
    string IpAddress { get; }
}

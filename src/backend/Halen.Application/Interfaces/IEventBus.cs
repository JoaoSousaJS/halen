namespace Halen.Application.Interfaces;

public interface IEventBus
{
    Task PublishAsync<T>(string topic, T message, CancellationToken ct = default) where T : class;
}

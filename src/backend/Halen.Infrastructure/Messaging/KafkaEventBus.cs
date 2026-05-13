using System.Text.Json;
using Confluent.Kafka;
using Halen.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Halen.Infrastructure.Messaging;

public class KafkaEventBus(IProducer<string, string> producer, ILogger<KafkaEventBus> logger) : IEventBus
{
    public async Task PublishAsync<T>(string topic, T message, CancellationToken ct = default) where T : class
    {
        var json = JsonSerializer.Serialize(message);
        var kafkaMessage = new Message<string, string>
        {
            Key = Guid.NewGuid().ToString(),
            Value = json
        };

        var result = await producer.ProduceAsync(topic, kafkaMessage, ct);
        logger.LogInformation("Published to {Topic} at offset {Offset}", topic, result.Offset);
    }
}

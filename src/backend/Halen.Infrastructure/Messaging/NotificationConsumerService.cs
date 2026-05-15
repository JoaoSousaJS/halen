using Confluent.Kafka;
using Halen.Application.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Halen.Infrastructure.Messaging;

public class NotificationConsumerService(
    NotificationMessageHandler handler,
    IConfiguration configuration,
    ILogger<NotificationConsumerService> logger) : BackgroundService
{
    private static readonly string[] SubscribedTopics =
    [
        Topics.AppointmentBooked,
        Topics.AppointmentCancelled,
        Topics.AppointmentCompleted,
        Topics.PrescriptionIssued,
        Topics.PrescriptionCancelled,
        Topics.KycSubmitted,
        Topics.KycReviewed
    ];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Yield immediately so the rest of the startup pipeline can continue.
        // Without this, the blocking Consume() call below would prevent the
        // app from finishing startup (MapControllers, Kestrel listen, etc.).
        await Task.Yield();

        var config = new ConsumerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            GroupId = "halen-notifications",
            AutoOffsetReset = AutoOffsetReset.Latest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(SubscribedTopics);
        logger.LogInformation("Kafka notification consumer started, subscribed to {Topics}", string.Join(", ", SubscribedTopics));

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(stoppingToken);

                    await handler.HandleAsync(result.Topic, result.Message.Value, stoppingToken);

                    consumer.Commit(result);
                }
                catch (ConsumeException ex)
                {
                    logger.LogError(ex, "Kafka consume error on topic {Topic}", ex.ConsumerRecord?.Topic);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Error handling notification message");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during graceful shutdown — stoppingToken was cancelled.
        }
        finally
        {
            consumer.Close();
            logger.LogInformation("Kafka notification consumer stopped");
        }
    }
}

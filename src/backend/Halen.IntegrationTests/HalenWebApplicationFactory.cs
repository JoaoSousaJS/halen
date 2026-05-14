using Confluent.Kafka;
using Halen.Application.Interfaces;
using Halen.Infrastructure.Messaging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Moq;
using Testcontainers.PostgreSql;

namespace Halen.IntegrationTests;

public class HalenWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithDatabase("halen_test")
        .WithUsername("halen")
        .WithPassword("halen_secret")
        .Build();

    public async Task StartAsync()
    {
        await _postgres.StartAsync();
    }

    public async Task StopAsync()
    {
        await _postgres.StopAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", _postgres.GetConnectionString());
        builder.UseSetting("Jwt:Secret", "halen-integration-test-super-secret-key-32chars!");
        builder.UseSetting("Jwt:Issuer", "halen-test");
        builder.UseSetting("Jwt:Audience", "halen-test");
        builder.UseSetting("Seed:AdminEmail", "admin@test.com");
        builder.UseSetting("Seed:AdminPassword", "Admin1234!");
        builder.UseSetting("Kafka:BootstrapServers", "localhost:9092");

        builder.ConfigureServices(services =>
        {
            // Replace real Kafka producer with a mock so tests don't need a broker
            services.RemoveAll<IProducer<string, string>>();
            services.AddSingleton(_ => Mock.Of<IProducer<string, string>>());

            // Replace real IEventBus with a no-op implementation
            services.RemoveAll<IEventBus>();
            services.AddScoped<IEventBus, NullEventBus>();

            // Replace real INotificationSender so SignalR isn't needed
            services.RemoveAll<INotificationSender>();
            services.AddSingleton<INotificationSender, NullNotificationSender>();

            // Remove the Kafka consumer — no broker in tests.
            // Targets only NotificationConsumerService, not other hosted services.
            var consumer = services.FirstOrDefault(d =>
                d.ImplementationType == typeof(NotificationConsumerService));
            if (consumer is not null)
                services.Remove(consumer);
        });
    }

    // ── Null event bus — discards all published events ────────────────────────
    private sealed class NullEventBus : IEventBus
    {
        public Task PublishAsync<T>(string topic, T message, CancellationToken ct = default)
            where T : class => Task.CompletedTask;
    }

    // ── Null notification sender — discards all notifications ────────────────
    private sealed class NullNotificationSender : INotificationSender
    {
        public Task SendToUserAsync(string userId, NotificationDto notification, CancellationToken ct = default)
            => Task.CompletedTask;
    }
}

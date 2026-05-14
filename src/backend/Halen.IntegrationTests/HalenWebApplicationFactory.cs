using Confluent.Kafka;
using Halen.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
        });
    }

    // ── Null event bus — discards all published events ────────────────────────
    private sealed class NullEventBus : IEventBus
    {
        public Task PublishAsync<T>(string topic, T message, CancellationToken ct = default)
            where T : class => Task.CompletedTask;
    }
}

---
name: kafka-realtime-dotnet
description: 'Patterns for event-driven real-time notifications with Kafka, SignalR, and .NET 8. Covers producers, consumers, domain events, hub design, and testing.'
---

# Kafka + SignalR Real-Time Patterns — .NET 8

Guidance for building event-driven, real-time notification features in a .NET 8 Clean Architecture project using `Confluent.Kafka` and ASP.NET Core SignalR.

---

## Architecture Overview

```
Command Handler → IEventBus (publish) → Kafka Topic
                                             ↓
                              BackgroundService (consume)
                                             ↓
                              IHubContext<NotificationHub> → SignalR → Browser
```

- **Kafka** handles backend-to-backend messaging (durable, replayable, decoupled).
- **SignalR** handles server-to-browser push (WebSocket with fallback to SSE/long-poll).
- A `BackgroundService` bridges the two: consumes Kafka, pushes to SignalR.

---

## 1. Domain Events

Define events as records in the **Application layer** (not Domain — they reference IDs, not entities).

```csharp
// Halen.Application/Events/
public record AppointmentBookedEvent(Guid AppointmentId, Guid PatientId, Guid DoctorId, DateTime ScheduledAt);
public record AppointmentCancelledEvent(Guid AppointmentId, Guid CancelledByUserId);
public record AppointmentCompletedEvent(Guid AppointmentId, Guid DoctorId, Guid PatientId);
```

### Topic naming convention

`{bounded-context}.{entity}.{verb-past-tense}`

Examples: `scheduling.appointment.booked`, `scheduling.appointment.cancelled`

Use dot-separated lowercase. One event type per topic keeps consumers simple.

---

## 2. Publishing from Command Handlers

Use the existing `IEventBus` abstraction. Publish **after** the database commit succeeds — never inside a transaction (the event would be visible to consumers before the data is committed).

```csharp
await db.SaveChangesAsync(ct);
await transaction.CommitAsync(ct);

await eventBus.PublishAsync("scheduling.appointment.booked",
    new AppointmentBookedEvent(appointment.Id, patientProfile.Id, request.DoctorId, request.ScheduledAt), ct);
```

### Kafka producer key strategy

Use a deterministic key so related events land on the same partition (preserves ordering):

```csharp
// In KafkaEventBus, use the entity ID as the key — not a random GUID
public async Task PublishAsync<T>(string topic, T message, string? key = null, CancellationToken ct = default)
{
    var json = JsonSerializer.Serialize(message);
    var kafkaMessage = new Message<string, string>
    {
        Key = key ?? Guid.NewGuid().ToString(),
        Value = json
    };
    await producer.ProduceAsync(topic, kafkaMessage, ct);
}
```

---

## 3. SignalR Hub

### Strongly-typed hub (preferred)

Define a client interface — gives compile-time safety and makes the hub testable:

```csharp
// Halen.Application/Interfaces/INotificationClient.cs
public interface INotificationClient
{
    Task ReceiveNotification(NotificationDto notification);
}

// Halen.API/Hubs/NotificationHub.cs
[Authorize]
public class NotificationHub : Hub<INotificationClient>
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier; // from JWT "sub" claim
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user-{userId}");
        await base.OnDisconnectedAsync(exception);
    }
}
```

### JWT auth for WebSockets

WebSocket connections cannot send headers after the initial handshake. The client passes the JWT as a query parameter, and the server extracts it:

```csharp
// In Program.cs, configure JwtBearerEvents
options.Events = new JwtBearerEvents
{
    OnMessageReceived = context =>
    {
        var accessToken = context.Request.Query["access_token"];
        var path = context.HttpContext.Request.Path;
        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
        {
            context.Token = accessToken;
        }
        return Task.CompletedTask;
    }
};

// Map the hub endpoint
app.MapHub<NotificationHub>("/hubs/notifications");
```

### User identifier mapping

SignalR uses `ClaimTypes.NameIdentifier` by default to set `Context.UserIdentifier`. Since we use `MapInboundClaims = false`, the claim is `"sub"` not the long URI. Register a custom provider:

```csharp
public class SubClaimUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
        => connection.User?.FindFirst("sub")?.Value;
}

// In Program.cs
builder.Services.AddSingleton<IUserIdProvider, SubClaimUserIdProvider>();
```

---

## 4. Kafka Consumer as BackgroundService

```csharp
public class NotificationConsumerService(
    IHubContext<NotificationHub, INotificationClient> hubContext,
    IOptions<KafkaConsumerOptions> options,
    ILogger<NotificationConsumerService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            GroupId = "halen-notification-consumer",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            EnableAutoOffsetStore = false,
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(options.Value.Topics);

        // Yield to unblock app startup — Consume() is a blocking call
        await Task.Yield();

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(ct);
                await HandleMessage(result, ct);
                consumer.Commit(result);
            }
            catch (ConsumeException ex)
            {
                logger.LogError(ex, "Kafka consume error on topic {Topic}", ex.ConsumerRecord?.Topic);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        consumer.Close(); // graceful leave-group + final offset commit
    }

    private async Task HandleMessage(ConsumeResult<string, string> result, CancellationToken ct)
    {
        // Route by topic to determine notification type and target user
        // Use IHubContext to push to the right user/group
        // IHubContext is thread-safe and designed for use outside hubs
    }
}
```

### Key decisions

- **`EnableAutoCommit = false`**: commit only after successful processing (at-least-once delivery).
- **`await Task.Yield()`**: the `Consume()` call blocks the thread. Without yielding first, the hosted service would block app startup.
- **`consumer.Close()`**: sends a LeaveGroup request so Kafka triggers immediate rebalance instead of waiting for `session.timeout.ms`.
- **Error handling**: log and skip poison messages, or produce to a dead-letter topic (`{original-topic}.dlq`).

---

## 5. React Client

```typescript
import { HubConnectionBuilder, LogLevel, HubConnection } from '@microsoft/signalr';

function useNotifications(token: string | null) {
  const [connection, setConnection] = useState<HubConnection | null>(null);

  useEffect(() => {
    if (!token) return;

    const conn = new HubConnectionBuilder()
      .withUrl('/hubs/notifications', {
        accessTokenFactory: () => token,
      })
      .withAutomaticReconnect([0, 2_000, 5_000, 10_000, 30_000])
      .configureLogging(LogLevel.Warning)
      .build();

    conn.on('ReceiveNotification', (notification: NotificationDto) => {
      // Update React Query cache, show toast, etc.
    });

    conn.start().catch(console.error);
    setConnection(conn);

    return () => { conn.stop(); };
  }, [token]);

  return connection;
}
```

### Reconnection strategy

`withAutomaticReconnect([0, 2000, 5000, 10000, 30000])` — escalating delays. After the last retry fails, the connection enters "Disconnected" state. Listen for `onreconnecting` and `onclose` to update UI.

---

## 6. Testing

### Unit test: SignalR hub

```csharp
var mockClients = new Mock<IHubCallerClients<INotificationClient>>();
var mockCaller = new Mock<INotificationClient>();
mockClients.Setup(c => c.Caller).Returns(mockCaller.Object);

var hub = new NotificationHub { Clients = mockClients.Object };
// invoke hub methods, verify mockCaller interactions
```

### Unit test: Kafka consumer message handling

Extract the `HandleMessage` logic into a testable service. Don't test `Consume()` in unit tests — that's an integration concern.

```csharp
var mockHub = new Mock<IHubContext<NotificationHub, INotificationClient>>();
var mockClient = new Mock<INotificationClient>();
mockHub.Setup(h => h.Clients.Group(It.IsAny<string>())).Returns(mockClient.Object);

var handler = new NotificationMessageHandler(mockHub.Object);
await handler.HandleAsync(message, CancellationToken.None);

mockClient.Verify(c => c.ReceiveNotification(It.IsAny<NotificationDto>()), Times.Once);
```

### Integration test: Kafka with Testcontainers

```csharp
// NuGet: Testcontainers.Kafka
private readonly KafkaContainer _kafka = new KafkaBuilder()
    .WithImage("confluentinc/cp-kafka:7.7.0")
    .Build();

[TestMethod]
public async Task Consumer_Receives_And_Forwards_Notification()
{
    await _kafka.StartAsync();
    // 1. Create producer pointing at _kafka.GetBootstrapAddress()
    // 2. Produce a test message
    // 3. Run consumer service with mock IHubContext
    // 4. Assert IHubContext received the notification
}
```

### Integration test: SignalR

Use `WebApplicationFactory` + `HubConnection` pointing at the test server:

```csharp
var client = _factory.CreateClient();
var hubConnection = new HubConnectionBuilder()
    .WithUrl($"{client.BaseAddress}hubs/notifications",
        opts => opts.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler())
    .Build();

await hubConnection.StartAsync();
```

---

## 7. Checklist for new event-driven features

1. Define the event record in `Halen.Application/Events/`
2. Publish from the command handler (after commit)
3. Subscribe in the consumer `BackgroundService`
4. Route to the correct SignalR user/group via `IHubContext`
5. Handle on the React client via the `useNotifications` hook
6. Unit test the message handler (mock `IHubContext`)
7. Integration test with Testcontainers Kafka

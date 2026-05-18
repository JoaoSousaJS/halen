using System.Net.Http.Json;
using FluentAssertions;
using Halen.Infrastructure.Persistence;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Halen.IntegrationTests.Messaging;

[TestClass]
public class ChatHubTests : IntegrationTestBase
{
    private static async Task<HubConnection> ConnectToChatHubAsync(HttpClient authenticatedClient)
    {
        var token = authenticatedClient.DefaultRequestHeaders.Authorization?.Parameter
            ?? throw new InvalidOperationException("Client has no Bearer token");

        var connection = new HubConnectionBuilder()
            .WithUrl(
                new Uri(Factory.Server.BaseAddress, "/hubs/chat"),
                opts =>
                {
                    opts.HttpMessageHandlerFactory = _ => Factory.Server.CreateHandler();
                    opts.AccessTokenProvider = () => Task.FromResult<string?>(token);
                })
            .Build();

        await connection.StartAsync();
        return connection;
    }

    private static async Task<(HttpClient PatientClient, HttpClient DoctorClient, Guid ThreadId)> SetupThreadAsync()
    {
        var (patientClient, _) = await PatientClientWithEmailAsync();
        var (doctorProfileId, doctorClient) = await CreateDoctorWithClientAsync();

        var bookResp = await patientClient.PostAsJsonAsync("/api/v1/appointments", new
        {
            DoctorId = doctorProfileId,
            ScheduledAt = DateTime.UtcNow.Date.AddDays(1).AddHours(10),
            Reason = "Chat hub test",
        });
        bookResp.EnsureSuccessStatusCode();
        var booking = await bookResp.Content.ReadFromJsonAsync<AppointmentIdResponse>();

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HalenDbContext>();
        var thread = await db.ConversationThreads
            .FirstAsync(t => t.AppointmentId == booking!.AppointmentId);

        return (patientClient, doctorClient, thread.Id);
    }

    // ── JoinThread ─────────────────────────────────────────────────────────

    [TestMethod]
    public async Task JoinThread_Participant_Succeeds()
    {
        var (patientClient, _, threadId) = await SetupThreadAsync();

        await using var hub = await ConnectToChatHubAsync(patientClient);

        var act = () => hub.InvokeAsync("JoinThread", threadId);

        await act.Should().NotThrowAsync();
    }

    [TestMethod]
    public async Task JoinThread_NonParticipant_ThrowsHubException()
    {
        var (_, _, threadId) = await SetupThreadAsync();
        var unrelated = await PatientClientAsync();

        await using var hub = await ConnectToChatHubAsync(unrelated);

        var act = () => hub.InvokeAsync("JoinThread", threadId);

        await act.Should().ThrowAsync<HubException>()
            .WithMessage("*not a participant*");
    }

    [TestMethod]
    public async Task JoinThread_InvalidThreadId_ThrowsHubException()
    {
        var patientClient = await PatientClientAsync();

        await using var hub = await ConnectToChatHubAsync(patientClient);

        var act = () => hub.InvokeAsync("JoinThread", Guid.NewGuid());

        await act.Should().ThrowAsync<HubException>()
            .WithMessage("*not found*");
    }

    // ── SendTyping ─────────────────────────────────────────────────────────

    [TestMethod]
    public async Task SendTyping_ParticipantTyping_OtherReceivesEvent()
    {
        var (patientClient, doctorClient, threadId) = await SetupThreadAsync();

        await using var patientHub = await ConnectToChatHubAsync(patientClient);
        await using var doctorHub = await ConnectToChatHubAsync(doctorClient);

        await patientHub.InvokeAsync("JoinThread", threadId);
        await doctorHub.InvokeAsync("JoinThread", threadId);

        var typingTcs = new TaskCompletionSource<Guid>();
        doctorHub.On<Guid, string>("UserTyping", (tid, _) => typingTcs.TrySetResult(tid));

        await patientHub.InvokeAsync("SendTyping", threadId);

        var result = await Task.WhenAny(typingTcs.Task, Task.Delay(5000));
        result.Should().Be(typingTcs.Task, "Doctor should receive typing event");
        typingTcs.Task.Result.Should().Be(threadId);
    }

    [TestMethod]
    public async Task SendTyping_RapidFire_ThrottlesTo3Seconds()
    {
        var (patientClient, doctorClient, threadId) = await SetupThreadAsync();

        await using var patientHub = await ConnectToChatHubAsync(patientClient);
        await using var doctorHub = await ConnectToChatHubAsync(doctorClient);

        await patientHub.InvokeAsync("JoinThread", threadId);
        await doctorHub.InvokeAsync("JoinThread", threadId);

        var typingCount = 0;
        doctorHub.On<Guid, string>("UserTyping", (_, _) => Interlocked.Increment(ref typingCount));

        await patientHub.InvokeAsync("SendTyping", threadId);
        await patientHub.InvokeAsync("SendTyping", threadId);
        await patientHub.InvokeAsync("SendTyping", threadId);

        await Task.Delay(1000);

        typingCount.Should().Be(1, "Server-side throttle should suppress rapid typing events");
    }

    // ── LeaveThread ────────────────────────────────────────────────────────

    [TestMethod]
    public async Task LeaveThread_AfterJoin_Succeeds()
    {
        var (patientClient, _, threadId) = await SetupThreadAsync();

        await using var hub = await ConnectToChatHubAsync(patientClient);
        await hub.InvokeAsync("JoinThread", threadId);

        var act = () => hub.InvokeAsync("LeaveThread", threadId);

        await act.Should().NotThrowAsync();
    }

    // ── DTOs ────────────────────────────────────────────────────────────────

    private sealed record AppointmentIdResponse(Guid AppointmentId);
}

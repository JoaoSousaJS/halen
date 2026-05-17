using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

namespace Halen.IntegrationTests;

internal static class HubHelpers
{
    public static async Task<HubConnection> ConnectToConsultationHubAsync(
        HalenWebApplicationFactory factory,
        HttpClient authenticatedClient)
    {
        var token = authenticatedClient.DefaultRequestHeaders.Authorization?.Parameter
            ?? throw new InvalidOperationException("Client has no Bearer token");

        var connection = new HubConnectionBuilder()
            .WithUrl(
                new Uri(factory.Server.BaseAddress, "/hubs/consultation"),
                opts =>
                {
                    opts.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                    opts.AccessTokenProvider = () => Task.FromResult<string?>(token);
                })
            .Build();

        await connection.StartAsync();
        return connection;
    }

    public static async Task<string> GetTokenAsync(HalenWebApplicationFactory factory, string email, string password)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { Email = email, Password = password });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<TokenResponse>();
        return body!.Token;
    }

    private sealed record TokenResponse(string Token);
}

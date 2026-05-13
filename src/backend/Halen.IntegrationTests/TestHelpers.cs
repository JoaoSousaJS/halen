using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Halen.IntegrationTests;

internal static class TestHelpers
{
    /// <summary>
    /// Creates an HttpClient with the Bearer token obtained by logging in with
    /// the supplied credentials. Throws if login does not return 200.
    /// </summary>
    public static async Task<HttpClient> GetBearerClientAsync(
        HalenWebApplicationFactory factory,
        string email,
        string password)
    {
        // Use a plain (unauthenticated) client just for the login call
        var loginClient = factory.CreateClient();

        var response = await loginClient.PostAsJsonAsync("/api/v1/auth/login", new
        {
            Email    = email,
            Password = password,
        });

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<TokenResponse>()
            ?? throw new InvalidOperationException("Login returned null body.");

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body.Token);

        return client;
    }

    private sealed record TokenResponse(string Token);
}

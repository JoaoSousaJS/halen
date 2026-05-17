using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Halen.IntegrationTests.Auth;

/// <summary>
/// Rate limit tests use a custom factory configuration (Auth limit = 3),
/// so they manage their own factory lifecycle instead of inheriting IntegrationTestBase.
/// </summary>
[TestClass]
public class RateLimitTests
{
    private static HalenWebApplicationFactory _factory = null!;
    private static HttpClient _client = null!;

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext _)
    {
        _factory = new HalenWebApplicationFactory();
        await _factory.StartAsync();
        _client = _factory
            .WithWebHostBuilder(b => b.UseSetting("RateLimit:Auth", "3"))
            .CreateClient();
    }

    [ClassCleanup]
    public static async Task ClassCleanup()
    {
        _client.Dispose();
        await _factory.StopAsync();
        await _factory.DisposeAsync();
    }

    [TestMethod]
    public async Task AuthEndpoints_ExceedRateLimit_Returns429()
    {
        var responses = new List<HttpResponseMessage>();
        for (var i = 0; i < 4; i++)
        {
            responses.Add(await _client.PostAsJsonAsync("/api/v1/auth/login", new
            {
                Email = $"ratelimit+{i}@test.com",
                Password = "Whatever123!",
            }));
        }

        var statusCodes = responses.Select(r => r.StatusCode).ToList();

        statusCodes.Count(c => c == HttpStatusCode.TooManyRequests).Should().BeGreaterThanOrEqualTo(1,
            "at least one request should be rejected when exceeding the rate limit of 3 per minute");
    }
}

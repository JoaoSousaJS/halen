using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Halen.Domain.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Halen.IntegrationTests.Auth;

[TestClass]
public class AuthControllerTests : IntegrationTestBase
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static object ValidPatientPayload(string email = "patient@test.com") => new
    {
        FirstName = "Jane",
        LastName  = "Doe",
        Email     = email,
        Password  = "Patient1234!",
        Role      = (int)UserRole.Patient,
    };

    // ── Tests ─────────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Register_WithValidPatient_Returns200WithToken()
    {
        var client = Factory.CreateClient();
        var uniqueEmail = $"patient+{Guid.NewGuid():N}@test.com";
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", ValidPatientPayload(uniqueEmail));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<TokenResponse>();
        body.Should().NotBeNull();
        body!.Token.Should().NotBeNullOrWhiteSpace();
    }

    [TestMethod]
    public async Task Register_WithDuplicateEmail_Returns400()
    {
        var client = Factory.CreateClient();
        var email = $"dupe+{Guid.NewGuid():N}@test.com";
        var payload = ValidPatientPayload(email);

        // First registration should succeed
        var first = await client.PostAsJsonAsync("/api/v1/auth/register", payload);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        // Second registration with same email should fail
        var second = await client.PostAsJsonAsync("/api/v1/auth/register", payload);
        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task Register_WithInvalidEmail_Returns400()
    {
        var client = Factory.CreateClient();
        var payload = new
        {
            FirstName = "Bad",
            LastName  = "Email",
            Email     = "not-an-email",
            Password  = "Patient1234!",
            Role      = (int)UserRole.Patient,
        };

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<ValidationErrorResponse>();
        body.Should().NotBeNull();
        body!.Errors.Should().NotBeNullOrEmpty();
    }

    [TestMethod]
    public async Task Login_WithValidCredentials_Returns200WithToken()
    {
        var client = Factory.CreateClient();
        var email = $"login+{Guid.NewGuid():N}@test.com";

        // Register first
        await client.PostAsJsonAsync("/api/v1/auth/register", ValidPatientPayload(email));

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            Email    = email,
            Password = "Patient1234!",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<TokenResponse>();
        body.Should().NotBeNull();
        body!.Token.Should().NotBeNullOrWhiteSpace();
    }

    [TestMethod]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var client = Factory.CreateClient();
        var email = $"wrongpwd+{Guid.NewGuid():N}@test.com";

        // Register first
        await client.PostAsJsonAsync("/api/v1/auth/register", ValidPatientPayload(email));

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            Email    = email,
            Password = "WrongPassword999!",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task Login_WithNonExistentUser_Returns401()
    {
        var client = Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            Email    = "nobody@nothere.com",
            Password = "Whatever123!",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Response DTOs ─────────────────────────────────────────────────────────

    private sealed record TokenResponse(string Token);

    private sealed record ValidationError(string Field, string Message);

    private sealed record ValidationErrorResponse(ValidationError[] Errors);
}

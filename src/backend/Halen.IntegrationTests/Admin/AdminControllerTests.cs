using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Halen.Domain.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Halen.IntegrationTests.Admin;

[TestClass]
public class AdminControllerTests
{
    private static HalenWebApplicationFactory _factory = null!;

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext _)
    {
        _factory = new HalenWebApplicationFactory();
        await _factory.StartAsync();
    }

    [ClassCleanup]
    public static async Task ClassCleanup()
    {
        await _factory.StopAsync();
        await _factory.DisposeAsync();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static object ValidDoctorPayload(string suffix = "") => new
    {
        FirstName         = "Dr",
        LastName          = "House",
        Email             = $"doctor{suffix}@test.com",
        Password          = "Doctor1234!",
        Specialty         = "Diagnostics",
        LicenseNumber     = $"LIC-{Guid.NewGuid().ToString("N")[..8]}",
        ConsultationFee   = 150.00m,
        YearsOfExperience = 10,
    };

    private static async Task<HttpClient> AdminClientAsync() =>
        await TestHelpers.GetBearerClientAsync(_factory, "admin@test.com", "Admin1234!");

    private static async Task<HttpClient> PatientClientAsync()
    {
        // Register a fresh patient and return an authenticated client for them
        var email    = $"patient+{Guid.NewGuid():N}@test.com";
        var anon     = _factory.CreateClient();

        await anon.PostAsJsonAsync("/api/v1/auth/register", new
        {
            FirstName = "Plain",
            LastName  = "Patient",
            Email     = email,
            Password  = "Patient1234!",
            Role      = (int)UserRole.Patient,
        });

        return await TestHelpers.GetBearerClientAsync(_factory, email, "Patient1234!");
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task CreateDoctor_AsAdmin_Returns200WithDoctorId()
    {
        var client  = await AdminClientAsync();
        var payload = ValidDoctorPayload($"+{Guid.NewGuid():N}");

        var response = await client.PostAsJsonAsync("/api/v1/admin/doctors", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<DoctorIdResponse>();
        body.Should().NotBeNull();
        body!.DoctorId.Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task CreateDoctor_AsPatient_Returns403()
    {
        var client  = await PatientClientAsync();
        var payload = ValidDoctorPayload($"+{Guid.NewGuid():N}");

        var response = await client.PostAsJsonAsync("/api/v1/admin/doctors", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [TestMethod]
    public async Task CreateDoctor_WithoutAuth_Returns401()
    {
        var client  = _factory.CreateClient();
        var payload = ValidDoctorPayload();

        var response = await client.PostAsJsonAsync("/api/v1/admin/doctors", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task CreateDoctor_WithInvalidData_Returns400()
    {
        var client = await AdminClientAsync();

        // Specialty is empty — should fail validation
        var payload = new
        {
            FirstName         = "Dr",
            LastName          = "Invalid",
            Email             = $"baddoctor+{Guid.NewGuid():N}@test.com",
            Password          = "Doctor1234!",
            Specialty         = "",
            LicenseNumber     = "LIC-000",
            ConsultationFee   = 100.00m,
            YearsOfExperience = 5,
        };

        var response = await client.PostAsJsonAsync("/api/v1/admin/doctors", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── List Users ─────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task ListUsers_AsAdmin_Returns200WithUsers()
    {
        var client = await AdminClientAsync();

        var response = await client.GetAsync("/api/v1/admin/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListUsersResponse>();
        body.Should().NotBeNull();
        body!.Users.Should().NotBeNull();
    }

    [TestMethod]
    public async Task ListUsers_FilterByRole_ReturnsOnlyMatchingRole()
    {
        // Create a doctor first so we have both roles
        var admin = await AdminClientAsync();
        await admin.PostAsJsonAsync("/api/v1/admin/doctors", ValidDoctorPayload($"+role{Guid.NewGuid():N}"));

        var response = await admin.GetAsync("/api/v1/admin/users?role=doctor");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListUsersResponse>();
        body.Should().NotBeNull();
        body!.Users.Should().OnlyContain(u => u.Role == "Doctor");
    }

    [TestMethod]
    public async Task ListUsers_SearchByName_ReturnsMatches()
    {
        var admin = await AdminClientAsync();

        // Register a patient with a known name
        var anon = _factory.CreateClient();
        var uniqueName = $"Zara{Guid.NewGuid():N}"[..10];
        await anon.PostAsJsonAsync("/api/v1/auth/register", new
        {
            FirstName = uniqueName,
            LastName = "Test",
            Email = $"{uniqueName.ToLower()}@test.com",
            Password = "Patient1234!",
            Role = (int)UserRole.Patient,
        });

        var response = await admin.GetAsync($"/api/v1/admin/users?search={uniqueName}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListUsersResponse>();
        body.Should().NotBeNull();
        body!.Users.Should().ContainSingle(u => u.Name.Contains(uniqueName));
    }

    [TestMethod]
    public async Task ListUsers_AsPatient_Returns403()
    {
        var client = await PatientClientAsync();

        var response = await client.GetAsync("/api/v1/admin/users");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [TestMethod]
    public async Task ListUsers_WithoutAuth_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/admin/users");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Response DTOs ─────────────────────────────────────────────────────────

    private sealed record DoctorIdResponse(Guid DoctorId);

    private sealed record AdminUserResponse(
        Guid Id,
        string Name,
        string Role,
        string Status,
        string? Plan,
        DateTime? LastLoginAt,
        bool IsFlagged);

    private sealed record ListUsersResponse(
        AdminUserResponse[] Users,
        int TotalCount);
}

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

    // ── Response DTOs ─────────────────────────────────────────────────────────

    private sealed record DoctorIdResponse(Guid DoctorId);
}

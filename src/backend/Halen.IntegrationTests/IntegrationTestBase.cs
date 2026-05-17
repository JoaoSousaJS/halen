using System.Net.Http.Json;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Halen.IntegrationTests;

/// <summary>
/// Base class for integration tests that share the factory lifecycle and common
/// client helpers (AdminClientAsync, PatientClientAsync, CreateDoctorWithClientAsync).
///
/// Derived classes should NOT define their own [ClassInitialize]/[ClassCleanup] unless
/// they need custom factory configuration (e.g., RateLimitTests).
/// </summary>
[TestClass]
public abstract class IntegrationTestBase
{
    private static HalenWebApplicationFactory _factory = null!;

    /// <summary>
    /// Exposes the factory for test classes that need direct access
    /// (e.g., to create a raw client without auth, or to access Services).
    /// </summary>
    protected static HalenWebApplicationFactory Factory => _factory;

    [ClassInitialize(InheritanceBehavior.BeforeEachDerivedClass)]
    public static async Task BaseClassInitialize(TestContext _)
    {
        _factory = new HalenWebApplicationFactory();
        await _factory.StartAsync();
    }

    [ClassCleanup(InheritanceBehavior.BeforeEachDerivedClass)]
    public static async Task BaseClassCleanup()
    {
        await _factory.StopAsync();
        await _factory.DisposeAsync();
    }

    // ── Shared Client Helpers ────────────────────────────────────────────────

    /// <summary>
    /// Returns an authenticated HttpClient for the seeded admin user.
    /// </summary>
    protected static async Task<HttpClient> AdminClientAsync() =>
        await TestHelpers.GetBearerClientAsync(_factory, "admin@test.com", "Admin1234!");

    /// <summary>
    /// Registers a fresh patient and returns an authenticated HttpClient.
    /// </summary>
    protected static async Task<HttpClient> PatientClientAsync()
    {
        var email = $"patient+{Guid.NewGuid():N}@test.com";
        var anon = _factory.CreateClient();

        var reg = await anon.PostAsJsonAsync("/api/v1/auth/register", new
        {
            FirstName = "Test",
            LastName = "Patient",
            Email = email,
            Password = "Patient1234!",
            Role = (int)UserRole.Patient,
        });
        reg.EnsureSuccessStatusCode();

        return await TestHelpers.GetBearerClientAsync(_factory, email, "Patient1234!");
    }

    /// <summary>
    /// Registers a fresh patient and returns the authenticated HttpClient and email.
    /// Useful when you need to reference the patient's email later.
    /// </summary>
    protected static async Task<(HttpClient Client, string Email)> PatientClientWithEmailAsync()
    {
        var email = $"patient+{Guid.NewGuid():N}@test.com";
        var anon = _factory.CreateClient();

        await anon.PostAsJsonAsync("/api/v1/auth/register", new
        {
            FirstName = "Test",
            LastName = "Patient",
            Email = email,
            Password = "Patient1234!",
            Role = (int)UserRole.Patient,
        });

        var client = await TestHelpers.GetBearerClientAsync(_factory, email, "Patient1234!");
        return (client, email);
    }

    /// <summary>
    /// Creates a doctor via the admin endpoint, optionally approves KYC (default: true),
    /// and returns the doctor profile ID plus an authenticated HttpClient.
    /// </summary>
    protected static async Task<(Guid DoctorProfileId, HttpClient Client)> CreateDoctorWithClientAsync(
        string lastName = "Test", bool approveKyc = true)
    {
        var (doctorId, client, _) = await CreateDoctorCoreAsync(lastName, approveKyc);
        return (doctorId, client);
    }

    /// <summary>
    /// Creates a doctor via the admin endpoint, optionally approves KYC (default: true),
    /// and returns the doctor profile ID, authenticated HttpClient, and the email.
    /// </summary>
    protected static async Task<(Guid DoctorProfileId, HttpClient Client, string Email)> CreateDoctorWithClientAndEmailAsync(
        string lastName = "Test", bool approveKyc = true)
    {
        return await CreateDoctorCoreAsync(lastName, approveKyc);
    }

    private static async Task<(Guid DoctorProfileId, HttpClient Client, string Email)> CreateDoctorCoreAsync(
        string lastName, bool approveKyc)
    {
        var admin = await AdminClientAsync();
        var email = $"doctor+{Guid.NewGuid():N}@test.com";
        var response = await admin.PostAsJsonAsync("/api/v1/admin/doctors", new
        {
            FirstName = "Dr",
            LastName = lastName,
            Email = email,
            Password = "Doctor1234!",
            Specialty = "General",
            LicenseNumber = $"LIC-{Guid.NewGuid().ToString("N")[..8]}",
            ConsultationFee = 100.00m,
            YearsOfExperience = 5,
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<DoctorIdResponse>();
        var doctorId = body!.DoctorId;
        if (approveKyc)
        {
            await TestHelpers.ApproveDoctorKycAsync(_factory, doctorId);
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Halen.Infrastructure.Persistence.HalenDbContext>();
            var clinic = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                .FirstAsync(db.Clinics, c => c.Slug == "default");

            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            {
                db.DoctorAvailabilities.Add(new DoctorAvailability
                {
                    DoctorProfileId = doctorId,
                    ClinicId = clinic.Id,
                    DayOfWeek = day,
                    StartTime = new TimeOnly(0, 0),
                    EndTime = new TimeOnly(23, 40),
                    SlotDurationMinutes = 20,
                    IsActive = true,
                });
            }
            await db.SaveChangesAsync();
        }

        var client = await TestHelpers.GetBearerClientAsync(_factory, email, "Doctor1234!");
        return (doctorId, client, email);
    }

    // ── Shared DTOs ──────────────────────��──────────────────────────────────

    protected sealed record DoctorIdResponse(Guid DoctorId);
}

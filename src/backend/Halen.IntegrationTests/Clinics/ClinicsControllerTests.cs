using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Halen.IntegrationTests.Clinics;

[TestClass]
public class ClinicsControllerTests : IntegrationTestBase
{
    [TestMethod]
    public async Task CreateClinic_AsAdmin_ReturnsCreated()
    {
        var client = await AdminClientAsync();
        var slug = $"clinic-{Guid.NewGuid():N}"[..20];

        var response = await client.PostAsJsonAsync("/api/v1/clinics", new
        {
            Name = "Test Clinic",
            Slug = slug,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ClinicIdResponse>();
        body!.ClinicId.Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task CreateClinic_DuplicateSlug_Returns400()
    {
        var client = await AdminClientAsync();
        var slug = $"dup-{Guid.NewGuid():N}"[..20];

        await client.PostAsJsonAsync("/api/v1/clinics", new { Name = "First", Slug = slug });
        var response = await client.PostAsJsonAsync("/api/v1/clinics", new { Name = "Second", Slug = slug });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task CreateClinic_AsPatient_Returns403()
    {
        var client = await PatientClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/clinics", new
        {
            Name = "Hacked",
            Slug = "hacked",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [TestMethod]
    public async Task ListClinics_AsAdmin_ReturnsResults()
    {
        var client = await AdminClientAsync();

        var response = await client.GetAsync("/api/v1/clinics");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListClinicsResponse>();
        body!.Clinics.Should().NotBeEmpty();
        body.TotalCount.Should().BeGreaterThan(0);
    }

    [TestMethod]
    public async Task UpdateClinic_DeactivateAndReactivate_ReturnsNoContent()
    {
        var client = await AdminClientAsync();
        var slug = $"upd-{Guid.NewGuid():N}"[..20];

        var createResp = await client.PostAsJsonAsync("/api/v1/clinics", new { Name = "Updatable", Slug = slug });
        var created = await createResp.Content.ReadFromJsonAsync<ClinicIdResponse>();

        var updateResp = await client.PutAsJsonAsync($"/api/v1/clinics/{created!.ClinicId}", new
        {
            Name = "Updated Name",
            IsActive = false,
        });

        updateResp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [TestMethod]
    public async Task SetFeatureFlag_Toggle_ReturnsNoContent()
    {
        var client = await AdminClientAsync();
        var slug = $"feat-{Guid.NewGuid():N}"[..20];

        var createResp = await client.PostAsJsonAsync("/api/v1/clinics", new { Name = "Feature Test", Slug = slug });
        var created = await createResp.Content.ReadFromJsonAsync<ClinicIdResponse>();

        var resp = await client.PutAsJsonAsync(
            $"/api/v1/clinics/{created!.ClinicId}/features/prescriptions",
            new { IsEnabled = true });

        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [TestMethod]
    public async Task GetMyFeatures_AsPatient_ReturnsFlags()
    {
        var client = await PatientClientAsync();

        var response = await client.GetAsync("/api/v1/me/features");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var flags = await response.Content.ReadFromJsonAsync<FeatureFlagDto[]>();
        flags.Should().NotBeNull();
    }

    [TestMethod]
    public async Task SetFeatureFlag_InvalidKey_Returns400()
    {
        var client = await AdminClientAsync();
        var slug = $"inv-{Guid.NewGuid():N}"[..20];

        var createResp = await client.PostAsJsonAsync("/api/v1/clinics", new { Name = "Invalid", Slug = slug });
        var created = await createResp.Content.ReadFromJsonAsync<ClinicIdResponse>();

        var resp = await client.PutAsJsonAsync(
            $"/api/v1/clinics/{created!.ClinicId}/features/nonexistent_feature",
            new { IsEnabled = true });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task DeactivateClinic_DoesNotSuspendPlatformAdmin()
    {
        var client = await AdminClientAsync();
        var slug = $"deact-{Guid.NewGuid():N}"[..20];

        var createResp = await client.PostAsJsonAsync("/api/v1/clinics", new { Name = "Deactivate Test", Slug = slug });
        createResp.EnsureSuccessStatusCode();
        var created = await createResp.Content.ReadFromJsonAsync<ClinicIdResponse>();
        var clinicId = Guid.Parse(created!.ClinicId);

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HalenDbContext>();
            var admin = await db.Users.FirstAsync(u => u.Email == "admin@test.com");
            admin.ClinicId = clinicId;
            await db.SaveChangesAsync();
        }

        var deactivateResp = await client.PutAsJsonAsync($"/api/v1/clinics/{clinicId}", new
        {
            Name = "Deactivate Test",
            IsActive = false,
        });
        deactivateResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HalenDbContext>();
            var admin = await db.Users.FirstAsync(u => u.Email == "admin@test.com");
            admin.Status.Should().Be(AccountStatus.Active);

            admin.ClinicId = (await db.Clinics.FirstAsync(c => c.Slug == "default")).Id;
            admin.Status = AccountStatus.Active;
            await db.SaveChangesAsync();
        }
    }

    [TestMethod]
    public async Task CreateClinicAdmin_AsAdmin_ReturnsCreated()
    {
        var client = await AdminClientAsync();
        var slug = $"adm-{Guid.NewGuid():N}"[..20];

        var createResp = await client.PostAsJsonAsync("/api/v1/clinics", new { Name = "Admin Test", Slug = slug });
        createResp.EnsureSuccessStatusCode();
        var created = await createResp.Content.ReadFromJsonAsync<ClinicIdResponse>();

        var resp = await client.PostAsJsonAsync($"/api/v1/clinics/{created!.ClinicId}/admins", new
        {
            Email = $"cadmin+{Guid.NewGuid():N}@test.com",
            FirstName = "Clinic",
            LastName = "Admin",
            TemporaryPassword = "ClinicAdmin1234!",
        });

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await resp.Content.ReadFromJsonAsync<UserIdResponse>();
        body!.UserId.Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task CreateClinicAdmin_AsPatient_Returns403()
    {
        var patient = await PatientClientAsync();

        var resp = await patient.PostAsJsonAsync($"/api/v1/clinics/{Guid.NewGuid()}/admins", new
        {
            Email = "blocked@test.com",
            FirstName = "Blocked",
            LastName = "User",
            TemporaryPassword = "Blocked1234!",
        });

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [TestMethod]
    public async Task CreateClinicAdmin_AsClinicAdmin_Returns403()
    {
        var admin = await AdminClientAsync();
        var slug = $"ca403-{Guid.NewGuid():N}"[..20];
        var createResp = await admin.PostAsJsonAsync("/api/v1/clinics", new { Name = "CA403", Slug = slug });
        createResp.EnsureSuccessStatusCode();
        var created = await createResp.Content.ReadFromJsonAsync<ClinicIdResponse>();
        var clinicId = Guid.Parse(created!.ClinicId);

        var clinicAdminClient = await CreateClinicAdminClientAsync(clinicId);

        var resp = await clinicAdminClient.PostAsJsonAsync($"/api/v1/clinics/{clinicId}/admins", new
        {
            Email = "another@test.com",
            FirstName = "Another",
            LastName = "Admin",
            TemporaryPassword = "Another1234!",
        });

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [TestMethod]
    public async Task CreateClinicAdmin_NonExistentClinic_Returns404()
    {
        var client = await AdminClientAsync();

        var resp = await client.PostAsJsonAsync($"/api/v1/clinics/{Guid.NewGuid()}/admins", new
        {
            Email = "ghost@test.com",
            FirstName = "Ghost",
            LastName = "Clinic",
            TemporaryPassword = "Ghost1234!",
        });

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task CreateClinicAdmin_InvalidEmail_Returns400()
    {
        var client = await AdminClientAsync();
        var slug = $"inv-{Guid.NewGuid():N}"[..20];

        var createResp = await client.PostAsJsonAsync("/api/v1/clinics", new { Name = "Invalid Email", Slug = slug });
        var created = await createResp.Content.ReadFromJsonAsync<ClinicIdResponse>();

        var resp = await client.PostAsJsonAsync($"/api/v1/clinics/{created!.ClinicId}/admins", new
        {
            Email = "not-an-email",
            FirstName = "Bad",
            LastName = "Email",
            TemporaryPassword = "Password1234!",
        });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task CreateClinicAdmin_DuplicateEmail_Returns400()
    {
        var client = await AdminClientAsync();
        var slug = $"dup-{Guid.NewGuid():N}"[..20];
        var email = $"dupca+{Guid.NewGuid():N}@test.com";

        var createResp = await client.PostAsJsonAsync("/api/v1/clinics", new { Name = "Dup Email", Slug = slug });
        var created = await createResp.Content.ReadFromJsonAsync<ClinicIdResponse>();

        await client.PostAsJsonAsync($"/api/v1/clinics/{created!.ClinicId}/admins", new
        {
            Email = email,
            FirstName = "First",
            LastName = "Admin",
            TemporaryPassword = "First1234!",
        });

        var resp = await client.PostAsJsonAsync($"/api/v1/clinics/{created.ClinicId}/admins", new
        {
            Email = email,
            FirstName = "Second",
            LastName = "Admin",
            TemporaryPassword = "Second1234!",
        });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task CreateClinicAdmin_InactiveClinic_Returns400()
    {
        var client = await AdminClientAsync();
        var slug = $"inact-{Guid.NewGuid():N}"[..20];

        var createResp = await client.PostAsJsonAsync("/api/v1/clinics", new { Name = "Inactive", Slug = slug });
        var created = await createResp.Content.ReadFromJsonAsync<ClinicIdResponse>();

        await client.PutAsJsonAsync($"/api/v1/clinics/{created!.ClinicId}", new
        {
            Name = "Inactive",
            IsActive = false,
        });

        var resp = await client.PostAsJsonAsync($"/api/v1/clinics/{created.ClinicId}/admins", new
        {
            Email = $"inactive+{Guid.NewGuid():N}@test.com",
            FirstName = "Inactive",
            LastName = "Admin",
            TemporaryPassword = "Inactive1234!",
        });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task CreateClinicAdmin_VerifyCorrectClinicId()
    {
        var client = await AdminClientAsync();
        var slug = $"verify-{Guid.NewGuid():N}"[..20];
        var email = $"verify+{Guid.NewGuid():N}@test.com";

        var createResp = await client.PostAsJsonAsync("/api/v1/clinics", new { Name = "Verify", Slug = slug });
        var created = await createResp.Content.ReadFromJsonAsync<ClinicIdResponse>();
        var clinicId = Guid.Parse(created!.ClinicId);

        await client.PostAsJsonAsync($"/api/v1/clinics/{clinicId}/admins", new
        {
            Email = email,
            FirstName = "Verify",
            LastName = "Admin",
            TemporaryPassword = "Verify1234!",
        });

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HalenDbContext>();
        var user = await db.Users.FirstAsync(u => u.Email == email);
        user.ClinicId.Should().Be(clinicId);
        user.Role.Should().Be(UserRole.ClinicAdmin);
        user.Status.Should().Be(AccountStatus.Active);
    }

    private static async Task<HttpClient> CreateClinicAdminClientAsync(Guid clinicId)
    {
        var email = $"clinicadmin+{Guid.NewGuid():N}@test.com";

        using (var scope = Factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<User>>();
            var user = new User
            {
                Email = email,
                UserName = email,
                FirstName = "Clinic",
                LastName = "Admin",
                ClinicId = clinicId,
                Role = UserRole.ClinicAdmin,
                Status = AccountStatus.Active,
            };
            await userManager.CreateAsync(user, "ClinicAdmin1234!");
            await userManager.AddToRoleAsync(user, "ClinicAdmin");
        }

        return await TestHelpers.GetBearerClientAsync(Factory, email, "ClinicAdmin1234!");
    }

    private sealed record ClinicIdResponse(string ClinicId);
    private sealed record ListClinicsResponse(ClinicDto[] Clinics, int TotalCount);
    private sealed record ClinicDto(string Id, string Name, string Slug, bool IsActive, string CreatedAt);
    private sealed record FeatureFlagDto(string FeatureKey, bool IsEnabled);
    private sealed record UserIdResponse(Guid UserId);
}

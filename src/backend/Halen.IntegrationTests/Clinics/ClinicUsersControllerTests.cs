using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Halen.IntegrationTests.Clinics;

[TestClass]
public class ClinicUsersControllerTests : IntegrationTestBase
{
    [TestMethod]
    public async Task CreateUser_AsAdmin_ReturnsCreated()
    {
        var client = await AdminClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/clinic/users", new
        {
            Email = $"newuser+{Guid.NewGuid():N}@test.com",
            FirstName = "New",
            LastName = "User",
            TemporaryPassword = "Temp1234!",
            Role = 0,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<UserIdResponse>();
        body!.UserId.Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task CreateUser_AsPatient_Returns403()
    {
        var client = await PatientClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/clinic/users", new
        {
            Email = "hacked@test.com",
            FirstName = "H",
            LastName = "H",
            TemporaryPassword = "Temp1234!",
            Role = 0,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [TestMethod]
    public async Task ListUsers_AsAdmin_ReturnsResults()
    {
        var client = await AdminClientAsync();

        var response = await client.GetAsync("/api/v1/clinic/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListUsersResponse>();
        body!.Users.Should().NotBeNull();
        body.TotalCount.Should().BeGreaterThanOrEqualTo(0);
    }

    [TestMethod]
    public async Task CreateUser_InvalidEmail_Returns400()
    {
        var client = await AdminClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/clinic/users", new
        {
            Email = "not-an-email",
            FirstName = "Bad",
            LastName = "Email",
            TemporaryPassword = "Temp1234!",
            Role = 0,
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task CreateUser_AsClinicAdmin_ReturnsCreated()
    {
        var clinicAdminEmail = $"cadmin+{Guid.NewGuid():N}@test.com";

        using (var scope = Factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider
                .GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Halen.Domain.Entities.User>>();
            var db = scope.ServiceProvider.GetRequiredService<Halen.Infrastructure.Persistence.HalenDbContext>();

            var defaultClinic = await db.Clinics.FirstAsync(c => c.Slug == "default");

            var user = new Halen.Domain.Entities.User
            {
                Id = Guid.NewGuid(),
                FirstName = "Clinic",
                LastName = "Admin",
                Email = clinicAdminEmail,
                UserName = clinicAdminEmail,
                Role = Halen.Domain.Enums.UserRole.ClinicAdmin,
                ClinicId = defaultClinic.Id,
                Status = Halen.Domain.Enums.AccountStatus.Active,
            };
            await userManager.CreateAsync(user, "Admin1234!");
            await userManager.AddToRoleAsync(user, "ClinicAdmin");
        }

        var client = await TestHelpers.GetBearerClientAsync(Factory, clinicAdminEmail, "Admin1234!");

        var response = await client.PostAsJsonAsync("/api/v1/clinic/users", new
        {
            Email = $"newuser+{Guid.NewGuid():N}@test.com",
            FirstName = "Created",
            LastName = "ByClinicAdmin",
            TemporaryPassword = "Temp1234!",
            Role = 0,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private sealed record UserIdResponse(string UserId);
    private sealed record ListUsersResponse(UserDto[] Users, int TotalCount);
    private sealed record UserDto(string Id, string Name, string Email, string Role, string Status, string CreatedAt);
}

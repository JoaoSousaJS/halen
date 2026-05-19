using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Halen.Domain.Entities;
using Halen.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Halen.IntegrationTests.AuditTrail;

[TestClass]
public class AuditLogControllerTests : IntegrationTestBase
{
    [TestMethod]
    public async Task Search_AsAdmin_Returns200WithPaginatedResults()
    {
        var admin = await AdminClientAsync();
        await SeedAuditLogAsync("TestAction");

        var response = await admin.GetAsync("/api/v1/audit-logs?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuditLogsResponse>();
        body!.Logs.Should().NotBeEmpty();
        body.TotalCount.Should().BeGreaterThan(0);
    }

    [TestMethod]
    public async Task Search_AsPatient_Returns403()
    {
        var patient = await PatientClientAsync();

        var response = await patient.GetAsync("/api/v1/audit-logs");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [TestMethod]
    public async Task Search_AsDoctor_Returns403()
    {
        var (_, doctor) = await CreateDoctorWithClientAsync();

        var response = await doctor.GetAsync("/api/v1/audit-logs");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [TestMethod]
    public async Task Search_WithFeatureDisabled_Returns403()
    {
        var admin = await AdminClientAsync();
        var slug = $"nofeat-{Guid.NewGuid():N}"[..20];
        var createResp = await admin.PostAsJsonAsync("/api/v1/clinics", new { Name = "No Audit Clinic", Slug = slug });
        createResp.EnsureSuccessStatusCode();
        var created = await createResp.Content.ReadFromJsonAsync<ClinicIdResponseDto>();
        var clinicId = Guid.Parse(created!.ClinicId);

        var clinicAdmin = await CreateClinicAdminAsync(clinicId, enableAudit: false);

        var response = await clinicAdmin.GetAsync("/api/v1/audit-logs");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [TestMethod]
    public async Task Search_WithClinicIdFilter_NonPlatformAdmin_Returns400()
    {
        var admin = await AdminClientAsync();

        var slug = $"audit-{Guid.NewGuid():N}"[..20];
        var createResp = await admin.PostAsJsonAsync("/api/v1/clinics", new { Name = "Audit Test Clinic", Slug = slug });
        createResp.EnsureSuccessStatusCode();
        var created = await createResp.Content.ReadFromJsonAsync<ClinicIdResponseDto>();

        var clinicAdmin = await CreateClinicAdminAsync(Guid.Parse(created!.ClinicId));

        var response = await clinicAdmin.GetAsync($"/api/v1/audit-logs?clinicId={created.ClinicId}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task Export_AsAdmin_ReturnsCsvFile()
    {
        var admin = await AdminClientAsync();
        await SeedAuditLogAsync("ExportTest");

        var from = DateTime.UtcNow.AddDays(-1).ToString("o");
        var response = await admin.GetAsync($"/api/v1/audit-logs/export?from={from}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Timestamp,ActorName,Action,TargetId,IpAddress,Metadata");
    }

    [TestMethod]
    public async Task Export_WithoutFromDate_Returns400()
    {
        var admin = await AdminClientAsync();

        var response = await admin.GetAsync("/api/v1/audit-logs/export");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task BookAppointment_CreatesAuditLog_VisibleViaSearch()
    {
        var admin = await AdminClientAsync();
        var patient = await PatientClientAsync();
        var (doctorId, _) = await CreateDoctorWithClientAsync();

        var bookResp = await patient.PostAsJsonAsync("/api/v1/appointments", new
        {
            DoctorId = doctorId,
            ScheduledAt = DateTime.UtcNow.Date.AddDays(7).AddHours(10),
            Reason = "Audit trail test"
        });
        bookResp.EnsureSuccessStatusCode();

        var response = await admin.GetAsync("/api/v1/audit-logs?action=BookAppointment&page=1&pageSize=5");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<AuditLogsResponse>();

        body!.Logs.Should().Contain(l => l.Action == "BookAppointment");
    }

    private static async Task SeedAuditLogAsync(string action)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HalenDbContext>();
        var clinic = await db.Clinics.FirstAsync(c => c.Slug == "default");
        db.AuditLogs.Add(new AuditLog
        {
            ClinicId = clinic.Id,
            ActorId = Guid.NewGuid(),
            ActorName = "Test Actor",
            Action = action,
            TargetId = Guid.NewGuid().ToString(),
            IpAddress = "127.0.0.1",
            Metadata = null
        });
        await db.SaveChangesAsync();
    }

    private static async Task<HttpClient> CreateClinicAdminAsync(Guid clinicId, bool enableAudit = true)
    {
        var email = $"clinicadmin+{Guid.NewGuid():N}@test.com";

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HalenDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<User>>();

            var user = new User
            {
                Email = email,
                UserName = email,
                FirstName = "Clinic",
                LastName = "Admin",
                ClinicId = clinicId,
                Status = Domain.Enums.AccountStatus.Active
            };
            await userManager.CreateAsync(user, "ClinicAdmin1234!");
            await userManager.AddToRoleAsync(user, "ClinicAdmin");

            var flag = await db.ClinicFeatureFlags
                .FirstOrDefaultAsync(f => f.ClinicId == clinicId && f.FeatureKey == "audit_trail");
            if (flag is null)
            {
                db.ClinicFeatureFlags.Add(new Domain.Entities.ClinicFeatureFlag
                    { ClinicId = clinicId, FeatureKey = "audit_trail", IsEnabled = enableAudit });
                await db.SaveChangesAsync();
            }
            else if (flag.IsEnabled != enableAudit)
            {
                flag.IsEnabled = enableAudit;
                await db.SaveChangesAsync();
            }
        }

        return await TestHelpers.GetBearerClientAsync(Factory, email, "ClinicAdmin1234!");
    }

    private sealed record AuditLogsResponse(AuditLogItem[] Logs, int TotalCount);
    private sealed record AuditLogItem(
        Guid Id, DateTime Timestamp, Guid ActorId, string ActorName,
        string Action, string TargetId, string? Metadata, string IpAddress);
    private sealed record ClinicIdResponseDto(string ClinicId);
}

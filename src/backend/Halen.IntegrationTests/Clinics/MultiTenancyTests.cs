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
public class MultiTenancyTests : IntegrationTestBase
{
    private static async Task<(HttpClient Client, string Email)> CreateClinicPatientAsync(Guid clinicId)
    {
        var email = $"patient+{Guid.NewGuid():N}@test.com";

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HalenDbContext>();

        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Tenant",
            LastName = "Patient",
            Email = email,
            UserName = email,
            NormalizedEmail = email.ToUpperInvariant(),
            NormalizedUserName = email.ToUpperInvariant(),
            Role = UserRole.Patient,
            ClinicId = clinicId,
            Status = AccountStatus.Active,
            EmailConfirmed = true,
        };

        var userManager = scope.ServiceProvider
            .GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<User>>();
        await userManager.CreateAsync(user, "Patient1234!");
        await userManager.AddToRoleAsync(user, "Patient");

        db.PatientProfiles.Add(new PatientProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            ClinicId = clinicId,
        });
        await db.SaveChangesAsync();

        var client = await TestHelpers.GetBearerClientAsync(Factory, email, "Patient1234!");
        return (client, email);
    }

    [TestMethod]
    public async Task FeatureFlagMiddleware_DisabledFlag_Returns403()
    {
        var admin = await AdminClientAsync();
        var slug = $"nofeat-{Guid.NewGuid():N}"[..20];

        var createResp = await admin.PostAsJsonAsync("/api/v1/clinics", new { Name = "No Features", Slug = slug });
        createResp.EnsureSuccessStatusCode();
        var created = await createResp.Content.ReadFromJsonAsync<ClinicIdResponse>();
        var clinicId = Guid.Parse(created!.ClinicId);

        var (patient, _) = await CreateClinicPatientAsync(clinicId);

        var response = await patient.GetAsync("/api/v1/prescriptions");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [TestMethod]
    public async Task FeatureFlagMiddleware_EnabledFlag_AllowsAccess()
    {
        var admin = await AdminClientAsync();
        var slug = $"feat-{Guid.NewGuid():N}"[..20];

        var createResp = await admin.PostAsJsonAsync("/api/v1/clinics", new { Name = "With Features", Slug = slug });
        createResp.EnsureSuccessStatusCode();
        var created = await createResp.Content.ReadFromJsonAsync<ClinicIdResponse>();
        var clinicId = Guid.Parse(created!.ClinicId);

        await admin.PutAsJsonAsync(
            $"/api/v1/clinics/{clinicId}/features/prescriptions",
            new { IsEnabled = true });

        var (patient, _) = await CreateClinicPatientAsync(clinicId);

        var response = await patient.GetAsync("/api/v1/prescriptions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [TestMethod]
    public async Task CrossTenantIsolation_PatientCannotSeeOtherClinicAppointments()
    {
        var admin = await AdminClientAsync();

        var slugA = $"tena-{Guid.NewGuid():N}"[..20];
        var slugB = $"tenb-{Guid.NewGuid():N}"[..20];

        var respA = await admin.PostAsJsonAsync("/api/v1/clinics", new { Name = "Clinic A", Slug = slugA });
        var respB = await admin.PostAsJsonAsync("/api/v1/clinics", new { Name = "Clinic B", Slug = slugB });
        respA.EnsureSuccessStatusCode();
        respB.EnsureSuccessStatusCode();

        var clinicAId = Guid.Parse((await respA.Content.ReadFromJsonAsync<ClinicIdResponse>())!.ClinicId);
        var clinicBId = Guid.Parse((await respB.Content.ReadFromJsonAsync<ClinicIdResponse>())!.ClinicId);

        var (patientA, _) = await CreateClinicPatientAsync(clinicAId);
        var (patientB, _) = await CreateClinicPatientAsync(clinicBId);

        // Seed an appointment in Clinic A so we have real data to prove filtering
        var appointmentId = Guid.NewGuid();
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HalenDbContext>();

            var doctorUser = new User
            {
                Id = Guid.NewGuid(),
                FirstName = "Doc",
                LastName = "A",
                Email = $"doc+{Guid.NewGuid():N}@test.com",
                UserName = $"doc+{Guid.NewGuid():N}@test.com",
                Role = UserRole.Doctor,
                ClinicId = clinicAId,
                Status = AccountStatus.Active,
            };
            db.Users.Add(doctorUser);

            var doctorProfile = new DoctorProfile
            {
                Id = Guid.NewGuid(),
                UserId = doctorUser.Id,
                ClinicId = clinicAId,
                KycStatus = KycStatus.Approved,
            };
            db.DoctorProfiles.Add(doctorProfile);

            var patientProfile = await db.PatientProfiles
                .FirstAsync(p => p.ClinicId == clinicAId);

            db.Appointments.Add(new Appointment
            {
                Id = appointmentId,
                ClinicId = clinicAId,
                PatientId = patientProfile.Id,
                DoctorId = doctorProfile.Id,
                ScheduledAt = DateTime.UtcNow.AddDays(1),
                Reason = "Cross-tenant test",
            });
            await db.SaveChangesAsync();
        }

        // Patient A should see the appointment
        var appointmentsA = await patientA.GetAsync("/api/v1/appointments");
        appointmentsA.StatusCode.Should().Be(HttpStatusCode.OK);
        var bodyA = await appointmentsA.Content.ReadAsStringAsync();
        bodyA.Should().Contain(appointmentId.ToString());

        // Patient B (different clinic) should NOT see it
        var appointmentsB = await patientB.GetAsync("/api/v1/appointments");
        appointmentsB.StatusCode.Should().Be(HttpStatusCode.OK);
        var bodyB = await appointmentsB.Content.ReadAsStringAsync();
        bodyB.Should().NotContain(appointmentId.ToString());
    }

    [TestMethod]
    public async Task DeactivateClinic_SuspendsActiveUsers()
    {
        var admin = await AdminClientAsync();
        var slug = $"deac-{Guid.NewGuid():N}"[..20];

        var createResp = await admin.PostAsJsonAsync("/api/v1/clinics", new { Name = "Deactivatable", Slug = slug });
        createResp.EnsureSuccessStatusCode();
        var created = await createResp.Content.ReadFromJsonAsync<ClinicIdResponse>();
        var clinicId = Guid.Parse(created!.ClinicId);

        var (_, patientEmail) = await CreateClinicPatientAsync(clinicId);

        var deactivateResp = await admin.PutAsJsonAsync($"/api/v1/clinics/{clinicId}", new
        {
            Name = "Deactivatable",
            IsActive = false,
        });
        deactivateResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HalenDbContext>();
        var user = await db.Users
            .IgnoreQueryFilters()
            .FirstAsync(u => u.Email == patientEmail);

        user.Status.Should().Be(AccountStatus.Suspended);
    }

    private sealed record ClinicIdResponse(string ClinicId);
}

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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

    [TestMethod]
    public async Task CrossTenantIsolation_DoctorCannotSeePrescriptionsFromOtherClinic()
    {
        var admin = await AdminClientAsync();

        // Create two clinics
        var slugA = $"rxta-{Guid.NewGuid():N}"[..20];
        var slugB = $"rxtb-{Guid.NewGuid():N}"[..20];

        var respA = await admin.PostAsJsonAsync("/api/v1/clinics", new { Name = "Rx Clinic A", Slug = slugA });
        var respB = await admin.PostAsJsonAsync("/api/v1/clinics", new { Name = "Rx Clinic B", Slug = slugB });
        respA.EnsureSuccessStatusCode();
        respB.EnsureSuccessStatusCode();

        var clinicAId = Guid.Parse((await respA.Content.ReadFromJsonAsync<ClinicIdResponse>())!.ClinicId);
        var clinicBId = Guid.Parse((await respB.Content.ReadFromJsonAsync<ClinicIdResponse>())!.ClinicId);

        // Enable prescriptions feature for both clinics
        await admin.PutAsJsonAsync($"/api/v1/clinics/{clinicAId}/features/prescriptions", new { IsEnabled = true });
        await admin.PutAsJsonAsync($"/api/v1/clinics/{clinicBId}/features/prescriptions", new { IsEnabled = true });

        // Create a doctor + patient in Clinic A with a prescription
        var prescriptionId = Guid.NewGuid();
        Guid doctorBUserId;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HalenDbContext>();
            var userManager = scope.ServiceProvider
                .GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<User>>();

            // Doctor in Clinic A
            var doctorAUser = new User
            {
                Id = Guid.NewGuid(),
                FirstName = "DocA",
                LastName = "TenantTest",
                Email = $"doca+{Guid.NewGuid():N}@test.com",
                UserName = $"doca+{Guid.NewGuid():N}@test.com",
                Role = UserRole.Doctor,
                ClinicId = clinicAId,
                Status = AccountStatus.Active,
                EmailConfirmed = true,
            };
            await userManager.CreateAsync(doctorAUser, "Doctor1234!");
            await userManager.AddToRoleAsync(doctorAUser, "Doctor");

            var doctorAProfile = new DoctorProfile
            {
                Id = Guid.NewGuid(),
                UserId = doctorAUser.Id,
                ClinicId = clinicAId,
                KycStatus = KycStatus.Approved,
                Specialty = "General",
                LicenseNumber = $"LIC-{Guid.NewGuid().ToString("N")[..8]}",
            };
            db.DoctorProfiles.Add(doctorAProfile);

            // Patient in Clinic A
            var patientUser = new User
            {
                Id = Guid.NewGuid(),
                FirstName = "PatA",
                LastName = "TenantTest",
                Email = $"pata+{Guid.NewGuid():N}@test.com",
                UserName = $"pata+{Guid.NewGuid():N}@test.com",
                Role = UserRole.Patient,
                ClinicId = clinicAId,
                Status = AccountStatus.Active,
                EmailConfirmed = true,
            };
            await userManager.CreateAsync(patientUser, "Patient1234!");
            await userManager.AddToRoleAsync(patientUser, "Patient");

            var patientProfile = new PatientProfile
            {
                Id = Guid.NewGuid(),
                UserId = patientUser.Id,
                ClinicId = clinicAId,
            };
            db.PatientProfiles.Add(patientProfile);

            // Prescription in Clinic A
            db.Prescriptions.Add(new Prescription
            {
                Id = prescriptionId,
                ClinicId = clinicAId,
                DoctorId = doctorAProfile.Id,
                PatientId = patientProfile.Id,
                DrugName = "CrossTenantDrug",
                Dosage = "100mg",
                Frequency = "Daily",
                RefillsRemaining = 1,
                Status = PrescriptionStatus.Active,
            });

            // Doctor in Clinic B
            var doctorBEmail = $"docb+{Guid.NewGuid():N}@test.com";
            var doctorBUser = new User
            {
                Id = Guid.NewGuid(),
                FirstName = "DocB",
                LastName = "TenantTest",
                Email = doctorBEmail,
                UserName = doctorBEmail,
                NormalizedEmail = doctorBEmail.ToUpperInvariant(),
                NormalizedUserName = doctorBEmail.ToUpperInvariant(),
                Role = UserRole.Doctor,
                ClinicId = clinicBId,
                Status = AccountStatus.Active,
                EmailConfirmed = true,
            };
            await userManager.CreateAsync(doctorBUser, "Doctor1234!");
            await userManager.AddToRoleAsync(doctorBUser, "Doctor");
            doctorBUserId = doctorBUser.Id;

            db.DoctorProfiles.Add(new DoctorProfile
            {
                Id = Guid.NewGuid(),
                UserId = doctorBUser.Id,
                ClinicId = clinicBId,
                KycStatus = KycStatus.Approved,
                Specialty = "General",
                LicenseNumber = $"LIC-{Guid.NewGuid().ToString("N")[..8]}",
            });

            await db.SaveChangesAsync();
        }

        // Doctor B should NOT see prescription from Clinic A
        var doctorBEmail2 = await GetUserEmailAsync(doctorBUserId);
        var doctorBClient = await TestHelpers.GetBearerClientAsync(Factory, doctorBEmail2, "Doctor1234!");

        var prescriptions = await doctorBClient.GetAsync("/api/v1/prescriptions");
        prescriptions.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await prescriptions.Content.ReadAsStringAsync();
        body.Should().NotContain(prescriptionId.ToString());
    }

    [TestMethod]
    public async Task CrossTenantIsolation_ClinicAdminCannotSeeUsersFromOtherClinic()
    {
        var admin = await AdminClientAsync();

        // Create two clinics
        var slugA = $"adma-{Guid.NewGuid():N}"[..20];
        var slugB = $"admb-{Guid.NewGuid():N}"[..20];

        var respA = await admin.PostAsJsonAsync("/api/v1/clinics", new { Name = "Admin Clinic A", Slug = slugA });
        var respB = await admin.PostAsJsonAsync("/api/v1/clinics", new { Name = "Admin Clinic B", Slug = slugB });
        respA.EnsureSuccessStatusCode();
        respB.EnsureSuccessStatusCode();

        var clinicAId = Guid.Parse((await respA.Content.ReadFromJsonAsync<ClinicIdResponse>())!.ClinicId);
        var clinicBId = Guid.Parse((await respB.Content.ReadFromJsonAsync<ClinicIdResponse>())!.ClinicId);

        // Create a ClinicAdmin in Clinic B
        var clinicAdminEmail = $"cadmin+{Guid.NewGuid():N}@test.com";
        using (var scope = Factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider
                .GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<User>>();

            var clinicAdminUser = new User
            {
                Id = Guid.NewGuid(),
                FirstName = "ClinicAdmin",
                LastName = "B",
                Email = clinicAdminEmail,
                UserName = clinicAdminEmail,
                NormalizedEmail = clinicAdminEmail.ToUpperInvariant(),
                NormalizedUserName = clinicAdminEmail.ToUpperInvariant(),
                Role = UserRole.ClinicAdmin,
                ClinicId = clinicBId,
                Status = AccountStatus.Active,
                EmailConfirmed = true,
            };
            await userManager.CreateAsync(clinicAdminUser, "Admin1234!");
            await userManager.AddToRoleAsync(clinicAdminUser, "ClinicAdmin");
        }

        // Create a patient in Clinic A
        var (_, patientAEmail) = await CreateClinicPatientAsync(clinicAId);

        // ClinicAdmin in Clinic B should not see users from Clinic A
        var clinicAdminClient = await TestHelpers.GetBearerClientAsync(Factory, clinicAdminEmail, "Admin1234!");
        var usersResp = await clinicAdminClient.GetAsync("/api/v1/clinic/users");
        usersResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var usersBody = await usersResp.Content.ReadAsStringAsync();
        usersBody.Should().NotContain(patientAEmail);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static async Task<string> GetUserEmailAsync(Guid userId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HalenDbContext>();
        var user = await db.Users.FirstAsync(u => u.Id == userId);
        return user.Email!;
    }

    private sealed record ClinicIdResponse(string ClinicId);
}

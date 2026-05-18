using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Halen.IntegrationTests.MedicalRecords;

[TestClass]
public class RecordAccessControllerTests : IntegrationTestBase
{
    private static async Task<Guid> GetPatientProfileIdAsync(HttpClient patientClient, Guid doctorProfileId)
    {
        var bookResponse = await patientClient.PostAsJsonAsync("/api/v1/appointments", new
        {
            DoctorId = doctorProfileId,
            ScheduledAt = DateTime.UtcNow.Date.AddDays(30).AddHours(10),
            Reason = "Profile lookup",
        });
        bookResponse.EnsureSuccessStatusCode();

        var response = await patientClient.GetAsync("/api/v1/appointments");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<AppointmentsResponse>();
        return body!.Appointments[0].PatientId;
    }

    private static async Task<Guid> GetDoctorUserIdAsync(Guid doctorProfileId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Halen.Infrastructure.Persistence.HalenDbContext>();
        var doc = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .FirstAsync(db.DoctorProfiles, d => d.Id == doctorProfileId);
        return doc.UserId;
    }

    /// <summary>
    /// Grants access as admin and returns the created access ID.
    /// </summary>
    private static async Task<Guid> GrantAccessAsync(
        HttpClient admin, Guid patientProfileId, Guid doctorUserId)
    {
        var response = await admin.PostAsJsonAsync(
            $"/api/v1/record-access/{patientProfileId}/grant",
            new
            {
                GrantToUserId = doctorUserId,
                AccessLevel = "Full",
                Reason = "Treatment",
            });

        var responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, responseBody);

        var result = await response.Content.ReadFromJsonAsync<AccessIdResponse>();
        return result!.AccessId;
    }

    // ── Grant Access Tests ──────────────────────────────────────────────────

    [TestMethod]
    public async Task GrantAccess_AsAdmin_Returns201()
    {
        var admin = await AdminClientAsync();
        var (doctorProfileId, _) = await CreateDoctorWithClientAsync();
        var patient = await PatientClientAsync();
        var patientProfileId = await GetPatientProfileIdAsync(patient, doctorProfileId);
        var doctorUserId = await GetDoctorUserIdAsync(doctorProfileId);

        var response = await admin.PostAsJsonAsync(
            $"/api/v1/record-access/{patientProfileId}/grant",
            new
            {
                GrantToUserId = doctorUserId,
                AccessLevel = "Full",
                Reason = "Treatment",
            });

        var responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, responseBody);
    }

    [TestMethod]
    public async Task GrantAccess_AsDoctor_Returns403()
    {
        var (_, doctor) = await CreateDoctorWithClientAsync();

        var response = await doctor.PostAsJsonAsync(
            $"/api/v1/record-access/{Guid.NewGuid()}/grant",
            new
            {
                GrantToUserId = Guid.NewGuid(),
                AccessLevel = "Full",
                Reason = "Treatment",
            });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [TestMethod]
    public async Task GrantAccess_AsPatient_Returns403()
    {
        var patient = await PatientClientAsync();

        var response = await patient.PostAsJsonAsync(
            $"/api/v1/record-access/{Guid.NewGuid()}/grant",
            new
            {
                GrantToUserId = Guid.NewGuid(),
                AccessLevel = "Full",
                Reason = "Treatment",
            });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [TestMethod]
    public async Task GrantAccess_WithoutAuth_Returns401()
    {
        var anon = Factory.CreateClient();

        var response = await anon.PostAsJsonAsync(
            $"/api/v1/record-access/{Guid.NewGuid()}/grant",
            new
            {
                GrantToUserId = Guid.NewGuid(),
                AccessLevel = "Full",
                Reason = "Treatment",
            });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Revoke Access Tests ─────────────────────────────────────────────────

    [TestMethod]
    public async Task RevokeAccess_AsAdmin_Returns200()
    {
        var admin = await AdminClientAsync();
        var (doctorProfileId, _) = await CreateDoctorWithClientAsync();
        var patient = await PatientClientAsync();
        var patientProfileId = await GetPatientProfileIdAsync(patient, doctorProfileId);
        var doctorUserId = await GetDoctorUserIdAsync(doctorProfileId);

        var accessId = await GrantAccessAsync(admin, patientProfileId, doctorUserId);

        var response = await admin.PostAsJsonAsync(
            $"/api/v1/record-access/{accessId}/revoke",
            new { Reason = "No longer needed" });

        var responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, responseBody);
    }

    [TestMethod]
    public async Task RevokeAccess_NonExistentAccess_Returns404()
    {
        var admin = await AdminClientAsync();
        var fakeAccessId = Guid.NewGuid();

        var response = await admin.PostAsJsonAsync(
            $"/api/v1/record-access/{fakeAccessId}/revoke",
            new { Reason = "Cleanup" });

        var responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.NotFound, responseBody);
    }

    // ── Access Matrix Tests ─────────────────────────────────────────────────

    [TestMethod]
    public async Task GetAccessMatrix_AsAdmin_ReturnsEntries()
    {
        var admin = await AdminClientAsync();
        var (doctorProfileId, _) = await CreateDoctorWithClientAsync();
        var patient = await PatientClientAsync();
        var patientProfileId = await GetPatientProfileIdAsync(patient, doctorProfileId);
        var doctorUserId = await GetDoctorUserIdAsync(doctorProfileId);

        await GrantAccessAsync(admin, patientProfileId, doctorUserId);

        var response = await admin.GetAsync(
            $"/api/v1/record-access/{patientProfileId}/matrix");

        var responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, responseBody);

        var result = await response.Content.ReadFromJsonAsync<AccessMatrixResponse>();
        result!.Entries.Should().NotBeEmpty();
        result.TotalCount.Should().BeGreaterThanOrEqualTo(1);
    }

    [TestMethod]
    public async Task GetAccessMatrix_AsDoctor_Returns403()
    {
        var (_, doctor) = await CreateDoctorWithClientAsync();

        var response = await doctor.GetAsync(
            $"/api/v1/record-access/{Guid.NewGuid()}/matrix");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Access Logs Tests ───────────────────────────────────────────────────

    [TestMethod]
    public async Task GetAccessLogs_AsAdmin_ReturnsLogs()
    {
        var admin = await AdminClientAsync();
        var (doctorProfileId, _) = await CreateDoctorWithClientAsync();
        var patient = await PatientClientAsync();
        var patientProfileId = await GetPatientProfileIdAsync(patient, doctorProfileId);
        var doctorUserId = await GetDoctorUserIdAsync(doctorProfileId);

        // Grant access to generate at least one log entry
        await GrantAccessAsync(admin, patientProfileId, doctorUserId);

        var response = await admin.GetAsync(
            $"/api/v1/record-access/{patientProfileId}/logs");

        var responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, responseBody);

        var result = await response.Content.ReadFromJsonAsync<AccessLogsResponse>();
        result.Should().NotBeNull();
        result!.TotalCount.Should().BeGreaterThanOrEqualTo(0);
    }

    [TestMethod]
    public async Task GetAccessLogs_AsDoctor_Returns403()
    {
        var (_, doctor) = await CreateDoctorWithClientAsync();

        var response = await doctor.GetAsync(
            $"/api/v1/record-access/{Guid.NewGuid()}/logs");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Response DTOs ───────────────────────────────────────────────────────

    private sealed record AccessIdResponse(Guid AccessId);

    private sealed record AppointmentDto(
        Guid Id, DateTime ScheduledAt, int DurationMinutes, string Reason,
        string Status, string? Notes, string DoctorName, string Specialty,
        decimal ConsultationFee, string PatientName, Guid PatientId);

    private sealed record AppointmentsResponse(AppointmentDto[] Appointments, int TotalCount);

    private sealed record AccessMatrixEntryDto(
        Guid Id, string UserName, string UserRole, string AccessLevel,
        DateTime GrantedAt, string GrantedBy, DateTime? RevokedAt, DateTime? LastViewed);

    private sealed record AccessMatrixResponse(AccessMatrixEntryDto[] Entries, int TotalCount);

    private sealed record AccessLogDto(
        Guid Id, string AccessedBy, string Action, string ResourceType, DateTime AccessedAt);

    private sealed record AccessLogsResponse(AccessLogDto[] Logs, int TotalCount);
}

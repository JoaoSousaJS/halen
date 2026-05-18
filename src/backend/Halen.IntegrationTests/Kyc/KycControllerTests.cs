using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Halen.Domain.Enums;
using Halen.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Halen.IntegrationTests.Kyc;

[TestClass]
public class KycControllerTests : IntegrationTestBase
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static MultipartFormDataContent CreateKycFormData()
    {
        var form = new MultipartFormDataContent();

        var license = new ByteArrayContent(new byte[] { 0xFF, 0xD8, 0xFF });
        license.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        form.Add(license, "licensePhoto", "license.jpg");

        var cert = new ByteArrayContent(new byte[] { 0x25, 0x50, 0x44, 0x46 });
        cert.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        form.Add(cert, "medicalCertificate", "cert.pdf");

        var id = new ByteArrayContent(new byte[] { 0x89, 0x50, 0x4E, 0x47 });
        id.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        form.Add(id, "identityProof", "id.png");

        return form;
    }

    private static async Task SubmitKycAsync(HttpClient doctor)
    {
        using var form = CreateKycFormData();
        var response = await doctor.PostAsync("/api/v1/doctor/kyc/documents", form);
        response.EnsureSuccessStatusCode();
    }

    // ── KYC Status ───────────────────────────────────────────────────────────

    [TestMethod]
    public async Task GetKycStatus_NewDoctor_ReturnsNotSubmitted()
    {
        var (_, doctor, _) = await CreateDoctorWithClientAndEmailAsync("Kyc", approveKyc: false);

        var response = await doctor.GetAsync("/api/v1/doctor/kyc/status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<KycStatusResponse>();
        body!.Status.Should().Be("NotSubmitted");
        body.Documents.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetKycStatus_WithoutAuth_Returns401()
    {
        var anon = Factory.CreateClient();

        var response = await anon.GetAsync("/api/v1/doctor/kyc/status");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task GetKycStatus_AsPatient_Returns403()
    {
        var patient = await PatientClientAsync();

        var response = await patient.GetAsync("/api/v1/doctor/kyc/status");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── KYC Submission ───────────────────────────────────────────────────────

    [TestMethod]
    public async Task SubmitKyc_AsDoctor_Returns200AndSetsSubmitted()
    {
        var (_, doctor, _) = await CreateDoctorWithClientAndEmailAsync("Kyc", approveKyc: false);

        using var form = CreateKycFormData();
        var response = await doctor.PostAsync("/api/v1/doctor/kyc/documents", form);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var status = await doctor.GetFromJsonAsync<KycStatusResponse>("/api/v1/doctor/kyc/status");
        status!.Status.Should().Be("Submitted");
        status.Documents.Should().HaveCount(3);
    }

    [TestMethod]
    public async Task SubmitKyc_Twice_Returns400()
    {
        var (_, doctor, _) = await CreateDoctorWithClientAndEmailAsync("Kyc", approveKyc: false);
        await SubmitKycAsync(doctor);

        using var form = CreateKycFormData();
        var response = await doctor.PostAsync("/api/v1/doctor/kyc/documents", form);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task SubmitKyc_WithoutAuth_Returns401()
    {
        var anon = Factory.CreateClient();

        using var form = CreateKycFormData();
        var response = await anon.PostAsync("/api/v1/doctor/kyc/documents", form);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Admin KYC Details ────────────────────────────────────────────────────

    [TestMethod]
    public async Task GetKycDetails_AsAdmin_ReturnsDetails()
    {
        var (doctorId, doctor, _) = await CreateDoctorWithClientAndEmailAsync("Kyc", approveKyc: false);
        await SubmitKycAsync(doctor);
        var admin = await AdminClientAsync();

        var response = await admin.GetAsync($"/api/v1/admin/doctors/{doctorId}/kyc");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<KycDetailsResponse>();
        body!.DoctorProfileId.Should().Be(doctorId);
        body.Status.Should().Be("Submitted");
        body.Documents.Should().HaveCount(3);
        body.DoctorName.Should().Contain("Kyc");
    }

    [TestMethod]
    public async Task GetKycDetails_NonExistentDoctor_Returns404()
    {
        var admin = await AdminClientAsync();

        var response = await admin.GetAsync($"/api/v1/admin/doctors/{Guid.NewGuid()}/kyc");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task GetKycDetails_AsDoctor_Returns403()
    {
        var (doctorId, doctor, _) = await CreateDoctorWithClientAndEmailAsync("Kyc", approveKyc: false);

        var response = await doctor.GetAsync($"/api/v1/admin/doctors/{doctorId}/kyc");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Admin KYC Review — Approve ───────────────────────────────────────────

    [TestMethod]
    public async Task ReviewKyc_Approve_Returns200AndActivatesDoctor()
    {
        var (doctorId, doctor, _) = await CreateDoctorWithClientAndEmailAsync("Kyc", approveKyc: false);
        await SubmitKycAsync(doctor);
        var admin = await AdminClientAsync();

        var response = await admin.PostAsJsonAsync(
            $"/api/v1/admin/doctors/{doctorId}/kyc/review",
            new { Decision = KycDecision.Approved, RejectionReason = (string?)null });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HalenDbContext>();
        var profile = await db.DoctorProfiles.Include(d => d.User).FirstAsync(d => d.Id == doctorId);
        profile.KycStatus.Should().Be(KycStatus.Approved);
        profile.User.Status.Should().Be(AccountStatus.Active);
    }

    [TestMethod]
    public async Task ReviewKyc_Approve_DoctorSeesApprovedStatus()
    {
        var (doctorId, doctor, _) = await CreateDoctorWithClientAndEmailAsync("Kyc", approveKyc: false);
        await SubmitKycAsync(doctor);
        var admin = await AdminClientAsync();

        await admin.PostAsJsonAsync(
            $"/api/v1/admin/doctors/{doctorId}/kyc/review",
            new { Decision = KycDecision.Approved, RejectionReason = (string?)null });

        var status = await doctor.GetFromJsonAsync<KycStatusResponse>("/api/v1/doctor/kyc/status");
        status!.Status.Should().Be("Approved");
    }

    // ── Admin KYC Review — Reject ────────────────────────────────────────────

    [TestMethod]
    public async Task ReviewKyc_Reject_Returns200AndSetsRejected()
    {
        var (doctorId, doctor, _) = await CreateDoctorWithClientAndEmailAsync("Kyc", approveKyc: false);
        await SubmitKycAsync(doctor);
        var admin = await AdminClientAsync();

        var response = await admin.PostAsJsonAsync(
            $"/api/v1/admin/doctors/{doctorId}/kyc/review",
            new { Decision = KycDecision.Rejected, RejectionReason = "Blurry photo" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var status = await doctor.GetFromJsonAsync<KycStatusResponse>("/api/v1/doctor/kyc/status");
        status!.Status.Should().Be("Rejected");
        status.LastRejectionReason.Should().Be("Blurry photo");
    }

    [TestMethod]
    public async Task ReviewKyc_RejectWithoutReason_Returns400()
    {
        var (doctorId, doctor, _) = await CreateDoctorWithClientAndEmailAsync("Kyc", approveKyc: false);
        await SubmitKycAsync(doctor);
        var admin = await AdminClientAsync();

        var response = await admin.PostAsJsonAsync(
            $"/api/v1/admin/doctors/{doctorId}/kyc/review",
            new { Decision = KycDecision.Rejected, RejectionReason = (string?)null });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task ReviewKyc_AsDoctor_Returns403()
    {
        var (doctorId, doctor, _) = await CreateDoctorWithClientAndEmailAsync("Kyc", approveKyc: false);

        var response = await doctor.PostAsJsonAsync(
            $"/api/v1/admin/doctors/{doctorId}/kyc/review",
            new { Decision = KycDecision.Approved, RejectionReason = (string?)null });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [TestMethod]
    public async Task ReviewKyc_DoctorNotSubmitted_Returns400()
    {
        var (doctorId, _, _) = await CreateDoctorWithClientAndEmailAsync("Kyc", approveKyc: false);
        var admin = await AdminClientAsync();

        var response = await admin.PostAsJsonAsync(
            $"/api/v1/admin/doctors/{doctorId}/kyc/review",
            new { Decision = KycDecision.Approved, RejectionReason = (string?)null });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Resubmission After Rejection ─────────────────────────────────────────

    [TestMethod]
    public async Task Resubmit_AfterRejection_ClearsOldDocsAndSetsSubmitted()
    {
        var (doctorId, doctor, _) = await CreateDoctorWithClientAndEmailAsync("Kyc", approveKyc: false);
        await SubmitKycAsync(doctor);

        var admin = await AdminClientAsync();
        await admin.PostAsJsonAsync(
            $"/api/v1/admin/doctors/{doctorId}/kyc/review",
            new { Decision = KycDecision.Rejected, RejectionReason = "Try again" });

        await SubmitKycAsync(doctor);

        var status = await doctor.GetFromJsonAsync<KycStatusResponse>("/api/v1/doctor/kyc/status");
        status!.Status.Should().Be("Submitted");
        status.Documents.Should().HaveCount(3);
    }

    // ── Document Download ────────────────────────────────────────────────────

    [TestMethod]
    public async Task DownloadDocument_AsAdmin_ReturnsFile()
    {
        var (doctorId, doctor, _) = await CreateDoctorWithClientAndEmailAsync("Kyc", approveKyc: false);
        await SubmitKycAsync(doctor);

        var admin = await AdminClientAsync();
        var details = await admin.GetFromJsonAsync<KycDetailsResponse>(
            $"/api/v1/admin/doctors/{doctorId}/kyc");
        var docId = details!.Documents[0].Id;

        var response = await admin.GetAsync($"/api/v1/admin/kyc/documents/{docId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType.Should().NotBeNull();
    }

    [TestMethod]
    public async Task DownloadDocument_NonExistent_Returns404()
    {
        var admin = await AdminClientAsync();

        var response = await admin.GetAsync($"/api/v1/admin/kyc/documents/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Full E2E Flow ────────────────────────────────────────────────────────

    [TestMethod]
    public async Task FullFlow_CreateDoctor_SubmitKyc_Approve_CanBookAppointment()
    {
        var (doctorId, doctor, _) = await CreateDoctorWithClientAndEmailAsync("Kyc", approveKyc: false);

        var statusBefore = await doctor.GetFromJsonAsync<KycStatusResponse>("/api/v1/doctor/kyc/status");
        statusBefore!.Status.Should().Be("NotSubmitted");

        await SubmitKycAsync(doctor);

        var statusAfter = await doctor.GetFromJsonAsync<KycStatusResponse>("/api/v1/doctor/kyc/status");
        statusAfter!.Status.Should().Be("Submitted");

        var admin = await AdminClientAsync();
        await admin.PostAsJsonAsync(
            $"/api/v1/admin/doctors/{doctorId}/kyc/review",
            new { Decision = KycDecision.Approved, RejectionReason = (string?)null });

        var statusApproved = await doctor.GetFromJsonAsync<KycStatusResponse>("/api/v1/doctor/kyc/status");
        statusApproved!.Status.Should().Be("Approved");

        var patient = await PatientClientAsync();

        var bookResponse = await patient.PostAsJsonAsync("/api/v1/appointments", new
        {
            DoctorId = doctorId,
            ScheduledAt = DateTime.UtcNow.Date.AddDays(5).AddHours(10),
            Reason = "Post-KYC booking",
        });
        bookResponse.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // ── Response DTOs ────────────────────────────────────────────────────────

    private sealed record KycStatusResponse(
        string Status,
        string? SubmittedAt,
        string? LastRejectionReason,
        KycDocumentDto[] Documents);

    private sealed record KycDocumentDto(
        Guid Id,
        string DocumentType,
        string FileName,
        string UploadedAt);

    private sealed record KycDetailsResponse(
        Guid DoctorProfileId,
        string DoctorName,
        string Specialty,
        string LicenseNumber,
        string Status,
        string? SubmittedAt,
        KycDocumentDto[] Documents,
        KycReviewDto[] ReviewHistory);

    private sealed record KycReviewDto(
        Guid Id,
        string Decision,
        string? RejectionReason,
        string ReviewerName,
        string ReviewedAt);
}

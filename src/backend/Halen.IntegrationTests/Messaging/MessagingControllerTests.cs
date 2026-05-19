using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Halen.IntegrationTests.Messaging;

[TestClass]
public class MessagingControllerTests : IntegrationTestBase
{
    private static async Task<(HttpClient PatientClient, HttpClient DoctorClient, Guid ThreadId)> SetupThreadAsync()
    {
        var (patientClient, _) = await PatientClientWithEmailAsync();
        var (doctorProfileId, doctorClient) = await CreateDoctorWithClientAsync();

        var bookResp = await patientClient.PostAsJsonAsync("/api/v1/appointments", new
        {
            DoctorId = doctorProfileId,
            ScheduledAt = DateTime.UtcNow.Date.AddDays(1).AddHours(10),
            Reason = "Messaging test",
        });
        bookResp.EnsureSuccessStatusCode();
        var booking = await bookResp.Content.ReadFromJsonAsync<AppointmentIdResponse>();

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HalenDbContext>();
        var thread = await db.ConversationThreads
            .FirstAsync(t => t.AppointmentId == booking!.AppointmentId);

        return (patientClient, doctorClient, thread.Id);
    }

    // ── GET /api/v1/messaging/threads ──────────────────────────────────────

    [TestMethod]
    public async Task GetMyThreads_ReturnsThreadsForParticipant()
    {
        var (patientClient, _, threadId) = await SetupThreadAsync();

        var response = await patientClient.GetAsync("/api/v1/messaging/threads");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ThreadListResponse>();
        body!.Threads.Should().Contain(t => t.ThreadId == threadId);
    }

    [TestMethod]
    public async Task GetMyThreads_DoctorSeesOwnThreads()
    {
        var (_, doctorClient, threadId) = await SetupThreadAsync();

        var response = await doctorClient.GetAsync("/api/v1/messaging/threads");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ThreadListResponse>();
        body!.Threads.Should().Contain(t => t.ThreadId == threadId);
    }

    [TestMethod]
    public async Task GetMyThreads_UnrelatedPatientDoesNotSeeThread()
    {
        var (_, _, threadId) = await SetupThreadAsync();
        var unrelatedPatient = await PatientClientAsync();

        var response = await unrelatedPatient.GetAsync("/api/v1/messaging/threads");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ThreadListResponse>();
        body!.Threads.Should().NotContain(t => t.ThreadId == threadId);
    }

    // ── POST /api/v1/messaging/threads/{id}/messages ───────────────────────

    [TestMethod]
    public async Task SendMessage_PatientSendsMessage_Returns201()
    {
        var (patientClient, _, threadId) = await SetupThreadAsync();

        var response = await patientClient.PostAsJsonAsync(
            $"/api/v1/messaging/threads/{threadId}/messages",
            new { Content = "Hello doctor!" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<MessageIdResponse>();
        body!.MessageId.Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task SendMessage_DoctorSendsMessage_Returns201()
    {
        var (_, doctorClient, threadId) = await SetupThreadAsync();

        var response = await doctorClient.PostAsJsonAsync(
            $"/api/v1/messaging/threads/{threadId}/messages",
            new { Content = "Hello patient!" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [TestMethod]
    public async Task SendMessage_EmptyContent_Returns400()
    {
        var (patientClient, _, threadId) = await SetupThreadAsync();

        var response = await patientClient.PostAsJsonAsync(
            $"/api/v1/messaging/threads/{threadId}/messages",
            new { Content = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task SendMessage_NonParticipant_ReturnsForbidden()
    {
        var (_, _, threadId) = await SetupThreadAsync();
        var unrelatedPatient = await PatientClientAsync();

        var response = await unrelatedPatient.PostAsJsonAsync(
            $"/api/v1/messaging/threads/{threadId}/messages",
            new { Content = "Sneaky message" });

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
    }

    // ── GET /api/v1/messaging/threads/{id}/messages ────────────────────────

    [TestMethod]
    public async Task GetThreadMessages_ReturnsMessages()
    {
        var (patientClient, _, threadId) = await SetupThreadAsync();

        await patientClient.PostAsJsonAsync(
            $"/api/v1/messaging/threads/{threadId}/messages",
            new { Content = "First message" });
        await patientClient.PostAsJsonAsync(
            $"/api/v1/messaging/threads/{threadId}/messages",
            new { Content = "Second message" });

        var response = await patientClient.GetAsync(
            $"/api/v1/messaging/threads/{threadId}/messages");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<MessageListResponse>();
        body!.Messages.Should().HaveCountGreaterOrEqualTo(2);
    }

    [TestMethod]
    public async Task GetThreadMessages_NonParticipant_ReturnsForbidden()
    {
        var (_, _, threadId) = await SetupThreadAsync();
        var unrelatedPatient = await PatientClientAsync();

        var response = await unrelatedPatient.GetAsync(
            $"/api/v1/messaging/threads/{threadId}/messages");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
    }

    // ── POST /api/v1/messaging/threads/{id}/read ───────────────────────────

    [TestMethod]
    public async Task MarkAsRead_ResetsUnreadCount()
    {
        var (patientClient, doctorClient, threadId) = await SetupThreadAsync();

        await doctorClient.PostAsJsonAsync(
            $"/api/v1/messaging/threads/{threadId}/messages",
            new { Content = "Please check this" });

        var readResponse = await patientClient.PostAsync(
            $"/api/v1/messaging/threads/{threadId}/read", null);

        readResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HalenDbContext>();
        var thread = await db.ConversationThreads.FirstAsync(t => t.Id == threadId);
        thread.PatientUnreadCount.Should().Be(0);
    }

    // ── POST /api/v1/messaging/threads/{id}/close ──────────────────────────

    [TestMethod]
    public async Task CloseThread_DoctorCloses_ReturnsOk()
    {
        var (_, doctorClient, threadId) = await SetupThreadAsync();

        var response = await doctorClient.PostAsJsonAsync(
            $"/api/v1/messaging/threads/{threadId}/close",
            new { Reason = "Resolved" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HalenDbContext>();
        var thread = await db.ConversationThreads.FirstAsync(t => t.Id == threadId);
        thread.Status.Should().Be(ThreadStatus.Closed);
    }

    [TestMethod]
    public async Task CloseThread_PatientAttempts_ReturnsForbidden()
    {
        var (patientClient, _, threadId) = await SetupThreadAsync();

        var response = await patientClient.PostAsJsonAsync(
            $"/api/v1/messaging/threads/{threadId}/close",
            new { Reason = "I want to close" });

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task SendMessage_ToClosedThread_ReturnsError()
    {
        var (patientClient, doctorClient, threadId) = await SetupThreadAsync();

        await doctorClient.PostAsJsonAsync(
            $"/api/v1/messaging/threads/{threadId}/close",
            new { Reason = "Done" });

        var response = await patientClient.PostAsJsonAsync(
            $"/api/v1/messaging/threads/{threadId}/messages",
            new { Content = "Trying to send" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── GET /api/v1/messaging/search ───────────────────────────────────────

    [TestMethod]
    public async Task SearchMessages_FindsMatchingContent()
    {
        var (patientClient, _, threadId) = await SetupThreadAsync();

        await patientClient.PostAsJsonAsync(
            $"/api/v1/messaging/threads/{threadId}/messages",
            new { Content = "My unique symptom xylophone123" });

        var response = await patientClient.GetAsync(
            "/api/v1/messaging/search?q=xylophone123");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SearchResponse>();
        body!.Hits.Should().Contain(h => h.Content.Contains("xylophone123"));
    }

    [TestMethod]
    public async Task SearchMessages_ShortQuery_Returns400()
    {
        var patientClient = await PatientClientAsync();

        var response = await patientClient.GetAsync("/api/v1/messaging/search?q=x");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── POST /api/v1/messaging/threads/{id}/attachments ────────────────────

    [TestMethod]
    public async Task SendAttachment_ValidImage_Returns201()
    {
        var (patientClient, _, threadId) = await SetupThreadAsync();

        using var content = new MultipartFormDataContent();
        var fileBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG header
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "file", "test-image.png");

        var response = await patientClient.PostAsync(
            $"/api/v1/messaging/threads/{threadId}/attachments", content);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<MessageIdResponse>();
        body!.MessageId.Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task DownloadAttachment_ReturnsFileWithNosniff()
    {
        var (patientClient, _, threadId) = await SetupThreadAsync();

        using var uploadContent = new MultipartFormDataContent();
        var fileBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        uploadContent.Add(fileContent, "file", "download-test.png");

        var uploadResp = await patientClient.PostAsync(
            $"/api/v1/messaging/threads/{threadId}/attachments", uploadContent);
        uploadResp.EnsureSuccessStatusCode();
        var uploaded = await uploadResp.Content.ReadFromJsonAsync<MessageIdResponse>();

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HalenDbContext>();
        var attachment = await db.MessageAttachments
            .FirstAsync(a => a.Message!.ThreadId == threadId);

        var downloadResp = await patientClient.GetAsync(
            $"/api/v1/messaging/threads/{threadId}/attachments/{attachment.Id}");

        downloadResp.StatusCode.Should().Be(HttpStatusCode.OK);
        downloadResp.Headers.Should().ContainKey("X-Content-Type-Options");
        downloadResp.Headers.GetValues("X-Content-Type-Options").Should().Contain("nosniff");
    }

    [TestMethod]
    public async Task DownloadAttachment_NonParticipant_ReturnsForbidden()
    {
        var (patientClient, _, threadId) = await SetupThreadAsync();

        using var uploadContent = new MultipartFormDataContent();
        var fileBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        uploadContent.Add(fileContent, "file", "private-file.png");

        var uploadResp = await patientClient.PostAsync(
            $"/api/v1/messaging/threads/{threadId}/attachments", uploadContent);
        uploadResp.EnsureSuccessStatusCode();

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HalenDbContext>();
        var attachment = await db.MessageAttachments
            .FirstAsync(a => a.Message!.ThreadId == threadId);

        var (otherPatient, _) = await PatientClientWithEmailAsync();

        var downloadResp = await otherPatient.GetAsync(
            $"/api/v1/messaging/threads/{threadId}/attachments/{attachment.Id}");

        downloadResp.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
    }

    // ── Auth enforcement ───────────────────────────────────────────────────

    [TestMethod]
    public async Task AllEndpoints_Unauthenticated_Return401()
    {
        var anon = Factory.CreateClient();
        var fakeThreadId = Guid.NewGuid();

        var threads = await anon.GetAsync("/api/v1/messaging/threads");
        var messages = await anon.GetAsync($"/api/v1/messaging/threads/{fakeThreadId}/messages");
        var send = await anon.PostAsJsonAsync($"/api/v1/messaging/threads/{fakeThreadId}/messages",
            new { Content = "hi" });
        var read = await anon.PostAsync($"/api/v1/messaging/threads/{fakeThreadId}/read", null);
        var close = await anon.PostAsJsonAsync($"/api/v1/messaging/threads/{fakeThreadId}/close",
            new { Reason = "x" });
        var search = await anon.GetAsync("/api/v1/messaging/search?q=test");

        threads.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        messages.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        send.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        read.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        close.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        search.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Feature flag gating ────────────────────────────────────────────────

    [TestMethod]
    public async Task Endpoints_MessagingDisabled_Return403()
    {
        var admin = await AdminClientAsync();
        var slug = $"nomsg-{Guid.NewGuid():N}"[..20];

        var createResp = await admin.PostAsJsonAsync("/api/v1/clinics",
            new { Name = "No Messaging", Slug = slug });
        createResp.EnsureSuccessStatusCode();
        var created = await createResp.Content.ReadFromJsonAsync<ClinicIdResponse>();
        var clinicId = Guid.Parse(created!.ClinicId);

        var (patient, _) = await CreateClinicPatientAsync(clinicId);

        var response = await patient.GetAsync("/api/v1/messaging/threads");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [TestMethod]
    public async Task Endpoints_MessagingEnabled_AllowsAccess()
    {
        var admin = await AdminClientAsync();
        var slug = $"wmsge-{Guid.NewGuid():N}"[..20];

        var createResp = await admin.PostAsJsonAsync("/api/v1/clinics",
            new { Name = "With Messaging", Slug = slug });
        createResp.EnsureSuccessStatusCode();
        var created = await createResp.Content.ReadFromJsonAsync<ClinicIdResponse>();
        var clinicId = Guid.Parse(created!.ClinicId);

        await admin.PutAsJsonAsync(
            $"/api/v1/clinics/{clinicId}/features/messaging",
            new { IsEnabled = true });

        var (patient, _) = await CreateClinicPatientAsync(clinicId);

        var response = await patient.GetAsync("/api/v1/messaging/threads");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Thread creation via booking ────────────────────────────────────────

    [TestMethod]
    public async Task BookAppointment_CreatesConversationThread()
    {
        var (patientClient, _) = await PatientClientWithEmailAsync();
        var (doctorProfileId, _) = await CreateDoctorWithClientAsync();

        var bookResp = await patientClient.PostAsJsonAsync("/api/v1/appointments", new
        {
            DoctorId = doctorProfileId,
            ScheduledAt = DateTime.UtcNow.Date.AddDays(2).AddHours(14),
            Reason = "Thread creation test",
        });
        bookResp.EnsureSuccessStatusCode();
        var booking = await bookResp.Content.ReadFromJsonAsync<AppointmentIdResponse>();

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HalenDbContext>();
        var thread = await db.ConversationThreads
            .FirstOrDefaultAsync(t => t.AppointmentId == booking!.AppointmentId);

        thread.Should().NotBeNull();
        thread!.Status.Should().Be(ThreadStatus.Active);
        thread.Subject.Should().NotBeNullOrEmpty();
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static async Task<(HttpClient Client, string Email)> CreateClinicPatientAsync(Guid clinicId)
    {
        var email = $"patient+{Guid.NewGuid():N}@test.com";

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HalenDbContext>();
        var userManager = scope.ServiceProvider
            .GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<User>>();

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

    // ── DTOs ────────────────────────────────────────────────────────────────

    private sealed record AppointmentIdResponse(Guid AppointmentId);
    private sealed record ClinicIdResponse(string ClinicId);
    private sealed record MessageIdResponse(Guid MessageId);

    private sealed record ThreadListResponse(ThreadSummary[] Threads, int TotalCount);
    private sealed record ThreadSummary(
        Guid ThreadId,
        string OtherParticipantName,
        string Subject,
        string? LastMessagePreview,
        int UnreadCount,
        string Status);

    private sealed record MessageListResponse(MessageItem[] Messages, int TotalCount);
    private sealed record MessageItem(
        Guid Id,
        string SenderName,
        string Content,
        string MessageType,
        bool IsRead,
        DateTime CreatedAt);

    private sealed record SearchResponse(SearchHit[] Hits, int TotalCount);
    private sealed record SearchHit(
        Guid ThreadId,
        Guid MessageId,
        string Content,
        string SenderName);
}

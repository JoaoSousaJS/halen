using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;

namespace Halen.IntegrationTests.Consultations;

[TestClass]
public class ConsultationsControllerTests : IntegrationTestBase
{
    private static async Task<(HttpClient PatientClient, HttpClient DoctorClient, Guid AppointmentId)> SetupAppointmentAsync()
    {
        var (patientClient, _) = await PatientClientWithEmailAsync();
        var (doctorProfileId, doctorClient) = await CreateDoctorWithClientAsync();
        var appointmentId = await BookAppointmentAsync(patientClient, doctorProfileId);
        return (patientClient, doctorClient, appointmentId);
    }

    private static async Task<Guid> BookAppointmentAsync(HttpClient patient, Guid doctorId)
    {
        var response = await patient.PostAsJsonAsync("/api/v1/appointments", new
        {
            DoctorId = doctorId,
            ScheduledAt = DateTime.UtcNow.AddDays(1),
            Reason = "Consultation test",
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<AppointmentIdResponse>();
        return body!.AppointmentId;
    }

    private static async Task CreateRoomViaStartAsync(HttpClient client, Guid appointmentId)
    {
        var hub = await HubHelpers.ConnectToConsultationHubAsync(Factory, client);
        await hub.InvokeAsync("JoinRoom", appointmentId);
        await hub.StopAsync();
    }

    // ── GET /api/v1/consultations/{appointmentId} ───────────────────────────

    [TestMethod]
    public async Task Get_ReturnsRoom_AfterRoomCreated()
    {
        var (patientClient, doctorClient, appointmentId) = await SetupAppointmentAsync();
        await CreateRoomViaStartAsync(doctorClient, appointmentId);

        var response = await doctorClient.GetAsync($"/api/v1/consultations/{appointmentId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var room = await response.Content.ReadFromJsonAsync<ConsultationRoomResponse>();
        room.Should().NotBeNull();
        room!.AppointmentId.Should().Be(appointmentId);
        room.RoomCode.Should().StartWith("VC-");
        room.Status.Should().Be("Waiting");
        room.DoctorName.Should().NotBeNullOrEmpty();
        room.PatientName.Should().NotBeNullOrEmpty();
    }

    [TestMethod]
    public async Task Get_Returns404_WhenNoRoomExists()
    {
        var (patientClient, _, appointmentId) = await SetupAppointmentAsync();

        var response = await patientClient.GetAsync($"/api/v1/consultations/{appointmentId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task Get_Returns403_WhenCallerIsNotParticipant()
    {
        var (_, doctorClient, appointmentId) = await SetupAppointmentAsync();
        await CreateRoomViaStartAsync(doctorClient, appointmentId);

        var (unrelatedPatient, _) = await PatientClientWithEmailAsync();

        var response = await unrelatedPatient.GetAsync($"/api/v1/consultations/{appointmentId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [TestMethod]
    public async Task Get_Returns401_WhenUnauthenticated()
    {
        var anon = Factory.CreateClient();

        var response = await anon.GetAsync($"/api/v1/consultations/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── DTOs ─────────────────────────────────────────────────────────────────

    private sealed record AppointmentIdResponse(Guid AppointmentId);

    private sealed record ConsultationRoomResponse(
        Guid Id,
        Guid AppointmentId,
        string RoomCode,
        string Status,
        string? DoctorName,
        string? PatientName);
}

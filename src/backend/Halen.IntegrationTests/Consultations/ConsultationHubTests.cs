using System.Net.Http.Json;
using FluentAssertions;
using Halen.Domain.Enums;
using Halen.Infrastructure.Persistence;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Halen.IntegrationTests.Consultations;

[TestClass]
public class ConsultationHubTests : IntegrationTestBase
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
            Reason = "Hub test",
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<AppointmentIdResponse>();
        return body!.AppointmentId;
    }

    // ── Full Flow ───────────────────────────────────────────────────────────

    [TestMethod]
    public async Task FullFlow_PatientJoins_DoctorJoins_Chat_DoctorEnds()
    {
        var (patientClient, doctorClient, appointmentId) = await SetupAppointmentAsync();

        await using var patientHub = await HubHelpers.ConnectToConsultationHubAsync(Factory, patientClient);
        await using var doctorHub = await HubHelpers.ConnectToConsultationHubAsync(Factory, doctorClient);

        var consultationStartedTcs = new TaskCompletionSource<bool>();
        var chatReceivedTcs = new TaskCompletionSource<string>();
        var consultationEndedTcs = new TaskCompletionSource<bool>();

        patientHub.On<object>("ConsultationStarted", _ => consultationStartedTcs.TrySetResult(true));
        patientHub.On<object>("ConsultationEnded", _ => consultationEndedTcs.TrySetResult(true));
        patientHub.On<object>("ReceiveChat", msg =>
        {
            var text = msg.GetType().GetProperty("text")?.GetValue(msg)?.ToString();
            chatReceivedTcs.TrySetResult(text ?? "");
        });

        // Patient joins first — room is created in Waiting status
        await patientHub.InvokeAsync("JoinRoom", appointmentId);

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HalenDbContext>();
            var room = await db.ConsultationRooms.FirstAsync(r => r.AppointmentId == appointmentId);
            room.Status.Should().Be(ConsultationRoomStatus.Waiting);
            room.PatientJoinedAt.Should().NotBeNull();
        }

        // Doctor joins — room transitions to Active, appointment to InProgress
        await doctorHub.InvokeAsync("JoinRoom", appointmentId);

        var started = await Task.WhenAny(consultationStartedTcs.Task, Task.Delay(5000));
        started.Should().Be(consultationStartedTcs.Task, "ConsultationStarted should fire when both join");

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HalenDbContext>();
            var room = await db.ConsultationRooms.FirstAsync(r => r.AppointmentId == appointmentId);
            room.Status.Should().Be(ConsultationRoomStatus.Active);
            room.DoctorJoinedAt.Should().NotBeNull();

            var appointment = await db.Appointments.FirstAsync(a => a.Id == appointmentId);
            appointment.Status.Should().Be(AppointmentStatus.InProgress);
        }

        // Doctor sends chat
        await doctorHub.InvokeAsync("SendChat", appointmentId, "How are you feeling?");

        var chatResult = await Task.WhenAny(chatReceivedTcs.Task, Task.Delay(5000));
        chatResult.Should().Be(chatReceivedTcs.Task, "Patient should receive chat message");

        // Doctor ends consultation
        await doctorHub.InvokeAsync("EndConsultation", appointmentId, "Patient is recovering well");

        var ended = await Task.WhenAny(consultationEndedTcs.Task, Task.Delay(5000));
        ended.Should().Be(consultationEndedTcs.Task, "ConsultationEnded should fire");

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HalenDbContext>();
            var room = await db.ConsultationRooms.FirstAsync(r => r.AppointmentId == appointmentId);
            room.Status.Should().Be(ConsultationRoomStatus.Ended);
            room.EndedAt.Should().NotBeNull();
            room.Notes.Should().Be("Patient is recovering well");

            var appointment = await db.Appointments.FirstAsync(a => a.Id == appointmentId);
            appointment.Status.Should().Be(AppointmentStatus.Completed);
        }

        await patientHub.StopAsync();
        await doctorHub.StopAsync();
    }

    // ── Authorization ───────────────────────────────────────────────────────

    [TestMethod]
    public async Task JoinRoom_NonParticipant_ThrowsHubException()
    {
        var (_, doctorClient, appointmentId) = await SetupAppointmentAsync();

        var (unrelatedPatient, _) = await PatientClientWithEmailAsync();
        await using var hub = await HubHelpers.ConnectToConsultationHubAsync(Factory, unrelatedPatient);

        var act = () => hub.InvokeAsync("JoinRoom", appointmentId);

        await act.Should().ThrowAsync<HubException>();
    }

    [TestMethod]
    public async Task EndConsultation_AsPatient_ThrowsHubException()
    {
        var (patientClient, doctorClient, appointmentId) = await SetupAppointmentAsync();

        await using var doctorHub = await HubHelpers.ConnectToConsultationHubAsync(Factory, doctorClient);
        await doctorHub.InvokeAsync("JoinRoom", appointmentId);

        await using var patientHub = await HubHelpers.ConnectToConsultationHubAsync(Factory, patientClient);
        await patientHub.InvokeAsync("JoinRoom", appointmentId);

        var act = () => patientHub.InvokeAsync("EndConsultation", appointmentId, (string?)null);

        await act.Should().ThrowAsync<HubException>();
    }

    [TestMethod]
    public async Task UpdateNotes_AsPatient_ThrowsHubException()
    {
        var (patientClient, doctorClient, appointmentId) = await SetupAppointmentAsync();

        await using var doctorHub = await HubHelpers.ConnectToConsultationHubAsync(Factory, doctorClient);
        await doctorHub.InvokeAsync("JoinRoom", appointmentId);

        await using var patientHub = await HubHelpers.ConnectToConsultationHubAsync(Factory, patientClient);
        await patientHub.InvokeAsync("JoinRoom", appointmentId);

        var act = () => patientHub.InvokeAsync("UpdateNotes", appointmentId, "Sneaky notes");

        await act.Should().ThrowAsync<HubException>();
    }

    // ── DTOs ─────────────────────────────────────────────────────────────────

    private sealed record AppointmentIdResponse(Guid AppointmentId);
}

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Halen.IntegrationTests.Appointments;

[TestClass]
public class AppointmentsControllerTests : IntegrationTestBase
{
    private static async Task<Guid> CreateDoctorAsync()
    {
        var (doctorProfileId, _) = await CreateDoctorWithClientAsync();
        return doctorProfileId;
    }

    private static async Task<Guid> BookAppointmentAsync(HttpClient patient, Guid doctorId, DateTime? scheduledAt = null)
    {
        var response = await patient.PostAsJsonAsync("/api/v1/appointments", new
        {
            DoctorId = doctorId,
            ScheduledAt = scheduledAt ?? DateTime.UtcNow.AddDays(1),
            Reason = "Test appointment",
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<AppointmentIdResponse>();
        return body!.AppointmentId;
    }

    // ── Booking Tests ────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Book_AsPatient_Returns201WithAppointmentId()
    {
        var (patient, _) = await PatientClientWithEmailAsync();
        var doctorId = await CreateDoctorAsync();

        var response = await patient.PostAsJsonAsync("/api/v1/appointments", new
        {
            DoctorId = doctorId,
            ScheduledAt = DateTime.UtcNow.AddDays(2),
            Reason = "Checkup",
        });

        var responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, responseBody);
        var body = await response.Content.ReadFromJsonAsync<AppointmentIdResponse>();
        body!.AppointmentId.Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task Book_WithoutAuth_Returns401()
    {
        var anon = Factory.CreateClient();

        var response = await anon.PostAsJsonAsync("/api/v1/appointments", new
        {
            DoctorId = Guid.NewGuid(),
            ScheduledAt = DateTime.UtcNow.AddDays(1),
            Reason = "Checkup",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task Book_ConflictingSlot_Returns400()
    {
        var (patient1, _) = await PatientClientWithEmailAsync();
        var (patient2, _) = await PatientClientWithEmailAsync();
        var doctorId = await CreateDoctorAsync();
        var time = DateTime.UtcNow.AddDays(3);

        await BookAppointmentAsync(patient1, doctorId, time);

        var response = await patient2.PostAsJsonAsync("/api/v1/appointments", new
        {
            DoctorId = doctorId,
            ScheduledAt = time.AddMinutes(10),
            Reason = "Overlapping",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── List Tests ───────────────────────────────────────────────────────────

    [TestMethod]
    public async Task GetMine_AsPatient_ReturnsBookedAppointments()
    {
        var (patient, _) = await PatientClientWithEmailAsync();
        var doctorId = await CreateDoctorAsync();

        await BookAppointmentAsync(patient, doctorId);

        var response = await patient.GetAsync("/api/v1/appointments");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<AppointmentsResponse>();
        body!.Appointments.Should().NotBeEmpty();
        body.Appointments[0].DoctorName.Should().NotBeNullOrEmpty();
    }

    // ── Cancel Tests ─────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Cancel_OwnAppointment_Returns200()
    {
        var (patient, _) = await PatientClientWithEmailAsync();
        var doctorId = await CreateDoctorAsync();
        var appointmentId = await BookAppointmentAsync(patient, doctorId);

        var response = await patient.PostAsync($"/api/v1/appointments/{appointmentId}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [TestMethod]
    public async Task Cancel_OtherPatientsAppointment_Returns403()
    {
        var (patient1, _) = await PatientClientWithEmailAsync();
        var (patient2, _) = await PatientClientWithEmailAsync();
        var doctorId = await CreateDoctorAsync();
        var appointmentId = await BookAppointmentAsync(patient1, doctorId);

        var response = await patient2.PostAsync($"/api/v1/appointments/{appointmentId}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [TestMethod]
    public async Task Cancel_NonExistentAppointment_Returns404()
    {
        var (patient, _) = await PatientClientWithEmailAsync();

        var response = await patient.PostAsync($"/api/v1/appointments/{Guid.NewGuid()}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Complete Tests ───────────────────────────────────────────────────────

    [TestMethod]
    public async Task Complete_AsDoctor_Returns200()
    {
        var (patient, _) = await PatientClientWithEmailAsync();
        var doctorId = await CreateDoctorAsync();
        var appointmentId = await BookAppointmentAsync(patient, doctorId);

        var doctorEmail = await GetDoctorEmailAsync(doctorId);
        var doctor = await TestHelpers.GetBearerClientAsync(Factory, doctorEmail, "Doctor1234!");

        var response = await doctor.PostAsJsonAsync(
            $"/api/v1/appointments/{appointmentId}/complete",
            new { Notes = "Patient is healthy" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [TestMethod]
    public async Task Complete_AsPatient_Returns403()
    {
        var (patient, _) = await PatientClientWithEmailAsync();
        var doctorId = await CreateDoctorAsync();
        var appointmentId = await BookAppointmentAsync(patient, doctorId);

        var response = await patient.PostAsJsonAsync(
            $"/api/v1/appointments/{appointmentId}/complete",
            new { Notes = "Self-complete" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Doctors List ─────────────────────────────────────────────────────────

    [TestMethod]
    public async Task ListDoctors_AsPatient_ReturnsNonEmptyList()
    {
        var (patient, _) = await PatientClientWithEmailAsync();
        await CreateDoctorAsync();

        var response = await patient.GetAsync("/api/v1/appointments/doctors");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<DoctorsResponse>();
        body!.Doctors.Should().NotBeEmpty();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static async Task<string> GetDoctorEmailAsync(Guid doctorProfileId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Halen.Infrastructure.Persistence.HalenDbContext>();
        var profile = await db.DoctorProfiles.Include(d => d.User).FirstAsync(d => d.Id == doctorProfileId);
        return profile.User.Email!;
    }

    // ── Response DTOs ────────────────────────────────────────────────────────

    private sealed record AppointmentIdResponse(Guid AppointmentId);
    private sealed record DoctorDto(Guid Id, string Name, string Specialty, decimal ConsultationFee, int YearsOfExperience);
    private sealed record AppointmentDto(
        Guid Id, DateTime ScheduledAt, int DurationMinutes, string Reason,
        string Status, string? Notes, string DoctorName, string Specialty,
        decimal ConsultationFee, string PatientName, Guid PatientId);
    private sealed record AppointmentsResponse(AppointmentDto[] Appointments, int TotalCount);
    private sealed record DoctorsResponse(DoctorDto[] Doctors, int TotalCount);
}

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Halen.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Halen.IntegrationTests.Appointments;

[TestClass]
public class AppointmentsControllerTests
{
    private static HalenWebApplicationFactory _factory = null!;

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext _)
    {
        _factory = new HalenWebApplicationFactory();
        await _factory.StartAsync();
    }

    [ClassCleanup]
    public static async Task ClassCleanup()
    {
        await _factory.StopAsync();
        await _factory.DisposeAsync();
    }

    private static async Task<HttpClient> AdminClientAsync() =>
        await TestHelpers.GetBearerClientAsync(_factory, "admin@test.com", "Admin1234!");

    private static async Task<(HttpClient client, string email)> PatientClientAsync()
    {
        var email = $"patient+{Guid.NewGuid():N}@test.com";
        var anon = _factory.CreateClient();

        await anon.PostAsJsonAsync("/api/v1/auth/register", new
        {
            FirstName = "Test",
            LastName = "Patient",
            Email = email,
            Password = "Patient1234!",
            Role = (int)UserRole.Patient,
        });

        var client = await TestHelpers.GetBearerClientAsync(_factory, email, "Patient1234!");
        return (client, email);
    }

    private static async Task<Guid> CreateDoctorAsync()
    {
        var admin = await AdminClientAsync();
        var response = await admin.PostAsJsonAsync("/api/v1/admin/doctors", new
        {
            FirstName = "Dr",
            LastName = "Test",
            Email = $"doctor+{Guid.NewGuid():N}@test.com",
            Password = "Doctor1234!",
            Specialty = "General",
            LicenseNumber = $"LIC-{Guid.NewGuid().ToString("N")[..8]}",
            ConsultationFee = 100.00m,
            YearsOfExperience = 5,
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<DoctorIdResponse>();
        return body!.DoctorId;
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
        var (patient, _) = await PatientClientAsync();
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
        var anon = _factory.CreateClient();

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
        var (patient1, _) = await PatientClientAsync();
        var (patient2, _) = await PatientClientAsync();
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
        var (patient, _) = await PatientClientAsync();
        var doctorId = await CreateDoctorAsync();

        await BookAppointmentAsync(patient, doctorId);

        var response = await patient.GetAsync("/api/v1/appointments");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var appointments = await response.Content.ReadFromJsonAsync<AppointmentDto[]>();
        appointments.Should().NotBeEmpty();
        appointments![0].DoctorName.Should().NotBeNullOrEmpty();
    }

    // ── Cancel Tests ─────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Cancel_OwnAppointment_Returns200()
    {
        var (patient, _) = await PatientClientAsync();
        var doctorId = await CreateDoctorAsync();
        var appointmentId = await BookAppointmentAsync(patient, doctorId);

        var response = await patient.PostAsync($"/api/v1/appointments/{appointmentId}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [TestMethod]
    public async Task Cancel_OtherPatientsAppointment_Returns403()
    {
        var (patient1, _) = await PatientClientAsync();
        var (patient2, _) = await PatientClientAsync();
        var doctorId = await CreateDoctorAsync();
        var appointmentId = await BookAppointmentAsync(patient1, doctorId);

        var response = await patient2.PostAsync($"/api/v1/appointments/{appointmentId}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [TestMethod]
    public async Task Cancel_NonExistentAppointment_Returns404()
    {
        var (patient, _) = await PatientClientAsync();

        var response = await patient.PostAsync($"/api/v1/appointments/{Guid.NewGuid()}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Complete Tests ───────────────────────────────────────────────────────

    [TestMethod]
    public async Task Complete_AsDoctor_Returns200()
    {
        var (patient, _) = await PatientClientAsync();
        var doctorId = await CreateDoctorAsync();
        var appointmentId = await BookAppointmentAsync(patient, doctorId);

        var doctorEmail = await GetDoctorEmailAsync(doctorId);
        var doctor = await TestHelpers.GetBearerClientAsync(_factory, doctorEmail, "Doctor1234!");

        var response = await doctor.PostAsJsonAsync(
            $"/api/v1/appointments/{appointmentId}/complete",
            new { Notes = "Patient is healthy" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [TestMethod]
    public async Task Complete_AsPatient_Returns403()
    {
        var (patient, _) = await PatientClientAsync();
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
        var (patient, _) = await PatientClientAsync();
        await CreateDoctorAsync();

        var response = await patient.GetAsync("/api/v1/appointments/doctors");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var doctors = await response.Content.ReadFromJsonAsync<DoctorDto[]>();
        doctors.Should().NotBeEmpty();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static async Task<string> GetDoctorEmailAsync(Guid doctorProfileId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Halen.Infrastructure.Persistence.HalenDbContext>();
        var profile = await db.DoctorProfiles.Include(d => d.User).FirstAsync(d => d.Id == doctorProfileId);
        return profile.User.Email!;
    }

    // ── Response DTOs ────────────────────────────────────────────────────────

    private sealed record DoctorIdResponse(Guid DoctorId);
    private sealed record AppointmentIdResponse(Guid AppointmentId);
    private sealed record DoctorDto(Guid Id, string Name, string Specialty, decimal ConsultationFee, int YearsOfExperience);
    private sealed record AppointmentDto(
        Guid Id, DateTime ScheduledAt, int DurationMinutes, string Reason,
        string Status, string? Notes, string DoctorName, string Specialty,
        decimal ConsultationFee, string PatientName);
}

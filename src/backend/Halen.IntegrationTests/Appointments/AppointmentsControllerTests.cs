using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Halen.Domain.Enums;
using Halen.Infrastructure.Persistence;
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
            ScheduledAt = scheduledAt ?? DateTime.UtcNow.Date.AddDays(1).AddHours(10),
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
            ScheduledAt = DateTime.UtcNow.Date.AddDays(2).AddHours(10),
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
            ScheduledAt = DateTime.UtcNow.Date.AddDays(1).AddHours(10),
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
        var time = DateTime.UtcNow.Date.AddDays(3).AddHours(10);

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

    // ── Payment Verification Tests ────────────────────────────────────────────

    [TestMethod]
    public async Task Book_CreatesPaymentWithAuthorizedStatus()
    {
        var (patient, _) = await PatientClientWithEmailAsync();
        var doctorId = await CreateDoctorAsync();
        var appointmentId = await BookAppointmentAsync(patient, doctorId);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HalenDbContext>();
        var payment = await db.Payments.FirstOrDefaultAsync(p => p.AppointmentId == appointmentId);

        payment.Should().NotBeNull();
        payment!.Status.Should().Be(PaymentStatus.Authorized);
        payment.Amount.Should().Be(100m);
        payment.PaymentIntentId.Should().NotBeNull();
    }

    [TestMethod]
    public async Task Complete_CapturesPayment()
    {
        var (patient, _) = await PatientClientWithEmailAsync();
        var doctorId = await CreateDoctorAsync();
        var appointmentId = await BookAppointmentAsync(patient, doctorId);

        var doctorEmail = await GetDoctorEmailAsync(doctorId);
        var doctor = await TestHelpers.GetBearerClientAsync(Factory, doctorEmail, "Doctor1234!");

        var completeResponse = await doctor.PostAsJsonAsync(
            $"/api/v1/appointments/{appointmentId}/complete",
            new { Notes = "Patient is healthy" });
        completeResponse.EnsureSuccessStatusCode();

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HalenDbContext>();
        var payment = await db.Payments.FirstOrDefaultAsync(p => p.AppointmentId == appointmentId);

        payment.Should().NotBeNull();
        payment!.Status.Should().Be(PaymentStatus.Captured);
        payment.CapturedAt.Should().NotBeNull();
    }

    [TestMethod]
    public async Task Cancel_RefundsPayment()
    {
        var (patient, _) = await PatientClientWithEmailAsync();
        var doctorId = await CreateDoctorAsync();
        var appointmentId = await BookAppointmentAsync(patient, doctorId);

        var cancelResponse = await patient.PostAsync($"/api/v1/appointments/{appointmentId}/cancel", null);
        cancelResponse.EnsureSuccessStatusCode();

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HalenDbContext>();
        var payment = await db.Payments.FirstOrDefaultAsync(p => p.AppointmentId == appointmentId);

        payment.Should().NotBeNull();
        payment!.Status.Should().Be(PaymentStatus.Refunded);
        payment.RefundedAt.Should().NotBeNull();
    }

    [TestMethod]
    public async Task GetMine_IncludesPaymentFields()
    {
        var (patient, _) = await PatientClientWithEmailAsync();
        var doctorId = await CreateDoctorAsync();
        await BookAppointmentAsync(patient, doctorId);

        var response = await patient.GetAsync("/api/v1/appointments");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<AppointmentsResponse>();
        body!.Appointments.Should().NotBeEmpty();
        body.Appointments[0].PaymentStatus.Should().NotBeNullOrEmpty();
        body.Appointments[0].PaymentAmount.Should().NotBeNull();
    }

    // ── Doctor Schedule & Pagination Tests ─────────────────────────────────────

    [TestMethod]
    public async Task GetMine_AsDoctor_ReturnsOnlyOwnSchedule()
    {
        var (doctor1Id, doctor1) = await CreateDoctorWithClientAsync("Schedule1");
        var (doctor2Id, doctor2) = await CreateDoctorWithClientAsync("Schedule2");
        var (patient, _) = await PatientClientWithEmailAsync();

        // Book with doctor 1
        var apptId1 = await BookAppointmentAsync(patient, doctor1Id, DateTime.UtcNow.Date.AddDays(10).AddHours(10));
        // Book with doctor 2
        var apptId2 = await BookAppointmentAsync(patient, doctor2Id, DateTime.UtcNow.Date.AddDays(11).AddHours(10));

        // Doctor 1 should only see their own appointment
        var response1 = await doctor1.GetAsync("/api/v1/appointments");
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        var body1 = await response1.Content.ReadAsStringAsync();
        body1.Should().Contain(apptId1.ToString());
        body1.Should().NotContain(apptId2.ToString());

        // Doctor 2 should only see their own appointment
        var response2 = await doctor2.GetAsync("/api/v1/appointments");
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        var body2 = await response2.Content.ReadAsStringAsync();
        body2.Should().Contain(apptId2.ToString());
        body2.Should().NotContain(apptId1.ToString());
    }

    [TestMethod]
    public async Task GetMine_Pagination_ReturnsCorrectPage()
    {
        var (patient, _) = await PatientClientWithEmailAsync();
        var doctorId = await CreateDoctorAsync();

        // Book 3 appointments at different times
        await BookAppointmentAsync(patient, doctorId, DateTime.UtcNow.Date.AddDays(20).AddHours(10));
        await BookAppointmentAsync(patient, doctorId, DateTime.UtcNow.Date.AddDays(21).AddHours(10));
        await BookAppointmentAsync(patient, doctorId, DateTime.UtcNow.Date.AddDays(22).AddHours(10));

        // Request page 1 with pageSize 2
        var response = await patient.GetAsync("/api/v1/appointments?page=1&pageSize=2");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AppointmentsResponse>();
        body!.Appointments.Should().HaveCount(2);
        body.TotalCount.Should().BeGreaterThanOrEqualTo(3);

        // Request page 2 with pageSize 2
        var response2 = await patient.GetAsync("/api/v1/appointments?page=2&pageSize=2");
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        var body2 = await response2.Content.ReadFromJsonAsync<AppointmentsResponse>();
        body2!.Appointments.Should().HaveCountGreaterThanOrEqualTo(1);
    }

    [TestMethod]
    public async Task Cancel_CompletedAppointment_Returns400()
    {
        var (patient, _) = await PatientClientWithEmailAsync();
        var (doctorId, doctorClient) = await CreateDoctorWithClientAsync("CancelComplete");
        var appointmentId = await BookAppointmentAsync(patient, doctorId);

        // Complete the appointment first
        var completeResp = await doctorClient.PostAsJsonAsync(
            $"/api/v1/appointments/{appointmentId}/complete",
            new { Notes = "Done" });
        completeResp.EnsureSuccessStatusCode();

        // Now try to cancel the completed appointment
        var response = await patient.PostAsync($"/api/v1/appointments/{appointmentId}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task Cancel_AsClinicAdmin_Returns403()
    {
        var (patient, _) = await PatientClientWithEmailAsync();
        var doctorId = await CreateDoctorAsync();
        var appointmentId = await BookAppointmentAsync(patient, doctorId);

        // Create a ClinicAdmin user
        var clinicAdminEmail = $"cadmin+{Guid.NewGuid():N}@test.com";
        using (var scope = Factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider
                .GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Halen.Domain.Entities.User>>();
            var db = scope.ServiceProvider.GetRequiredService<Halen.Infrastructure.Persistence.HalenDbContext>();

            var defaultClinic = await db.Clinics.FirstAsync(c => c.Slug == "default");

            var user = new Halen.Domain.Entities.User
            {
                Id = Guid.NewGuid(),
                FirstName = "Clinic",
                LastName = "Admin",
                Email = clinicAdminEmail,
                UserName = clinicAdminEmail,
                Role = Halen.Domain.Enums.UserRole.ClinicAdmin,
                ClinicId = defaultClinic.Id,
                Status = Halen.Domain.Enums.AccountStatus.Active,
            };
            await userManager.CreateAsync(user, "Admin1234!");
            await userManager.AddToRoleAsync(user, "ClinicAdmin");
        }

        var clinicAdminClient = await TestHelpers.GetBearerClientAsync(Factory, clinicAdminEmail, "Admin1234!");

        var response = await clinicAdminClient.PostAsync($"/api/v1/appointments/{appointmentId}/cancel", null);

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
        decimal ConsultationFee, string PatientName, Guid PatientId,
        string? PaymentStatus = null, decimal? PaymentAmount = null);
    private sealed record AppointmentsResponse(AppointmentDto[] Appointments, int TotalCount);
    private sealed record DoctorsResponse(DoctorDto[] Doctors, int TotalCount);
}

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Halen.IntegrationTests.Prescriptions;

[TestClass]
public class PrescriptionsControllerTests : IntegrationTestBase
{
    private static async Task<Guid> GetPatientProfileIdAsync(HttpClient patientClient, Guid doctorProfileId)
    {
        var bookResponse = await patientClient.PostAsJsonAsync("/api/v1/appointments", new
        {
            DoctorId = doctorProfileId,
            ScheduledAt = DateTime.UtcNow.AddDays(30),
            Reason = "Profile lookup",
        });
        bookResponse.EnsureSuccessStatusCode();

        var response = await patientClient.GetAsync("/api/v1/appointments");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<AppointmentsResponse>();
        return body!.Appointments[0].PatientId;
    }

    // ── Issue Tests ──────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Issue_AsDoctor_Returns201WithPrescriptionId()
    {
        var (doctorProfileId, doctor) = await CreateDoctorWithClientAsync();
        var patient = await PatientClientAsync();
        var patientProfileId = await GetPatientProfileIdAsync(patient, doctorProfileId);

        var response = await doctor.PostAsJsonAsync("/api/v1/prescriptions", new
        {
            PatientId = patientProfileId,
            DrugName = "Amoxicillin",
            Dosage = "500mg",
            Frequency = "Twice daily",
            RefillsRemaining = 3,
            PharmacyName = "CVS",
        });

        var responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, responseBody);
    }

    [TestMethod]
    public async Task Issue_AsPatient_Returns403()
    {
        var patient = await PatientClientAsync();

        var response = await patient.PostAsJsonAsync("/api/v1/prescriptions", new
        {
            PatientId = Guid.NewGuid(),
            DrugName = "Amoxicillin",
            Dosage = "500mg",
            Frequency = "Twice daily",
            RefillsRemaining = 3,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [TestMethod]
    public async Task Issue_WithoutAuth_Returns401()
    {
        var anon = Factory.CreateClient();

        var response = await anon.PostAsJsonAsync("/api/v1/prescriptions", new
        {
            PatientId = Guid.NewGuid(),
            DrugName = "Amoxicillin",
            Dosage = "500mg",
            Frequency = "Twice daily",
            RefillsRemaining = 3,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task Issue_WithInvalidData_Returns400()
    {
        var (_, doctor) = await CreateDoctorWithClientAsync();

        var response = await doctor.PostAsJsonAsync("/api/v1/prescriptions", new
        {
            PatientId = Guid.Empty,
            DrugName = "",
            Dosage = "",
            Frequency = "",
            RefillsRemaining = -1,
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Cancel Tests ─────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Cancel_OwnPrescription_Returns200()
    {
        var (doctorProfileId, doctor) = await CreateDoctorWithClientAsync();
        var patient = await PatientClientAsync();
        var patientProfileId = await GetPatientProfileIdAsync(patient, doctorProfileId);

        var issueResponse = await doctor.PostAsJsonAsync("/api/v1/prescriptions", new
        {
            PatientId = patientProfileId,
            DrugName = "Test Drug",
            Dosage = "10mg",
            Frequency = "Daily",
            RefillsRemaining = 0,
        });
        issueResponse.EnsureSuccessStatusCode();
        var issued = await issueResponse.Content.ReadFromJsonAsync<PrescriptionIdResponse>();

        var response = await doctor.PostAsync($"/api/v1/prescriptions/{issued!.PrescriptionId}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [TestMethod]
    public async Task Cancel_OtherDoctorsPrescription_Returns403()
    {
        var (doctorProfileId, doctor1) = await CreateDoctorWithClientAsync();
        var patient = await PatientClientAsync();
        var patientProfileId = await GetPatientProfileIdAsync(patient, doctorProfileId);

        var issueResponse = await doctor1.PostAsJsonAsync("/api/v1/prescriptions", new
        {
            PatientId = patientProfileId,
            DrugName = "Test Drug",
            Dosage = "10mg",
            Frequency = "Daily",
            RefillsRemaining = 0,
        });
        issueResponse.EnsureSuccessStatusCode();
        var issued = await issueResponse.Content.ReadFromJsonAsync<PrescriptionIdResponse>();

        var (_, doctor2) = await CreateDoctorWithClientAsync();

        var response = await doctor2.PostAsync($"/api/v1/prescriptions/{issued!.PrescriptionId}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── List Tests ───────────────────────────────────────────────────────────

    [TestMethod]
    public async Task GetMine_AsDoctor_ReturnsIssuedPrescriptions()
    {
        var (doctorProfileId, doctor) = await CreateDoctorWithClientAsync();
        var patient = await PatientClientAsync();
        var patientProfileId = await GetPatientProfileIdAsync(patient, doctorProfileId);

        await doctor.PostAsJsonAsync("/api/v1/prescriptions", new
        {
            PatientId = patientProfileId,
            DrugName = "List Test Drug",
            Dosage = "20mg",
            Frequency = "Once daily",
            RefillsRemaining = 2,
        });

        var response = await doctor.GetAsync("/api/v1/prescriptions");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var prescriptions = await response.Content.ReadFromJsonAsync<PrescriptionDto[]>();
        prescriptions.Should().NotBeEmpty();
        prescriptions![0].DrugName.Should().NotBeNullOrEmpty();
    }

    [TestMethod]
    public async Task GetMine_AsPatient_ReturnsPrescriptions()
    {
        var (doctorProfileId, doctor) = await CreateDoctorWithClientAsync();
        var patient = await PatientClientAsync();
        var patientProfileId = await GetPatientProfileIdAsync(patient, doctorProfileId);

        await doctor.PostAsJsonAsync("/api/v1/prescriptions", new
        {
            PatientId = patientProfileId,
            DrugName = "Patient View Drug",
            Dosage = "5mg",
            Frequency = "Weekly",
            RefillsRemaining = 1,
        });

        var response = await patient.GetAsync("/api/v1/prescriptions");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var prescriptions = await response.Content.ReadFromJsonAsync<PrescriptionDto[]>();
        prescriptions.Should().NotBeEmpty();
        prescriptions![0].DoctorName.Should().NotBeNullOrEmpty();
    }

    // ── Response DTOs ────────────────────────────────────────────────────────

    private sealed record PrescriptionIdResponse(Guid PrescriptionId);
    private sealed record AppointmentDto(
        Guid Id, DateTime ScheduledAt, int DurationMinutes, string Reason,
        string Status, string? Notes, string DoctorName, string Specialty,
        decimal ConsultationFee, string PatientName, Guid PatientId);
    private sealed record AppointmentsResponse(AppointmentDto[] Appointments, int TotalCount);
    private sealed record PrescriptionDto(
        Guid Id, string DrugName, string Dosage, string Frequency,
        int RefillsRemaining, string Status, string? PharmacyName,
        string DoctorName, string PatientName, DateTime CreatedAt);
}

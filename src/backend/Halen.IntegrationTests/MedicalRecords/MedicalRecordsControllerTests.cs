using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Halen.IntegrationTests.MedicalRecords;

[TestClass]
public class MedicalRecordsControllerTests : IntegrationTestBase
{
    private const string Base = "/api/v1/medical-records";

    // ── Setup helper ────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a doctor and a patient who has booked an appointment with that
    /// doctor, so RecordAccessChecker grants the doctor access to the patient's
    /// medical records.  Returns both clients and the patient profile ID.
    /// </summary>
    private static async Task<(HttpClient Doctor, HttpClient Patient, Guid PatientProfileId, Guid DoctorProfileId)>
        SetupDoctorPatientWithAppointmentAsync()
    {
        var (doctorProfileId, doctor) = await CreateDoctorWithClientAsync();
        var patient = await PatientClientAsync();

        // Book an appointment so the doctor gains record access
        var bookResponse = await patient.PostAsJsonAsync("/api/v1/appointments", new
        {
            DoctorId = doctorProfileId,
            ScheduledAt = DateTime.UtcNow.Date.AddDays(30).AddHours(10),
            Reason = "Medical records access",
        });
        bookResponse.EnsureSuccessStatusCode();

        // Retrieve the patient profile ID from the appointments list
        var apptResponse = await patient.GetAsync("/api/v1/appointments");
        apptResponse.EnsureSuccessStatusCode();
        var apptBody = await apptResponse.Content.ReadFromJsonAsync<AppointmentsResponse>();
        var patientProfileId = apptBody!.Appointments[0].PatientId;

        return (doctor, patient, patientProfileId, doctorProfileId);
    }

    // ── Conditions CRUD ─────────────────────────────────────────────────────

    [TestMethod]
    public async Task AddCondition_AsDoctor_Returns201()
    {
        var (doctor, _, patientId, _) = await SetupDoctorPatientWithAppointmentAsync();

        var response = await doctor.PostAsJsonAsync($"{Base}/{patientId}/conditions", new
        {
            IcdCode = "J06.9",
            IcdDescription = "Acute upper respiratory infection",
            DateOfOnset = "2025-01-15",
            Severity = "Mild",
            Status = "Active",
            ClinicalNotes = "Presented with sore throat and cough",
        });

        var responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, responseBody);

        var body = await response.Content.ReadFromJsonAsync<ConditionIdResponse>();
        body!.ConditionId.Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task AddCondition_AsPatient_Returns201()
    {
        var (_, patient, patientId, _) = await SetupDoctorPatientWithAppointmentAsync();

        var response = await patient.PostAsJsonAsync($"{Base}/{patientId}/conditions", new
        {
            IcdCode = "K21.0",
            IcdDescription = "Gastro-esophageal reflux disease with esophagitis",
            Severity = "Moderate",
            Status = "Active",
        });

        var responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, responseBody);
    }

    [TestMethod]
    public async Task GetConditions_AsDoctor_ReturnsConditions()
    {
        var (doctor, _, patientId, _) = await SetupDoctorPatientWithAppointmentAsync();

        // Seed a condition first
        var addResponse = await doctor.PostAsJsonAsync($"{Base}/{patientId}/conditions", new
        {
            IcdCode = "E11.9",
            IcdDescription = "Type 2 diabetes mellitus",
            Severity = "Moderate",
            Status = "Active",
        });
        addResponse.EnsureSuccessStatusCode();

        var response = await doctor.GetAsync($"{Base}/{patientId}/conditions");
        response.EnsureSuccessStatusCode();

        var conditions = await response.Content.ReadFromJsonAsync<ConditionDto[]>();
        conditions.Should().NotBeEmpty();
        conditions![0].IcdCode.Should().NotBeNullOrEmpty();
    }

    [TestMethod]
    public async Task UpdateCondition_AsDoctor_Returns200()
    {
        var (doctor, _, patientId, _) = await SetupDoctorPatientWithAppointmentAsync();

        var addResponse = await doctor.PostAsJsonAsync($"{Base}/{patientId}/conditions", new
        {
            IcdCode = "J45.0",
            IcdDescription = "Predominantly allergic asthma",
            Severity = "Mild",
            Status = "Active",
        });
        addResponse.EnsureSuccessStatusCode();
        var added = await addResponse.Content.ReadFromJsonAsync<ConditionIdResponse>();

        var response = await doctor.PutAsJsonAsync($"{Base}/conditions/{added!.ConditionId}", new
        {
            Severity = "Severe",
            Status = "Active",
            ClinicalNotes = "Condition has worsened, adjusting treatment plan",
        });

        var responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, responseBody);
    }

    [TestMethod]
    public async Task AddCondition_InvalidData_Returns400()
    {
        var (doctor, _, patientId, _) = await SetupDoctorPatientWithAppointmentAsync();

        var response = await doctor.PostAsJsonAsync($"{Base}/{patientId}/conditions", new
        {
            IcdCode = "",
            IcdDescription = "",
            Severity = "Mild",
            Status = "Active",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Allergies CRUD ──────────────────────────────────────────────────────

    [TestMethod]
    public async Task AddAllergy_AsDoctor_Returns201()
    {
        var (doctor, _, patientId, _) = await SetupDoctorPatientWithAppointmentAsync();

        var response = await doctor.PostAsJsonAsync($"{Base}/{patientId}/allergies", new
        {
            AllergenName = "Penicillin",
            Reaction = "Anaphylaxis",
            Severity = "Severe",
            DateIdentified = "2023-06-10",
        });

        var responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, responseBody);
    }

    [TestMethod]
    public async Task GetAllergies_ReturnsAllAllergies()
    {
        var (doctor, _, patientId, _) = await SetupDoctorPatientWithAppointmentAsync();

        var add1 = await doctor.PostAsJsonAsync($"{Base}/{patientId}/allergies", new
        {
            AllergenName = "Peanuts",
            Reaction = "Hives",
            Severity = "Moderate",
        });
        add1.EnsureSuccessStatusCode();

        var add2 = await doctor.PostAsJsonAsync($"{Base}/{patientId}/allergies", new
        {
            AllergenName = "Latex",
            Reaction = "Contact dermatitis",
            Severity = "Mild",
        });
        add2.EnsureSuccessStatusCode();

        var response = await doctor.GetAsync($"{Base}/{patientId}/allergies");
        response.EnsureSuccessStatusCode();

        var allergies = await response.Content.ReadFromJsonAsync<AllergyDto[]>();
        allergies.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [TestMethod]
    public async Task UpdateAllergy_Returns200()
    {
        var (doctor, _, patientId, _) = await SetupDoctorPatientWithAppointmentAsync();

        var addResponse = await doctor.PostAsJsonAsync($"{Base}/{patientId}/allergies", new
        {
            AllergenName = "Sulfa drugs",
            Reaction = "Rash",
            Severity = "Mild",
        });
        addResponse.EnsureSuccessStatusCode();
        var added = await addResponse.Content.ReadFromJsonAsync<AllergyIdResponse>();

        var response = await doctor.PutAsJsonAsync($"{Base}/allergies/{added!.AllergyId}", new
        {
            Reaction = "Severe rash and swelling",
            Severity = "Severe",
            IsActive = true,
        });

        var responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, responseBody);
    }

    // ── Vitals ──────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task AddVital_AsDoctor_Returns201()
    {
        var (doctor, _, patientId, _) = await SetupDoctorPatientWithAppointmentAsync();

        var response = await doctor.PostAsJsonAsync($"{Base}/{patientId}/vitals", new
        {
            VitalType = "BloodPressure",
            Value = 120m,
            SecondaryValue = 80m,
            Unit = "mmHg",
            MeasuredAt = DateTime.UtcNow,
            Source = "ClinicalEntry",
        });

        var responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, responseBody);
    }

    [TestMethod]
    public async Task GetVitalsHistory_ReturnsReadings()
    {
        var (doctor, _, patientId, _) = await SetupDoctorPatientWithAppointmentAsync();

        // Seed a few vitals
        var add1 = await doctor.PostAsJsonAsync($"{Base}/{patientId}/vitals", new
        {
            VitalType = "HeartRate",
            Value = 72m,
            Unit = "bpm",
            MeasuredAt = DateTime.UtcNow.AddDays(-1),
            Source = "Manual",
        });
        add1.EnsureSuccessStatusCode();

        var add2 = await doctor.PostAsJsonAsync($"{Base}/{patientId}/vitals", new
        {
            VitalType = "HeartRate",
            Value = 78m,
            Unit = "bpm",
            MeasuredAt = DateTime.UtcNow,
            Source = "Manual",
        });
        add2.EnsureSuccessStatusCode();

        var response = await doctor.GetAsync($"{Base}/{patientId}/vitals/HeartRate/history?daysBack=90");
        response.EnsureSuccessStatusCode();

        var readings = await response.Content.ReadFromJsonAsync<VitalReadingDto[]>();
        readings.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    // ── Medications ─────────────────────────────────────────────────────────

    [TestMethod]
    public async Task AddMedication_Returns201()
    {
        var (doctor, _, patientId, _) = await SetupDoctorPatientWithAppointmentAsync();

        var response = await doctor.PostAsJsonAsync($"{Base}/{patientId}/medications", new
        {
            MedicationName = "Metformin",
            Dosage = "500mg",
            Frequency = "Twice daily",
            StartDate = "2025-01-01",
            PrescribedByName = "Dr. Test",
        });

        var responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, responseBody);
    }

    [TestMethod]
    public async Task GetMedications_ReturnsMedications()
    {
        var (doctor, _, patientId, _) = await SetupDoctorPatientWithAppointmentAsync();

        var addResponse = await doctor.PostAsJsonAsync($"{Base}/{patientId}/medications", new
        {
            MedicationName = "Lisinopril",
            Dosage = "10mg",
            Frequency = "Once daily",
            StartDate = "2025-03-01",
        });
        addResponse.EnsureSuccessStatusCode();

        var response = await doctor.GetAsync($"{Base}/{patientId}/medications");
        response.EnsureSuccessStatusCode();

        var medications = await response.Content.ReadFromJsonAsync<MedicationDto[]>();
        medications.Should().NotBeEmpty();
        medications![0].MedicationName.Should().NotBeNullOrEmpty();
    }

    [TestMethod]
    public async Task UpdateMedication_Returns200()
    {
        var (doctor, _, patientId, _) = await SetupDoctorPatientWithAppointmentAsync();

        var addResponse = await doctor.PostAsJsonAsync($"{Base}/{patientId}/medications", new
        {
            MedicationName = "Atorvastatin",
            Dosage = "20mg",
            Frequency = "Once daily at bedtime",
            StartDate = "2025-02-15",
        });
        addResponse.EnsureSuccessStatusCode();
        var added = await addResponse.Content.ReadFromJsonAsync<MedicationIdResponse>();

        var response = await doctor.PutAsJsonAsync($"{Base}/medications/{added!.MedicationId}", new
        {
            Dosage = "40mg",
            Frequency = "Once daily at bedtime",
            IsActive = true,
        });

        var responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, responseBody);
    }

    // ── Family History ──────────────────────────────────────────────────────

    [TestMethod]
    public async Task AddFamilyHistory_Returns201()
    {
        var (doctor, _, patientId, _) = await SetupDoctorPatientWithAppointmentAsync();

        var response = await doctor.PostAsJsonAsync($"{Base}/{patientId}/family-history", new
        {
            Relationship = "Father",
            ConditionName = "Type 2 Diabetes",
            AgeAtOnset = 52,
            Notes = "Managed with insulin",
        });

        var responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, responseBody);
    }

    [TestMethod]
    public async Task GetFamilyHistory_ReturnsEntries()
    {
        var (doctor, _, patientId, _) = await SetupDoctorPatientWithAppointmentAsync();

        var addResponse = await doctor.PostAsJsonAsync($"{Base}/{patientId}/family-history", new
        {
            Relationship = "Mother",
            ConditionName = "Breast cancer",
            AgeAtOnset = 48,
        });
        addResponse.EnsureSuccessStatusCode();

        var response = await doctor.GetAsync($"{Base}/{patientId}/family-history");
        response.EnsureSuccessStatusCode();

        var entries = await response.Content.ReadFromJsonAsync<FamilyHistoryDto[]>();
        entries.Should().NotBeEmpty();
        entries![0].ConditionName.Should().NotBeNullOrEmpty();
    }

    [TestMethod]
    public async Task UpdateFamilyHistory_Returns200()
    {
        var (doctor, _, patientId, _) = await SetupDoctorPatientWithAppointmentAsync();

        var addResponse = await doctor.PostAsJsonAsync($"{Base}/{patientId}/family-history", new
        {
            Relationship = "Sibling",
            ConditionName = "Hypertension",
            AgeAtOnset = 35,
        });
        addResponse.EnsureSuccessStatusCode();
        var added = await addResponse.Content.ReadFromJsonAsync<FamilyHistoryIdResponse>();

        var response = await doctor.PutAsJsonAsync($"{Base}/family-history/{added!.FamilyHistoryId}", new
        {
            ConditionName = "Essential hypertension",
            AgeAtOnset = 33,
            Notes = "Updated after confirming with family",
        });

        var responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, responseBody);
    }

    // ── Documents ───────────────────────────────────────────────────────────

    [TestMethod]
    public async Task GetDocuments_ReturnsDocuments()
    {
        var (doctor, _, patientId, _) = await SetupDoctorPatientWithAppointmentAsync();

        // Upload a document via multipart form
        using var fileContent = new StreamContent(new MemoryStream("Lab results content"u8.ToArray()));
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");

        using var formData = new MultipartFormDataContent
        {
            { fileContent, "file", "lab-results.pdf" },
            { new StringContent("LabResult"), "documentType" },
            { new StringContent("Blood Work Results"), "title" },
            { new StringContent("Annual checkup blood panel"), "description" },
        };

        var uploadResponse = await doctor.PostAsync($"{Base}/{patientId}/documents", formData);
        var uploadBody = await uploadResponse.Content.ReadAsStringAsync();
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.Created, uploadBody);

        // Now retrieve documents
        var response = await doctor.GetAsync($"{Base}/{patientId}/documents");
        response.EnsureSuccessStatusCode();

        var documents = await response.Content.ReadFromJsonAsync<DocumentDto[]>();
        documents.Should().NotBeEmpty();
        documents![0].Title.Should().Be("Blood Work Results");
    }

    // ── Composite Queries ───────────────────────────────────────────────────

    [TestMethod]
    public async Task GetSnapshot_ReturnsFullSnapshot()
    {
        var (doctor, _, patientId, _) = await SetupDoctorPatientWithAppointmentAsync();

        // Seed data across multiple categories
        var conditionTask = doctor.PostAsJsonAsync($"{Base}/{patientId}/conditions", new
        {
            IcdCode = "I10",
            IcdDescription = "Essential hypertension",
            Severity = "Moderate",
            Status = "Active",
        });

        var allergyTask = doctor.PostAsJsonAsync($"{Base}/{patientId}/allergies", new
        {
            AllergenName = "Aspirin",
            Reaction = "GI bleeding",
            Severity = "Severe",
        });

        var medicationTask = doctor.PostAsJsonAsync($"{Base}/{patientId}/medications", new
        {
            MedicationName = "Amlodipine",
            Dosage = "5mg",
            Frequency = "Once daily",
            StartDate = "2025-04-01",
        });

        var responses = await Task.WhenAll(conditionTask, allergyTask, medicationTask);
        foreach (var r in responses)
            r.EnsureSuccessStatusCode();

        var response = await doctor.GetAsync($"{Base}/{patientId}/snapshot");
        response.EnsureSuccessStatusCode();

        var snapshot = await response.Content.ReadFromJsonAsync<SnapshotDto>();
        snapshot.Should().NotBeNull();
        snapshot!.ActiveConditions.Should().NotBeEmpty();
        snapshot.Allergies.Should().NotBeEmpty();
        snapshot.ActiveMedications.Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task GetTimeline_ReturnsMixedEntries()
    {
        var (doctor, _, patientId, _) = await SetupDoctorPatientWithAppointmentAsync();

        // Seed data from different categories so the timeline has mixed entry types
        var conditionAdd = await doctor.PostAsJsonAsync($"{Base}/{patientId}/conditions", new
        {
            IcdCode = "M54.5",
            IcdDescription = "Low back pain",
            Severity = "Moderate",
            Status = "Active",
        });
        conditionAdd.EnsureSuccessStatusCode();

        var vitalAdd = await doctor.PostAsJsonAsync($"{Base}/{patientId}/vitals", new
        {
            VitalType = "Weight",
            Value = 82.5m,
            Unit = "kg",
            MeasuredAt = DateTime.UtcNow,
            Source = "Manual",
        });
        vitalAdd.EnsureSuccessStatusCode();

        var response = await doctor.GetAsync($"{Base}/{patientId}/timeline");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<TimelineResponse>();
        body.Should().NotBeNull();
        body!.Entries.Should().NotBeEmpty();
        body.TotalCount.Should().BeGreaterThanOrEqualTo(2);
    }

    [TestMethod]
    public async Task GetHeader_ReturnsPatientHeader()
    {
        var (doctor, _, patientId, _) = await SetupDoctorPatientWithAppointmentAsync();

        var response = await doctor.GetAsync($"{Base}/{patientId}/header");
        response.EnsureSuccessStatusCode();

        var header = await response.Content.ReadFromJsonAsync<PatientHeaderDto>();
        header.Should().NotBeNull();
        header!.PatientName.Should().NotBeNullOrEmpty();
    }

    // ── Auth / Access ───────────────────────────────────────────────────────

    [TestMethod]
    public async Task AnyEndpoint_WithoutAuth_Returns401()
    {
        var anon = Factory.CreateClient();
        var fakePatientId = Guid.NewGuid();

        var response = await anon.GetAsync($"{Base}/{fakePatientId}/conditions");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task GetConditions_DoctorWithoutAppointment_Returns403()
    {
        // Create a doctor who has no appointment with the patient
        var (_, unrelatedDoctor) = await CreateDoctorWithClientAsync();

        // Create a separate doctor+patient pair to get a real patient ID
        var (_, _, patientId, _) = await SetupDoctorPatientWithAppointmentAsync();

        var response = await unrelatedDoctor.GetAsync($"{Base}/{patientId}/conditions");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Response DTOs ───────────────────────────────────────────────────────

    private sealed record AppointmentDto(
        Guid Id, DateTime ScheduledAt, int DurationMinutes, string Reason,
        string Status, string? Notes, string DoctorName, string Specialty,
        decimal ConsultationFee, string PatientName, Guid PatientId);

    private sealed record AppointmentsResponse(AppointmentDto[] Appointments, int TotalCount);

    private sealed record ConditionIdResponse(Guid ConditionId);
    private sealed record ConditionDto(
        Guid Id, string IcdCode, string IcdDescription, string Severity,
        string Status, string? ClinicalNotes, DateOnly? DateOfOnset);

    private sealed record AllergyIdResponse(Guid AllergyId);
    private sealed record AllergyDto(
        Guid Id, string AllergenName, string? Reaction, string Severity, bool IsActive);

    private sealed record VitalReadingDto(
        Guid Id, string VitalType, decimal Value, decimal? SecondaryValue,
        string Unit, DateTime MeasuredAt, string Source);

    private sealed record MedicationIdResponse(Guid MedicationId);
    private sealed record MedicationDto(
        Guid Id, string MedicationName, string Dosage, string Frequency,
        DateOnly? StartDate, DateOnly? EndDate, bool IsActive);

    private sealed record FamilyHistoryIdResponse(Guid FamilyHistoryId);
    private sealed record FamilyHistoryDto(
        Guid Id, string Relationship, string ConditionName, int? AgeAtOnset, string? Notes);

    private sealed record DocumentDto(
        Guid Id, string Title, string DocumentType, string? Description, long FileSize);

    private sealed record LatestVitalsDto(
        object? BloodPressure, object? HeartRate, object? Weight, object? SpO2);

    private sealed record SnapshotDto(
        AllergyDto[] Allergies,
        ConditionDto[] ActiveConditions,
        MedicationDto[] ActiveMedications,
        FamilyHistoryDto[] FamilyHistory,
        LatestVitalsDto? LatestVitals,
        int OnboardingProgress);

    private sealed record TimelineEntryDto(
        string EntryType, DateTime OccurredAt, string Description);

    private sealed record TimelineResponse(TimelineEntryDto[] Entries, int TotalCount);

    private sealed record PatientHeaderDto(
        Guid PatientProfileId,
        string PatientName,
        string? City,
        string[] AllergyChips,
        string[] ConditionChips);
}

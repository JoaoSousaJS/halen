using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Halen.IntegrationTests.Doctors;

[TestClass]
public class DoctorsControllerTests : IntegrationTestBase
{
    // ── Search Tests ────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Search_NoFilters_ReturnsPaginatedResults()
    {
        var patient = await PatientClientAsync();
        await CreateDoctorWithClientAsync("Paginated");

        var response = await patient.GetAsync("/api/v1/doctors/search");

        var responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, responseBody);
        var body = await response.Content.ReadFromJsonAsync<SearchResponse>();
        body!.Doctors.Should().NotBeEmpty();
        body.TotalCount.Should().BeGreaterThanOrEqualTo(1);
    }

    [TestMethod]
    public async Task Search_ByName_ReturnsFuzzyMatches()
    {
        var patient = await PatientClientAsync();
        await CreateDoctorWithClientAsync("Silva");
        await CreateDoctorWithClientAsync("Jones");

        var response = await patient.GetAsync("/api/v1/doctors/search?search=silva");

        var responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, responseBody);
        var body = await response.Content.ReadFromJsonAsync<SearchResponse>();
        body!.Doctors.Should().NotBeEmpty();
        body.Doctors.Should().AllSatisfy(d => d.Name.Should().ContainEquivalentOf("Silva"));
    }

    [TestMethod]
    public async Task Search_CombinedFilters_SpecialtyAndMaxFee()
    {
        var patient = await PatientClientAsync();

        // Create a Cardiology doctor with fee 200 via admin API directly
        var admin = await AdminClientAsync();
        var createResp = await admin.PostAsJsonAsync("/api/v1/admin/doctors", new
        {
            FirstName = "Dr",
            LastName = "Cardio",
            Email = $"cardio+{Guid.NewGuid():N}@test.com",
            Password = "Doctor1234!",
            Specialty = "Cardiology",
            LicenseNumber = $"LIC-{Guid.NewGuid().ToString("N")[..8]}",
            ConsultationFee = 200.00m,
            YearsOfExperience = 10,
        });
        createResp.EnsureSuccessStatusCode();
        var created = await createResp.Content.ReadFromJsonAsync<DoctorIdResponse>();
        await TestHelpers.ApproveDoctorKycAsync(Factory, created!.DoctorId);

        // Should match: Cardiology with maxFee 250
        var matchResp = await patient.GetAsync("/api/v1/doctors/search?specialty=Cardiology&maxFee=250");
        var matchBody = await matchResp.Content.ReadAsStringAsync();
        matchResp.StatusCode.Should().Be(HttpStatusCode.OK, matchBody);
        var match = await matchResp.Content.ReadFromJsonAsync<SearchResponse>();
        match!.Doctors.Should().NotBeEmpty();
        match.Doctors.Should().AllSatisfy(d =>
        {
            d.Specialty.Should().Be("Cardiology");
            d.ConsultationFee.Should().BeLessThanOrEqualTo(250);
        });

        // Should NOT match: Cardiology with maxFee 50
        var noMatchResp = await patient.GetAsync("/api/v1/doctors/search?specialty=Cardiology&maxFee=50");
        var noMatchBody = await noMatchResp.Content.ReadAsStringAsync();
        noMatchResp.StatusCode.Should().Be(HttpStatusCode.OK, noMatchBody);
        var noMatch = await noMatchResp.Content.ReadFromJsonAsync<SearchResponse>();
        noMatch!.Doctors.Should().BeEmpty();
        noMatch.TotalCount.Should().Be(0);
    }

    // ── Specialties Tests ───────────────────────────────────────────────────

    [TestMethod]
    public async Task Specialties_ReturnsDistinctList()
    {
        var patient = await PatientClientAsync();

        // Ensure we have at least a General doctor
        await CreateDoctorWithClientAsync("SpecGeneral");

        // Create a Cardiology doctor
        var admin = await AdminClientAsync();
        var createResp = await admin.PostAsJsonAsync("/api/v1/admin/doctors", new
        {
            FirstName = "Dr",
            LastName = "SpecCardio",
            Email = $"speccardio+{Guid.NewGuid():N}@test.com",
            Password = "Doctor1234!",
            Specialty = "Cardiology",
            LicenseNumber = $"LIC-{Guid.NewGuid().ToString("N")[..8]}",
            ConsultationFee = 150.00m,
            YearsOfExperience = 8,
        });
        createResp.EnsureSuccessStatusCode();
        var created = await createResp.Content.ReadFromJsonAsync<DoctorIdResponse>();
        await TestHelpers.ApproveDoctorKycAsync(Factory, created!.DoctorId);

        var response = await patient.GetAsync("/api/v1/doctors/specialties");

        var responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, responseBody);
        var body = await response.Content.ReadFromJsonAsync<SpecialtiesResponse>();
        body!.Specialties.Should().Contain("General");
        body.Specialties.Should().Contain("Cardiology");
        body.Specialties.Should().OnlyHaveUniqueItems();
    }

    // ── GetProfile Tests ──────────────────────────────────────────────────

    [TestMethod]
    public async Task GetProfile_ApprovedDoctor_ReturnsFullProfile()
    {
        var patient = await PatientClientAsync();
        var (doctorId, _) = await CreateDoctorWithClientAsync("ProfileDoc");

        var response = await patient.GetAsync($"/api/v1/doctors/{doctorId}/profile");

        var responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, responseBody);
        var body = await response.Content.ReadFromJsonAsync<ProfileResponse>();
        body!.Doctor.Should().NotBeNull();
        body.Doctor.Name.Should().Contain("ProfileDoc");
        body.Doctor.Specialty.Should().Be("General");
        body.Doctor.ConsultationFee.Should().Be(100);
        body.Availability.Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task GetProfile_NonExistentId_Returns404()
    {
        var patient = await PatientClientAsync();

        var response = await patient.GetAsync($"/api/v1/doctors/{Guid.NewGuid()}/profile");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task GetProfile_UnapprovedDoctor_Returns404()
    {
        var patient = await PatientClientAsync();
        var (doctorId, _) = await CreateDoctorWithClientAsync("Unapproved", approveKyc: false);

        var response = await patient.GetAsync($"/api/v1/doctors/{doctorId}/profile");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task GetProfile_WithoutAuth_Returns401()
    {
        var anon = Factory.CreateClient();
        var (doctorId, _) = await CreateDoctorWithClientAsync("AnonProfile");

        var response = await anon.GetAsync($"/api/v1/doctors/{doctorId}/profile");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task GetProfile_AsDoctorRole_Returns403()
    {
        var (doctorId, doctorClient) = await CreateDoctorWithClientAsync("SelfProfile");

        var response = await doctorClient.GetAsync($"/api/v1/doctors/{doctorId}/profile");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [TestMethod]
    public async Task GetProfile_ReviewPagination_ReturnsCorrectPage()
    {
        var (doctorId, doctor) = await CreateDoctorWithClientAsync("PaginatedReviews");
        var patient = await PatientClientAsync();

        for (var i = 0; i < 3; i++)
        {
            var bookResp = await patient.PostAsJsonAsync("/api/v1/appointments", new
            {
                DoctorId = doctorId,
                ScheduledAt = DateTime.UtcNow.Date.AddDays(1 + i).AddHours(10),
                Reason = $"Review visit {i}",
            });
            bookResp.EnsureSuccessStatusCode();
            var booked = await bookResp.Content.ReadFromJsonAsync<BookResponse>();

            var completeResp = await doctor.PostAsJsonAsync(
                $"/api/v1/appointments/{booked!.AppointmentId}/complete",
                new { Notes = "Done" });
            completeResp.EnsureSuccessStatusCode();

            var reviewResp = await patient.PostAsJsonAsync("/api/v1/reviews", new
            {
                AppointmentId = booked.AppointmentId,
                Rating = 5 - i,
                Title = $"Review {i}",
                Body = $"Body {i}",
                Tags = new[] { "thorough" },
            });
            reviewResp.EnsureSuccessStatusCode();
        }

        await TestHelpers.ApproveAllPendingReviewsAsync(Factory, doctorId);

        var page1 = await patient.GetAsync($"/api/v1/doctors/{doctorId}/profile?reviewPage=1&reviewPageSize=2");
        page1.StatusCode.Should().Be(HttpStatusCode.OK);
        var body1 = await page1.Content.ReadFromJsonAsync<ProfileResponse>();
        body1!.Reviews.Should().HaveCount(2);
        body1.ReviewTotalCount.Should().Be(3);

        var page2 = await patient.GetAsync($"/api/v1/doctors/{doctorId}/profile?reviewPage=2&reviewPageSize=2");
        page2.StatusCode.Should().Be(HttpStatusCode.OK);
        var body2 = await page2.Content.ReadFromJsonAsync<ProfileResponse>();
        body2!.Reviews.Should().HaveCount(1);
        body2.ReviewTotalCount.Should().Be(3);
    }

    // ── Auth / Role Tests ───────────────────────────────────────────────────

    [TestMethod]
    public async Task Search_WithoutAuth_Returns401()
    {
        var anon = Factory.CreateClient();

        var response = await anon.GetAsync("/api/v1/doctors/search");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task Search_AsDoctorRole_Returns403()
    {
        var (_, doctorClient) = await CreateDoctorWithClientAsync("Forbidden");

        var response = await doctorClient.GetAsync("/api/v1/doctors/search");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Response DTOs ───────────────────────────────────────────────────────

    private sealed record DoctorSearchDto(
        Guid Id,
        string Name,
        string Specialty,
        decimal ConsultationFee,
        int YearsOfExperience,
        string[] Languages,
        NextSlotDto? NextAvailableSlot,
        decimal? AverageRating,
        int ReviewCount);

    private sealed record NextSlotDto(DateTime StartUtc, string DayOfWeek);

    private sealed record SearchResponse(DoctorSearchDto[] Doctors, int TotalCount);

    private sealed record SpecialtiesResponse(string[] Specialties);

    private sealed record BookResponse(Guid AppointmentId);

    private sealed record ProfileDoctorDto(
        Guid Id, string Name, string Specialty, decimal ConsultationFee,
        int YearsOfExperience, string[] Languages, decimal? AverageRating, int ReviewCount);

    private sealed record AvailabilityDayDto(string DayOfWeek, TimeWindowDto[] Windows);

    private sealed record TimeWindowDto(string StartTime, string EndTime, int SlotDurationMinutes);

    private sealed record ReviewDto(
        Guid Id, int Rating, string Title, string? Body, string[] Tags,
        string PostedAs, int HelpfulCount, string? DoctorResponse,
        DateTime? DoctorRespondedAt, DateTime CreatedAt);

    private sealed record ReviewsSummaryDto(decimal? AverageRating, int TotalCount);

    private sealed record ProfileResponse(
        ProfileDoctorDto Doctor,
        AvailabilityDayDto[] Availability,
        ReviewsSummaryDto? ReviewsSummary,
        ReviewDto[] Reviews,
        int ReviewTotalCount);
}

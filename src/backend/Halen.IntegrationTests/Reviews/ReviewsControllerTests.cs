using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Halen.IntegrationTests.Reviews;

[TestClass]
public class ReviewsControllerTests : IntegrationTestBase
{
    private static async Task<(HttpClient Patient, HttpClient Doctor, Guid DoctorProfileId, Guid AppointmentId)>
        SetupCompletedAppointmentAsync()
    {
        var (doctorProfileId, doctor) = await CreateDoctorWithClientAsync("ReviewDoc");
        var patient = await PatientClientAsync();

        var bookResponse = await patient.PostAsJsonAsync("/api/v1/appointments", new
        {
            DoctorId = doctorProfileId,
            ScheduledAt = DateTime.UtcNow.Date.AddDays(1).AddHours(10),
            Reason = "Review checkup",
        });
        bookResponse.EnsureSuccessStatusCode();
        var booked = await bookResponse.Content.ReadFromJsonAsync<BookResponse>();

        var completeResponse = await doctor.PostAsJsonAsync(
            $"/api/v1/appointments/{booked!.AppointmentId}/complete",
            new { Notes = "All good" });
        completeResponse.EnsureSuccessStatusCode();

        return (patient, doctor, doctorProfileId, booked.AppointmentId);
    }

    private static async Task<Guid> SubmitReviewAsync(HttpClient patient, Guid appointmentId, int rating = 5)
    {
        var response = await patient.PostAsJsonAsync("/api/v1/reviews", new
        {
            AppointmentId = appointmentId,
            Rating = rating,
            Title = "Great experience",
            Body = "Very thorough examination",
            Tags = new[] { "listens", "thorough" },
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ReviewIdResponse>();
        return body!.ReviewId;
    }

    // ── Submit Tests ─────────────────────────────────────────────────────────

    [TestMethod]
    public async Task SubmitReview_AsPatient_Returns201()
    {
        var (patient, _, doctorProfileId, appointmentId) = await SetupCompletedAppointmentAsync();

        var response = await patient.PostAsJsonAsync("/api/v1/reviews", new
        {
            AppointmentId = appointmentId,
            Rating = 5,
            Title = "Excellent doctor",
            Body = "Very helpful",
            Tags = new[] { "listens", "on time" },
        });

        var responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, responseBody);
    }

    [TestMethod]
    public async Task SubmitReview_AsDoctor_Returns403()
    {
        var (_, doctor, _, appointmentId) = await SetupCompletedAppointmentAsync();

        var response = await doctor.PostAsJsonAsync("/api/v1/reviews", new
        {
            AppointmentId = appointmentId,
            Rating = 5,
            Title = "Self review",
            Body = "",
            Tags = Array.Empty<string>(),
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [TestMethod]
    public async Task SubmitReview_WithoutAuth_Returns401()
    {
        var anon = Factory.CreateClient();

        var response = await anon.PostAsJsonAsync("/api/v1/reviews", new
        {
            AppointmentId = Guid.NewGuid(),
            Rating = 5,
            Title = "Anonymous review",
            Body = "",
            Tags = Array.Empty<string>(),
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task SubmitReview_InvalidData_Returns400()
    {
        var patient = await PatientClientAsync();

        var response = await patient.PostAsJsonAsync("/api/v1/reviews", new
        {
            AppointmentId = Guid.Empty,
            Rating = 0,
            Title = "",
            Body = "",
            Tags = Array.Empty<string>(),
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task SubmitReview_DuplicateReview_Returns400()
    {
        var (patient, _, _, appointmentId) = await SetupCompletedAppointmentAsync();

        await SubmitReviewAsync(patient, appointmentId);

        var response = await patient.PostAsJsonAsync("/api/v1/reviews", new
        {
            AppointmentId = appointmentId,
            Rating = 3,
            Title = "Second attempt",
            Body = "",
            Tags = Array.Empty<string>(),
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task SubmitReview_NonCompletedAppointment_Returns400()
    {
        var (doctorProfileId, _) = await CreateDoctorWithClientAsync("NonComplete");
        var patient = await PatientClientAsync();

        var bookResponse = await patient.PostAsJsonAsync("/api/v1/appointments", new
        {
            DoctorId = doctorProfileId,
            ScheduledAt = DateTime.UtcNow.Date.AddDays(2).AddHours(10),
            Reason = "Not completed yet",
        });
        bookResponse.EnsureSuccessStatusCode();
        var booked = await bookResponse.Content.ReadFromJsonAsync<BookResponse>();

        var response = await patient.PostAsJsonAsync("/api/v1/reviews", new
        {
            AppointmentId = booked!.AppointmentId,
            Rating = 5,
            Title = "Too early",
            Body = "",
            Tags = Array.Empty<string>(),
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── GetDoctorReviews Tests ───────────────────────────────────────────────

    [TestMethod]
    public async Task GetDoctorReviews_ReturnsReviewsAndAggregates()
    {
        var (patient, _, doctorProfileId, appointmentId) = await SetupCompletedAppointmentAsync();
        await SubmitReviewAsync(patient, appointmentId);

        var response = await patient.GetAsync($"/api/v1/reviews/doctor/{doctorProfileId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DoctorReviewsResponse>();
        body!.Reviews.Should().NotBeEmpty();
        body.AverageRating.Should().NotBeNull();
        body.ReviewCount.Should().BeGreaterThan(0);
        body.RatingBreakdown.Should().HaveCount(5);
    }

    [TestMethod]
    public async Task GetDoctorReviews_PaginationWorks()
    {
        var (patient, doctor, doctorProfileId, appointmentId) = await SetupCompletedAppointmentAsync();
        await SubmitReviewAsync(patient, appointmentId);

        var response = await patient.GetAsync($"/api/v1/reviews/doctor/{doctorProfileId}?page=1&pageSize=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DoctorReviewsResponse>();
        body!.Reviews.Should().HaveCount(1);
    }

    // ── RespondToReview Tests ────────────────────────────────────────────────

    [TestMethod]
    public async Task RespondToReview_AsDoctor_Returns200()
    {
        var (patient, doctor, _, appointmentId) = await SetupCompletedAppointmentAsync();
        var reviewId = await SubmitReviewAsync(patient, appointmentId);

        var response = await doctor.PostAsJsonAsync($"/api/v1/reviews/{reviewId}/respond", new
        {
            Response = "Thank you for the kind words!"
        });

        var responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, responseBody);
    }

    [TestMethod]
    public async Task RespondToReview_AsWrongDoctor_Returns403()
    {
        var (patient, _, _, appointmentId) = await SetupCompletedAppointmentAsync();
        var reviewId = await SubmitReviewAsync(patient, appointmentId);

        var (_, otherDoctor) = await CreateDoctorWithClientAsync("OtherDoc");

        var response = await otherDoctor.PostAsJsonAsync($"/api/v1/reviews/{reviewId}/respond", new
        {
            Response = "Not my review"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [TestMethod]
    public async Task RespondToReview_AlreadyResponded_Returns400()
    {
        var (patient, doctor, _, appointmentId) = await SetupCompletedAppointmentAsync();
        var reviewId = await SubmitReviewAsync(patient, appointmentId);

        await doctor.PostAsJsonAsync($"/api/v1/reviews/{reviewId}/respond", new
        {
            Response = "First response"
        });

        var response = await doctor.PostAsJsonAsync($"/api/v1/reviews/{reviewId}/respond", new
        {
            Response = "Second response"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── VoteHelpful Tests ────────────────────────────────────────────────────

    [TestMethod]
    public async Task VoteHelpful_Returns200WithNewCount()
    {
        var (patient, _, _, appointmentId) = await SetupCompletedAppointmentAsync();
        var reviewId = await SubmitReviewAsync(patient, appointmentId);

        var response = await patient.PostAsync($"/api/v1/reviews/{reviewId}/helpful", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<HelpfulResponse>();
        body!.NewCount.Should().Be(1);
    }

    // ── GetMyReviews Tests ───────────────────────────────────────────────────

    [TestMethod]
    public async Task GetMyReviews_AsDoctor_ReturnsOwnReviews()
    {
        var (patient, doctor, _, appointmentId) = await SetupCompletedAppointmentAsync();
        await SubmitReviewAsync(patient, appointmentId);

        var response = await doctor.GetAsync("/api/v1/doctor/reviews");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<MyReviewsResponse>();
        body!.Reviews.Should().NotBeEmpty();
    }

    // ── Moderation Tests ─────────────────────────────────────────────────────

    [TestMethod]
    public async Task GetModerationQueue_AsAdmin_Returns200()
    {
        var admin = await AdminClientAsync();

        var response = await admin.GetAsync("/api/v1/admin/reviews/moderation?filter=all");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [TestMethod]
    public async Task ModerateReview_AsAdmin_Returns200()
    {
        var (patient, _, _, appointmentId) = await SetupCompletedAppointmentAsync();
        var reviewId = await SubmitReviewAsync(patient, appointmentId);

        var admin = await AdminClientAsync();

        var response = await admin.PostAsJsonAsync($"/api/v1/admin/reviews/{reviewId}/moderate", new
        {
            Decision = "Hidden"
        });

        var responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, responseBody);
    }

    [TestMethod]
    public async Task ModerateReview_AsPatient_Returns403()
    {
        var patient = await PatientClientAsync();

        var response = await patient.PostAsJsonAsync($"/api/v1/admin/reviews/{Guid.NewGuid()}/moderate", new
        {
            Decision = "Hidden"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Response DTOs ────────────────────────────────────────────────────────

    private sealed record BookResponse(Guid AppointmentId);
    private sealed record ReviewIdResponse(Guid ReviewId);
    private sealed record HelpfulResponse(int NewCount);
    private sealed record DoctorReviewsResponse(
        ReviewDto[] Reviews, int TotalCount, decimal? AverageRating,
        int ReviewCount, RatingBreakdownDto[] RatingBreakdown, TagCountDto[] TopTags);
    private sealed record ReviewDto(
        Guid Id, int Rating, string Title, string Body, string[] Tags,
        string PostedAs, int HelpfulCount, string? DoctorResponse);
    private sealed record RatingBreakdownDto(int Stars, int Count);
    private sealed record TagCountDto(string Tag, int Count);
    private sealed record MyReviewsResponse(DoctorReviewItemDto[] Reviews, int TotalCount);
    private sealed record DoctorReviewItemDto(Guid Id, int Rating, string Title);
}

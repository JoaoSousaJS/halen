using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Halen.Application.Analytics.Queries;

namespace Halen.IntegrationTests.Analytics;

[TestClass]
public class AnalyticsControllerTests : IntegrationTestBase
{
    [TestMethod]
    public async Task Overview_AsPlatformAdmin_Returns200()
    {
        var client = await AdminClientAsync();
        var response = await client.GetAsync("/api/v1/analytics/overview");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<AnalyticsOverviewResult>();
        body.Should().NotBeNull();
        body!.AppointmentKpi.Should().NotBeNull();
        body.Funnel.Should().NotBeNull();
        body.ClinicBreakdown.Should().NotBeNull();
    }

    [TestMethod]
    public async Task Overview_AsPatient_Returns403()
    {
        var client = await PatientClientAsync();
        var response = await client.GetAsync("/api/v1/analytics/overview");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [TestMethod]
    public async Task Overview_AsDoctor_Returns403()
    {
        var (_, client) = await CreateDoctorWithClientAsync();
        var response = await client.GetAsync("/api/v1/analytics/overview");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [TestMethod]
    public async Task Overview_WithoutAuth_Returns401()
    {
        var client = Factory.CreateClient();
        var response = await client.GetAsync("/api/v1/analytics/overview");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    [DataRow("7d")]
    [DataRow("30d")]
    [DataRow("90d")]
    [DataRow("ytd")]
    public async Task Overview_WithPeriodParam_Returns200(string period)
    {
        var client = await AdminClientAsync();
        var response = await client.GetAsync($"/api/v1/analytics/overview?period={period}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [TestMethod]
    public async Task Appointments_AsPlatformAdmin_Returns200()
    {
        var client = await AdminClientAsync();
        var response = await client.GetAsync("/api/v1/analytics/appointments");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<AppointmentAnalyticsResult>();
        body.Should().NotBeNull();
        body!.ByDayOfWeek.Should().HaveCount(7);
        body.ByHourOfDay.Should().HaveCount(24);
    }

    [TestMethod]
    public async Task Revenue_AsPlatformAdmin_Returns200()
    {
        var client = await AdminClientAsync();
        var response = await client.GetAsync("/api/v1/analytics/revenue");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<RevenueAnalyticsResult>();
        body.Should().NotBeNull();
        body!.PaymentStatusBreakdown.Should().NotBeNull();
        body.ClinicRevenue.Should().NotBeNull();
    }

    [TestMethod]
    public async Task Heatmap_AsPlatformAdmin_Returns200()
    {
        var client = await AdminClientAsync();
        var response = await client.GetAsync("/api/v1/analytics/heatmap");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<HeatmapAnalyticsResult>();
        body.Should().NotBeNull();
        body!.Grid.Should().HaveCount(7);
        foreach (var row in body.Grid)
            row.Should().HaveCount(24);
    }

    [TestMethod]
    public async Task Doctors_AsPlatformAdmin_Returns200()
    {
        var client = await AdminClientAsync();
        var response = await client.GetAsync("/api/v1/analytics/doctors");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<DoctorAnalyticsResult>();
        body.Should().NotBeNull();
        body!.Ranked.Should().NotBeNull();
        body.TopRated.Should().NotBeNull();
        body.NeedsAttention.Should().NotBeNull();
    }

    [TestMethod]
    public async Task Geography_AsPlatformAdmin_Returns200()
    {
        var client = await AdminClientAsync();
        var response = await client.GetAsync("/api/v1/analytics/geography");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<GeographyAnalyticsResult>();
        body.Should().NotBeNull();
        body!.Regions.Should().NotBeNull();
        body.Retention.Should().NotBeNull();
    }
}

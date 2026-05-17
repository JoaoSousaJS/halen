using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Halen.IntegrationTests.Clinics;

[TestClass]
public class ClinicsControllerTests : IntegrationTestBase
{
    [TestMethod]
    public async Task CreateClinic_AsAdmin_ReturnsCreated()
    {
        var client = await AdminClientAsync();
        var slug = $"clinic-{Guid.NewGuid():N}"[..20];

        var response = await client.PostAsJsonAsync("/api/v1/clinics", new
        {
            Name = "Test Clinic",
            Slug = slug,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ClinicIdResponse>();
        body!.ClinicId.Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task CreateClinic_DuplicateSlug_Returns400()
    {
        var client = await AdminClientAsync();
        var slug = $"dup-{Guid.NewGuid():N}"[..20];

        await client.PostAsJsonAsync("/api/v1/clinics", new { Name = "First", Slug = slug });
        var response = await client.PostAsJsonAsync("/api/v1/clinics", new { Name = "Second", Slug = slug });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task CreateClinic_AsPatient_Returns403()
    {
        var client = await PatientClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/clinics", new
        {
            Name = "Hacked",
            Slug = "hacked",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [TestMethod]
    public async Task ListClinics_AsAdmin_ReturnsResults()
    {
        var client = await AdminClientAsync();

        var response = await client.GetAsync("/api/v1/clinics");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListClinicsResponse>();
        body!.Clinics.Should().NotBeEmpty();
        body.TotalCount.Should().BeGreaterThan(0);
    }

    [TestMethod]
    public async Task UpdateClinic_DeactivateAndReactivate_ReturnsNoContent()
    {
        var client = await AdminClientAsync();
        var slug = $"upd-{Guid.NewGuid():N}"[..20];

        var createResp = await client.PostAsJsonAsync("/api/v1/clinics", new { Name = "Updatable", Slug = slug });
        var created = await createResp.Content.ReadFromJsonAsync<ClinicIdResponse>();

        var updateResp = await client.PutAsJsonAsync($"/api/v1/clinics/{created!.ClinicId}", new
        {
            Name = "Updated Name",
            IsActive = false,
        });

        updateResp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [TestMethod]
    public async Task SetFeatureFlag_Toggle_ReturnsNoContent()
    {
        var client = await AdminClientAsync();
        var slug = $"feat-{Guid.NewGuid():N}"[..20];

        var createResp = await client.PostAsJsonAsync("/api/v1/clinics", new { Name = "Feature Test", Slug = slug });
        var created = await createResp.Content.ReadFromJsonAsync<ClinicIdResponse>();

        var resp = await client.PutAsJsonAsync(
            $"/api/v1/clinics/{created!.ClinicId}/features/prescriptions",
            new { IsEnabled = true });

        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [TestMethod]
    public async Task GetMyFeatures_AsPatient_ReturnsFlags()
    {
        var client = await PatientClientAsync();

        var response = await client.GetAsync("/api/v1/me/features");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var flags = await response.Content.ReadFromJsonAsync<FeatureFlagDto[]>();
        flags.Should().NotBeNull();
    }

    [TestMethod]
    public async Task SetFeatureFlag_InvalidKey_Returns400()
    {
        var client = await AdminClientAsync();
        var slug = $"inv-{Guid.NewGuid():N}"[..20];

        var createResp = await client.PostAsJsonAsync("/api/v1/clinics", new { Name = "Invalid", Slug = slug });
        var created = await createResp.Content.ReadFromJsonAsync<ClinicIdResponse>();

        var resp = await client.PutAsJsonAsync(
            $"/api/v1/clinics/{created!.ClinicId}/features/nonexistent_feature",
            new { IsEnabled = true });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private sealed record ClinicIdResponse(string ClinicId);
    private sealed record ListClinicsResponse(ClinicDto[] Clinics, int TotalCount);
    private sealed record ClinicDto(string Id, string Name, string Slug, bool IsActive, string CreatedAt);
    private sealed record FeatureFlagDto(string FeatureKey, bool IsEnabled);
}

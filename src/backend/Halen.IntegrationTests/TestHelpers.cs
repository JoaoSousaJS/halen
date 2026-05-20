using System.Net.Http.Headers;
using System.Net.Http.Json;
using Halen.Domain.Enums;
using Halen.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Halen.IntegrationTests;

internal static class TestHelpers
{
    /// <summary>
    /// Creates an HttpClient with the Bearer token obtained by logging in with
    /// the supplied credentials. Throws if login does not return 200.
    /// </summary>
    public static async Task<HttpClient> GetBearerClientAsync(
        HalenWebApplicationFactory factory,
        string email,
        string password)
    {
        var loginClient = factory.CreateClient();

        var response = await loginClient.PostAsJsonAsync("/api/v1/auth/login", new
        {
            Email    = email,
            Password = password,
        });

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<TokenResponse>()
            ?? throw new InvalidOperationException("Login returned null body.");

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body.Token);

        return client;
    }

    public static async Task ApproveDoctorKycAsync(HalenWebApplicationFactory factory, Guid doctorProfileId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HalenDbContext>();
        var doctor = await db.DoctorProfiles.Include(d => d.User).FirstAsync(d => d.Id == doctorProfileId);
        doctor.KycStatus = KycStatus.Approved;
        doctor.User.Status = AccountStatus.Active;
        await db.SaveChangesAsync();
    }

    public static async Task ApproveAllPendingReviewsAsync(HalenWebApplicationFactory factory, Guid doctorProfileId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HalenDbContext>();
        var pending = await db.Reviews
            .Where(r => r.DoctorProfileId == doctorProfileId
                && r.ModerationStatus == ReviewModerationStatus.Pending)
            .ToListAsync();
        foreach (var review in pending)
            review.ModerationStatus = ReviewModerationStatus.Approved;
        await db.SaveChangesAsync();
    }

    private sealed record TokenResponse(string Token);
}

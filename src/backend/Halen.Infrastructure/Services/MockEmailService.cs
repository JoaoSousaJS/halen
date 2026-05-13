using Halen.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Halen.Infrastructure.Services;

public class MockEmailService(ILogger<MockEmailService> logger) : IEmailService
{
    public Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        logger.LogInformation("[MOCK EMAIL] To: {To} | Subject: {Subject} | Body: {Body}", to, subject, body);
        return Task.CompletedTask;
    }
}

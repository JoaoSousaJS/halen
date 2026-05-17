using Halen.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Halen.Infrastructure.Services;

public class MockPaymentService(ILogger<MockPaymentService> logger) : IPaymentService
{
    public Task<PaymentIntentResult> CreateIntentAsync(Guid userId, decimal amount, string currency, string idempotencyKey, CancellationToken ct = default)
    {
        if (idempotencyKey.Contains("FAIL", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("[MOCK PAYMENT] Simulated failure for idempotencyKey={Key}", idempotencyKey);
            return Task.FromResult(new PaymentIntentResult(false, null, "Simulated payment failure"));
        }

        var intentId = $"mock_intent_{Guid.NewGuid():N}";
        logger.LogInformation(
            "[MOCK PAYMENT] CreateIntent ${Amount} {Currency} for user {UserId}, idempotencyKey={Key} — intentId: {IntentId}",
            amount, currency, userId, idempotencyKey, intentId);
        return Task.FromResult(new PaymentIntentResult(true, intentId));
    }

    public Task<PaymentCaptureResult> CaptureIntentAsync(string paymentIntentId, CancellationToken ct = default)
    {
        logger.LogInformation("[MOCK PAYMENT] Capture intent: {IntentId}", paymentIntentId);
        return Task.FromResult(new PaymentCaptureResult(true));
    }

    public Task<PaymentRefundResult> RefundIntentAsync(string paymentIntentId, CancellationToken ct = default)
    {
        logger.LogInformation("[MOCK PAYMENT] Refund intent: {IntentId}", paymentIntentId);
        return Task.FromResult(new PaymentRefundResult(true));
    }
}

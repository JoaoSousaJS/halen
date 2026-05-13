using Halen.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Halen.Infrastructure.Services;

public class MockPaymentService(ILogger<MockPaymentService> logger) : IPaymentService
{
    public Task<PaymentResult> ChargeAsync(Guid userId, decimal amount, string description, CancellationToken ct = default)
    {
        var txId = $"mock_tx_{Guid.NewGuid():N}";
        logger.LogInformation("[MOCK PAYMENT] Charge ${Amount} for user {UserId} — txId: {TxId}", amount, userId, txId);
        return Task.FromResult(new PaymentResult(true, txId));
    }

    public Task<PaymentResult> RefundAsync(string transactionId, CancellationToken ct = default)
    {
        logger.LogInformation("[MOCK PAYMENT] Refund for txId: {TxId}", transactionId);
        return Task.FromResult(new PaymentResult(true, transactionId));
    }
}

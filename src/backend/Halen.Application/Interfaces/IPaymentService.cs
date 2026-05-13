namespace Halen.Application.Interfaces;

public record PaymentResult(bool Success, string TransactionId, string? ErrorMessage = null);

public interface IPaymentService
{
    Task<PaymentResult> ChargeAsync(Guid userId, decimal amount, string description, CancellationToken ct = default);
    Task<PaymentResult> RefundAsync(string transactionId, CancellationToken ct = default);
}

namespace Halen.Application.Interfaces;

public record PaymentIntentResult(bool Success, string? PaymentIntentId, string? ErrorMessage = null);
public record PaymentCaptureResult(bool Success, string? ErrorMessage = null);
public record PaymentRefundResult(bool Success, string? ErrorMessage = null);

public interface IPaymentService
{
    Task<PaymentIntentResult> CreateIntentAsync(Guid userId, decimal amount, string currency, string idempotencyKey, CancellationToken ct = default);
    Task<PaymentCaptureResult> CaptureIntentAsync(string paymentIntentId, CancellationToken ct = default);
    Task<PaymentRefundResult> RefundIntentAsync(string paymentIntentId, CancellationToken ct = default);
}

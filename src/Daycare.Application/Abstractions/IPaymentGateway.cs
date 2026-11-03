namespace Daycare.Application.Abstractions;

public interface IPaymentGateway
{
    Task<PaymentGatewayResult> ProcessPaymentAsync(
        decimal amount,
        string currency,
        string reference,
        CancellationToken ct = default);
}

public record PaymentGatewayResult(
    bool Success,
    string? TransactionReference = null,
    string? ErrorMessage = null
);

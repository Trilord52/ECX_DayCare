using Daycare.Application.Abstractions;

namespace Daycare.Infrastructure.Services;

public class FakePaymentGateway : IPaymentGateway
{
    public Task<PaymentGatewayResult> ProcessPaymentAsync(
        decimal amount,
        string currency,
        string reference,
        CancellationToken ct = default)
    {
        var txnRef = $"FAKE-{Guid.NewGuid():N}";
        return Task.FromResult(new PaymentGatewayResult(true, txnRef));
    }
}


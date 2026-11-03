using Daycare.Application.Features.Billing.Invoices.DTOs;
using Daycare.Application.Features.Billing.Payments.VoidPayment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Daycare.Api.Controllers;

[Authorize(Roles = "Admin")]
public class PaymentsController : BaseApiController
{
    private readonly VoidPaymentUseCase _voidPaymentUseCase;

    public PaymentsController(VoidPaymentUseCase voidPaymentUseCase)
    {
        _voidPaymentUseCase = voidPaymentUseCase;
    }

    public record VoidPaymentBody(string Reason);

    [HttpPost("{id:guid}/void")]
    public async Task<ActionResult<PaymentDto>> VoidPayment(
        Guid id,
        [FromBody] VoidPaymentBody body,
        CancellationToken ct)
    {
        var response = await _voidPaymentUseCase.ExecuteAsync(
            new VoidPaymentRequest(id, body.Reason), ct);
        return Ok(response);
    }
}


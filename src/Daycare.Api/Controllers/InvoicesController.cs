using Daycare.Application.Common.Models;
using Daycare.Application.Features.Billing.Invoices.AddInvoiceAdjustment;
using Daycare.Application.Features.Billing.Invoices.DTOs;
using Daycare.Application.Features.Billing.Invoices.GenerateInvoices;
using Daycare.Application.Features.Billing.Invoices.GetInvoiceById;
using Daycare.Application.Features.Billing.Invoices.GetInvoices;
using Daycare.Application.Features.Billing.Invoices.VoidInvoice;
using Daycare.Application.Features.Billing.Payments.ProcessOnlinePayment;
using Daycare.Application.Features.Billing.Payments.RecordPayment;
using Daycare.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Daycare.Api.Controllers;

public class InvoicesController : BaseApiController
{
    private readonly GetInvoicesUseCase _getInvoicesUseCase;
    private readonly GetInvoiceByIdUseCase _getInvoiceByIdUseCase;
    private readonly GenerateInvoicesUseCase _generateInvoicesUseCase;
    private readonly AddInvoiceAdjustmentUseCase _addInvoiceAdjustmentUseCase;
    private readonly VoidInvoiceUseCase _voidInvoiceUseCase;
    private readonly RecordPaymentUseCase _recordPaymentUseCase;
    private readonly ProcessOnlinePaymentUseCase _processOnlinePaymentUseCase;

    public InvoicesController(
        GetInvoicesUseCase getInvoicesUseCase,
        GetInvoiceByIdUseCase getInvoiceByIdUseCase,
        GenerateInvoicesUseCase generateInvoicesUseCase,
        AddInvoiceAdjustmentUseCase addInvoiceAdjustmentUseCase,
        VoidInvoiceUseCase voidInvoiceUseCase,
        RecordPaymentUseCase recordPaymentUseCase,
        ProcessOnlinePaymentUseCase processOnlinePaymentUseCase)
    {
        _getInvoicesUseCase = getInvoicesUseCase;
        _getInvoiceByIdUseCase = getInvoiceByIdUseCase;
        _generateInvoicesUseCase = generateInvoicesUseCase;
        _addInvoiceAdjustmentUseCase = addInvoiceAdjustmentUseCase;
        _voidInvoiceUseCase = voidInvoiceUseCase;
        _recordPaymentUseCase = recordPaymentUseCase;
        _processOnlinePaymentUseCase = processOnlinePaymentUseCase;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Parent")]
    public async Task<ActionResult<PagedResult<InvoiceListItemDto>>> GetInvoices(
        [FromQuery] InvoiceStatus? status,
        [FromQuery] Guid? childId,
        [FromQuery] Guid? guardianId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] bool? overdueOnly,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var response = await _getInvoicesUseCase.ExecuteAsync(
            new GetInvoicesRequest(
                status,
                childId,
                guardianId,
                from,
                to,
                overdueOnly,
                search,
                page,
                pageSize), ct);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,Parent")]
    public async Task<ActionResult<InvoiceDetailDto>> GetInvoiceById(
        Guid id,
        CancellationToken ct)
    {
        var response = await _getInvoiceByIdUseCase.ExecuteAsync(
            new GetInvoiceByIdRequest(id), ct);
        return Ok(response);
    }

    public record GenerateInvoicesBody(int Year, int Month);

    [HttpPost("generate")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<GenerateInvoicesResultDto>> GenerateInvoices(
        [FromBody] GenerateInvoicesBody body,
        CancellationToken ct)
    {
        var response = await _generateInvoicesUseCase.ExecuteAsync(
            new GenerateInvoicesRequest(body.Year, body.Month), ct);
        return Ok(response);
    }

    public record AddAdjustmentBody(string Description, decimal Amount);

    [HttpPost("{id:guid}/adjustments")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<InvoiceDetailDto>> AddAdjustment(
        Guid id,
        [FromBody] AddAdjustmentBody body,
        CancellationToken ct)
    {
        var response = await _addInvoiceAdjustmentUseCase.ExecuteAsync(
            new AddInvoiceAdjustmentRequest(id, body.Description, body.Amount), ct);
        return Ok(response);
    }

    public record VoidInvoiceBody(string Reason);

    [HttpPost("{id:guid}/void")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<InvoiceDetailDto>> VoidInvoice(
        Guid id,
        [FromBody] VoidInvoiceBody body,
        CancellationToken ct)
    {
        var response = await _voidInvoiceUseCase.ExecuteAsync(
            new VoidInvoiceRequest(id, body.Reason), ct);
        return Ok(response);
    }

    public record RecordPaymentBody(
        decimal Amount,
        PaymentMethod Method,
        string? Reference = null,
        DateTime? PaidAt = null,
        string? Notes = null
    );

    [HttpPost("{id:guid}/payments")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<InvoiceDetailDto>> RecordPayment(
        Guid id,
        [FromBody] RecordPaymentBody body,
        CancellationToken ct)
    {
        var response = await _recordPaymentUseCase.ExecuteAsync(
            new RecordPaymentRequest(
                id,
                body.Amount,
                body.Method,
                body.Reference,
                body.PaidAt,
                body.Notes), ct);
        return Ok(response);
    }

    [HttpPost("{id:guid}/online-payments")]
    [Authorize(Roles = "Parent")]
    public async Task<ActionResult<InvoiceDetailDto>> ProcessOnlinePayment(
        Guid id,
        CancellationToken ct)
    {
        var response = await _processOnlinePaymentUseCase.ExecuteAsync(
            new ProcessOnlinePaymentRequest(id), ct);
        return Ok(response);
    }
}


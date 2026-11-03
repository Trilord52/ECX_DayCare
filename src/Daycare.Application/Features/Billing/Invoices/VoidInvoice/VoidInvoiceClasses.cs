using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Billing.Invoices.DTOs;
using Daycare.Domain.Enums;
using Daycare.Domain.Exceptions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Billing.Invoices.VoidInvoice;

public record VoidInvoiceRequest(
    Guid InvoiceId,
    string Reason
);

public class VoidInvoiceValidator : AbstractValidator<VoidInvoiceRequest>
{
    public VoidInvoiceValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty().WithMessage("Invoice ID is required.");
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300).WithMessage("Void reason is required and must not exceed 300 characters.");
    }
}

public class VoidInvoiceUseCase : UseCase<VoidInvoiceRequest, InvoiceDetailDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public VoidInvoiceUseCase(
        IApplicationDbContext dbContext,
        IDateTimeProvider dateTimeProvider,
        IValidator<VoidInvoiceRequest> validator)
        : base(validator)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    protected override async Task<InvoiceDetailDto> HandleAsync(
        VoidInvoiceRequest request,
        CancellationToken ct)
    {
        var invoice = await _dbContext.Invoices
            .Include(i => i.Child)
            .Include(i => i.Guardian)
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, ct);

        if (invoice == null)
        {
            throw new NotFoundException("Invoice", request.InvoiceId);
        }

        // BR-23: Cannot void if already void
        if (invoice.Status == InvoiceStatus.Void)
        {
            throw new DomainRuleException("INVOICE_VOID", "Invoice is already void.");
        }

        // BR-23: Allowed only when it has no active payments
        var hasActivePayments = await _dbContext.Payments
            .AnyAsync(p => p.InvoiceId == request.InvoiceId && p.VoidedAt == null, ct);

        if (hasActivePayments || invoice.AmountPaid > 0)
        {
            throw new DomainRuleException("INVOICE_HAS_PAYMENTS", "Cannot void an invoice with active payments.");
        }

        invoice.Void(request.Reason, _dateTimeProvider.UtcNow);
        await _dbContext.SaveChangesAsync(ct);

        var lineDtos = invoice.Lines
            .OrderBy(l => l.SortOrder)
            .Select(l => new InvoiceLineDto(
                l.Id,
                l.LineType,
                l.Description,
                l.Quantity,
                l.UnitPrice,
                l.LineTotal,
                l.SortOrder))
            .ToList();

        var paymentDtos = invoice.Payments
            .OrderBy(p => p.PaidAt)
            .Select(p => new PaymentDto(
                p.Id,
                p.InvoiceId,
                p.Amount,
                p.Method,
                p.Reference,
                p.PaidAt,
                p.Notes,
                p.RecordedByUserId,
                p.RecordedByName,
                p.VoidedAt,
                p.VoidReason,
                p.VoidedByName))
            .ToList();

        var today = _dateTimeProvider.LocalToday;
        var isOverdue = invoice.DueDate < today &&
                        (invoice.Status == InvoiceStatus.Issued || invoice.Status == InvoiceStatus.PartiallyPaid);

        return new InvoiceDetailDto(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.ChildId,
            invoice.Child.FirstName + " " + invoice.Child.LastName,
            invoice.GuardianId,
            invoice.Guardian.FirstName + " " + invoice.Guardian.LastName,
            invoice.PeriodStart,
            invoice.PeriodEnd,
            invoice.IssueDate,
            invoice.DueDate,
            invoice.Status,
            invoice.Currency,
            invoice.TotalAmount,
            invoice.AmountPaid,
            invoice.Balance,
            isOverdue,
            invoice.VoidReason,
            invoice.VoidedAt,
            lineDtos,
            paymentDtos);
    }
}


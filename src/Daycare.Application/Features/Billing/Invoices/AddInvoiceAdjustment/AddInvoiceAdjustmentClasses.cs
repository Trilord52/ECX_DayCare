using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Billing.Invoices.DTOs;
using Daycare.Domain.Enums;
using Daycare.Domain.Exceptions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Billing.Invoices.AddInvoiceAdjustment;

public record AddInvoiceAdjustmentRequest(
    Guid InvoiceId,
    string Description,
    decimal Amount
);

public class AddInvoiceAdjustmentValidator : AbstractValidator<AddInvoiceAdjustmentRequest>
{
    public AddInvoiceAdjustmentValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty().WithMessage("Invoice ID is required.");
        RuleFor(x => x.Description).NotEmpty().MaximumLength(300).WithMessage("Description is required and must not exceed 300 characters.");
        RuleFor(x => x.Amount).NotEqual(0).WithMessage("Adjustment amount cannot be zero.");
    }
}

public class AddInvoiceAdjustmentUseCase : UseCase<AddInvoiceAdjustmentRequest, InvoiceDetailDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AddInvoiceAdjustmentUseCase(
        IApplicationDbContext dbContext,
        IDateTimeProvider dateTimeProvider,
        IValidator<AddInvoiceAdjustmentRequest> validator)
        : base(validator)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    protected override async Task<InvoiceDetailDto> HandleAsync(
        AddInvoiceAdjustmentRequest request,
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

        if (invoice.Status == InvoiceStatus.Void)
        {
            throw new DomainRuleException("INVOICE_VOID", "Cannot add adjustment to a void invoice.");
        }

        var roundedAmount = decimal.Round(request.Amount, 2, MidpointRounding.AwayFromZero);
        var expectedTotal = invoice.TotalAmount + roundedAmount;

        // BR-20: If an adjustment would make total_amount lower than amount_paid, reject with OVERPAYMENT
        if (expectedTotal < invoice.AmountPaid)
        {
            throw new DomainRuleException("OVERPAYMENT", "Adjustment would reduce total amount below amount already paid.");
        }

        if (expectedTotal < 0)
        {
            throw new DomainRuleException("VALIDATION_FAILED", "Adjustment would result in negative total amount.");
        }

        var line = invoice.AddLine(InvoiceLineType.Adjustment, request.Description, 1, roundedAmount);
        _dbContext.InvoiceLines.Add(line);
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


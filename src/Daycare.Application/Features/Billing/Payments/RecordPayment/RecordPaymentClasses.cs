using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Billing.Invoices.DTOs;
using Daycare.Domain.Entities;
using Daycare.Domain.Enums;
using Daycare.Domain.Exceptions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Billing.Payments.RecordPayment;

public record RecordPaymentRequest(
    Guid InvoiceId,
    decimal Amount,
    PaymentMethod Method,
    string? Reference = null,
    DateTime? PaidAt = null,
    string? Notes = null
);

public class RecordPaymentValidator : AbstractValidator<RecordPaymentRequest>
{
    public RecordPaymentValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty().WithMessage("Invoice ID is required.");
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Payment amount must be greater than zero.");
        RuleFor(x => x.Reference).MaximumLength(100).WithMessage("Reference must not exceed 100 characters.");
        RuleFor(x => x.Notes).MaximumLength(500).WithMessage("Notes must not exceed 500 characters.");
    }
}

public class RecordPaymentUseCase : UseCase<RecordPaymentRequest, InvoiceDetailDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RecordPaymentUseCase(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        IValidator<RecordPaymentRequest> validator)
        : base(validator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    protected override async Task<InvoiceDetailDto> HandleAsync(
        RecordPaymentRequest request,
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

        // BR-21: The invoice must not be void
        if (invoice.Status == InvoiceStatus.Void)
        {
            throw new DomainRuleException("INVOICE_VOID", "Cannot record payment on a void invoice.");
        }

        var roundedAmount = decimal.Round(request.Amount, 2, MidpointRounding.AwayFromZero);

        // BR-21: Amount must not exceed the outstanding balance
        if (roundedAmount > invoice.Balance)
        {
            throw new DomainRuleException("OVERPAYMENT", $"Payment of {roundedAmount} exceeds outstanding balance of {invoice.Balance}.");
        }

        // BR-21: A non-empty reference must be unique among active payments
        if (!string.IsNullOrWhiteSpace(request.Reference))
        {
            var trimmedReference = request.Reference.Trim();
            var duplicate = await _dbContext.Payments
                .AnyAsync(p => p.Method == request.Method &&
                               p.Reference == trimmedReference &&
                               p.VoidedAt == null, ct);

            if (duplicate)
            {
                throw new DomainRuleException("DUPLICATE_REFERENCE", "A payment with this method and reference already exists.");
            }
        }

        var userId = _currentUser.UserId ?? throw new UnauthorizedException("Authenticated user ID is required.");
        var userName = _currentUser.FullName ?? _currentUser.Email ?? "Admin";

        var paidAtUtc = request.PaidAt.HasValue
            ? (request.PaidAt.Value.Kind == DateTimeKind.Utc
                ? request.PaidAt.Value
                : DateTime.SpecifyKind(request.PaidAt.Value, DateTimeKind.Utc))
            : _dateTimeProvider.UtcNow;

        invoice.ApplyPayment(roundedAmount);

        var payment = new Payment(
            invoice.Id,
            roundedAmount,
            request.Method,
            paidAtUtc,
            userId,
            userName,
            request.Reference,
            request.Notes);

        _dbContext.Payments.Add(payment);
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


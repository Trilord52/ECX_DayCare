using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Billing.Invoices.DTOs;
using Daycare.Domain.Entities;
using Daycare.Domain.Enums;
using Daycare.Domain.Exceptions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Billing.Payments.ProcessOnlinePayment;

public record ProcessOnlinePaymentRequest(Guid InvoiceId);

public class ProcessOnlinePaymentValidator : AbstractValidator<ProcessOnlinePaymentRequest>
{
    public ProcessOnlinePaymentValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty().WithMessage("Invoice ID is required.");
    }
}

public class ProcessOnlinePaymentUseCase : UseCase<ProcessOnlinePaymentRequest, InvoiceDetailDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IPaymentGateway _paymentGateway;

    public ProcessOnlinePaymentUseCase(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        IPaymentGateway paymentGateway,
        IValidator<ProcessOnlinePaymentRequest> validator)
        : base(validator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _paymentGateway = paymentGateway;
    }

    protected override async Task<InvoiceDetailDto> HandleAsync(
        ProcessOnlinePaymentRequest request,
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

        var guardianId = _currentUser.GuardianId;
        if (!guardianId.HasValue && _currentUser.UserId.HasValue)
        {
            guardianId = await _dbContext.Guardians
                .AsNoTracking()
                .Where(g => g.UserId == _currentUser.UserId.Value)
                .Select(g => (Guid?)g.Id)
                .FirstOrDefaultAsync(ct);
        }

        if (!guardianId.HasValue || invoice.GuardianId != guardianId.Value)
        {
            throw new ForbiddenException("Parents may only pay their own invoices.");
        }

        if (invoice.Balance <= 0)
        {
            throw new DomainRuleException("OVERPAYMENT", "Invoice has no outstanding balance.");
        }

        var amountToPay = invoice.Balance;
        var gatewayResult = await _paymentGateway.ProcessPaymentAsync(
            amountToPay, invoice.Currency, invoice.InvoiceNumber, ct);

        if (!gatewayResult.Success)
        {
            throw new DomainRuleException("VALIDATION_FAILED", gatewayResult.ErrorMessage ?? "Payment gateway error.");
        }

        var userId = _currentUser.UserId ?? throw new UnauthorizedException("Authenticated user ID is required.");
        var userName = _currentUser.FullName ?? _currentUser.Email ?? "Parent";

        invoice.ApplyPayment(amountToPay);

        var payment = new Payment(
            invoice.Id,
            amountToPay,
            PaymentMethod.Online,
            _dateTimeProvider.UtcNow,
            userId,
            userName,
            gatewayResult.TransactionReference,
            "Online payment via payment gateway");

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


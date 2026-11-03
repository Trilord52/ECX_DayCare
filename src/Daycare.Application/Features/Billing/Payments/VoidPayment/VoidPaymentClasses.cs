using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Billing.Invoices.DTOs;
using Daycare.Domain.Exceptions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Billing.Payments.VoidPayment;

public record VoidPaymentRequest(
    Guid PaymentId,
    string Reason
);

public class VoidPaymentValidator : AbstractValidator<VoidPaymentRequest>
{
    public VoidPaymentValidator()
    {
        RuleFor(x => x.PaymentId).NotEmpty().WithMessage("Payment ID is required.");
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300).WithMessage("Void reason is required and must not exceed 300 characters.");
    }
}

public class VoidPaymentUseCase : UseCase<VoidPaymentRequest, PaymentDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public VoidPaymentUseCase(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        IValidator<VoidPaymentRequest> validator)
        : base(validator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    protected override async Task<PaymentDto> HandleAsync(
        VoidPaymentRequest request,
        CancellationToken ct)
    {
        var payment = await _dbContext.Payments
            .Include(p => p.Invoice)
            .FirstOrDefaultAsync(p => p.Id == request.PaymentId, ct);

        if (payment == null)
        {
            throw new NotFoundException("Payment", request.PaymentId);
        }

        // BR-22: Voiding twice fails with PAYMENT_ALREADY_VOIDED
        if (payment.VoidedAt.HasValue)
        {
            throw new DomainRuleException("PAYMENT_ALREADY_VOIDED", "Payment has already been voided.");
        }

        var userId = _currentUser.UserId ?? throw new UnauthorizedException("Authenticated user ID is required.");
        var userName = _currentUser.FullName ?? _currentUser.Email ?? "Admin";

        payment.Void(request.Reason, userId, userName, _dateTimeProvider.UtcNow);
        payment.Invoice.VoidPayment(payment.Amount);

        await _dbContext.SaveChangesAsync(ct);

        return new PaymentDto(
            payment.Id,
            payment.InvoiceId,
            payment.Amount,
            payment.Method,
            payment.Reference,
            payment.PaidAt,
            payment.Notes,
            payment.RecordedByUserId,
            payment.RecordedByName,
            payment.VoidedAt,
            payment.VoidReason,
            payment.VoidedByName);
    }
}


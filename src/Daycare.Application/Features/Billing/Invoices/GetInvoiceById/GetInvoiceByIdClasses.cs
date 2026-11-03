using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Billing.Invoices.DTOs;
using Daycare.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Billing.Invoices.GetInvoiceById;

public record GetInvoiceByIdRequest(Guid Id);

public class GetInvoiceByIdValidator : AbstractValidator<GetInvoiceByIdRequest>
{
    public GetInvoiceByIdValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Invoice ID is required.");
    }
}

public class GetInvoiceByIdUseCase : UseCase<GetInvoiceByIdRequest, InvoiceDetailDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetInvoiceByIdUseCase(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        IValidator<GetInvoiceByIdRequest> validator)
        : base(validator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    protected override async Task<InvoiceDetailDto> HandleAsync(
        GetInvoiceByIdRequest request,
        CancellationToken ct)
    {
        // BR-24: Staff members cannot view billing data
        if (_currentUser.Role == "Staff")
        {
            throw new ForbiddenException("Staff members cannot view billing data.");
        }

        var invoice = await _dbContext.Invoices
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(i => i.Child)
            .Include(i => i.Guardian)
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == request.Id, ct);

        if (invoice == null)
        {
            throw new NotFoundException("Invoice", request.Id);
        }

        // BR-24: Parents see only invoices billed to their own guardian record
        if (_currentUser.Role == "Parent")
        {
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
                throw new ForbiddenException("Parents may only view their own invoices.");
            }
        }

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


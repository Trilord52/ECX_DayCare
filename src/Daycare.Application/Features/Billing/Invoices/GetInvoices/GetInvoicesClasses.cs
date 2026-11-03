using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Common.Models;
using Daycare.Application.Features.Billing.Invoices.DTOs;
using Daycare.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Billing.Invoices.GetInvoices;

public record GetInvoicesRequest(
    InvoiceStatus? Status = null,
    Guid? ChildId = null,
    Guid? GuardianId = null,
    DateOnly? From = null,
    DateOnly? To = null,
    bool? OverdueOnly = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 20
);

public class GetInvoicesValidator : AbstractValidator<GetInvoicesRequest>
{
    public GetInvoicesValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("Page must be at least 1.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("PageSize must be between 1 and 100.");
        RuleFor(x => x).Must(x => !x.From.HasValue || !x.To.HasValue || x.From <= x.To)
            .WithMessage("From date must be on or before To date.");
    }
}

public class GetInvoicesUseCase : UseCase<GetInvoicesRequest, PagedResult<InvoiceListItemDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetInvoicesUseCase(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        IValidator<GetInvoicesRequest> validator)
        : base(validator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    protected override async Task<PagedResult<InvoiceListItemDto>> HandleAsync(
        GetInvoicesRequest request,
        CancellationToken ct)
    {
        // BR-24: Staff members cannot view billing data
        if (_currentUser.Role == "Staff")
        {
            throw new ForbiddenException("Staff members cannot view billing data.");
        }

        var query = _dbContext.Invoices
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(i => i.Child)
            .Include(i => i.Guardian)
            .AsQueryable();

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

            if (!guardianId.HasValue)
            {
                return new PagedResult<InvoiceListItemDto>([], 0, request.Page, request.PageSize);
            }

            query = query.Where(i => i.GuardianId == guardianId.Value);
        }
        else if (_currentUser.Role == "Admin")
        {
            if (request.GuardianId.HasValue)
            {
                query = query.Where(i => i.GuardianId == request.GuardianId.Value);
            }
        }

        if (request.Status.HasValue)
        {
            query = query.Where(i => i.Status == request.Status.Value);
        }

        if (request.ChildId.HasValue)
        {
            query = query.Where(i => i.ChildId == request.ChildId.Value);
        }

        if (request.From.HasValue)
        {
            query = query.Where(i => i.IssueDate >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(i => i.IssueDate <= request.To.Value);
        }

        var today = _dateTimeProvider.LocalToday;
        if (request.OverdueOnly == true)
        {
            query = query.Where(i => i.DueDate < today &&
                                    (i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.PartiallyPaid));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(i => i.InvoiceNumber.ToLower().Contains(search) ||
                                     (i.Child.FirstName + " " + i.Child.LastName).ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(i => i.IssueDate)
            .ThenByDescending(i => i.InvoiceNumber)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(i => new InvoiceListItemDto(
                i.Id,
                i.InvoiceNumber,
                i.ChildId,
                i.Child.FirstName + " " + i.Child.LastName,
                i.GuardianId,
                i.Guardian.FirstName + " " + i.Guardian.LastName,
                i.PeriodStart,
                i.PeriodEnd,
                i.IssueDate,
                i.DueDate,
                i.Status,
                i.Currency,
                i.TotalAmount,
                i.AmountPaid,
                i.Balance,
                i.DueDate < today && (i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.PartiallyPaid)))
            .ToListAsync(ct);

        return new PagedResult<InvoiceListItemDto>(items, totalCount, request.Page, request.PageSize);
    }
}


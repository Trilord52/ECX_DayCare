using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Reports.DTOs;
using Daycare.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Reports.BillingReport;

public record GetBillingReportRequest(
    DateOnly? From = null,
    DateOnly? To = null
);

public class GetBillingReportValidator : AbstractValidator<GetBillingReportRequest>
{
    public GetBillingReportValidator()
    {
        RuleFor(x => x)
            .Must(x => !x.From.HasValue || !x.To.HasValue || x.From.Value <= x.To.Value)
            .WithMessage("'From' date must be less than or equal to 'To' date.");
    }
}

public class GetBillingReportUseCase : UseCase<GetBillingReportRequest, BillingReportDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetBillingReportUseCase(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        IValidator<GetBillingReportRequest> validator)
        : base(validator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    protected override async Task<BillingReportDto> HandleAsync(
        GetBillingReportRequest request,
        CancellationToken ct)
    {
        if (_currentUser.Role != "Admin")
        {
            throw new ForbiddenException("Only admins can access reports.");
        }

        var today = _dateTimeProvider.LocalToday;
        var to = request.To ?? today;
        var from = request.From ?? new DateOnly(today.Year, today.Month, 1).AddMonths(-2);

        var invoices = await _dbContext.Invoices
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(i => i.Child)
            .Include(i => i.Guardian)
            .Where(i => i.IssueDate >= from && i.IssueDate <= to && i.Status != InvoiceStatus.Void)
            .OrderByDescending(i => i.IssueDate)
            .ThenBy(i => i.InvoiceNumber)
            .ToListAsync(ct);

        var items = invoices.Select(i =>
        {
            var isOverdue = i.DueDate < today &&
                            (i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.PartiallyPaid);

            return new BillingReportItemDto(
                i.Id,
                i.InvoiceNumber,
                i.ChildId,
                i.Child.FirstName + " " + i.Child.LastName,
                i.GuardianId,
                i.Guardian.FirstName + " " + i.Guardian.LastName,
                i.IssueDate,
                i.DueDate,
                i.Status,
                i.TotalAmount,
                i.AmountPaid,
                i.Balance,
                isOverdue);
        }).ToList();

        var totalInvoices = items.Count;
        var totalBilled = items.Sum(i => i.TotalAmount);
        var totalCollected = items.Sum(i => i.AmountPaid);
        var totalOutstanding = items.Sum(i => i.Balance);
        var overdueCount = items.Count(i => i.IsOverdue);

        return new BillingReportDto(
            from,
            to,
            totalInvoices,
            totalBilled,
            totalCollected,
            totalOutstanding,
            overdueCount,
            "ETB",
            items);
    }
}

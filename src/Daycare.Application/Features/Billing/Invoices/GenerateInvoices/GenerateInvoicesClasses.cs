using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Features.Billing.Invoices.DTOs;
using Daycare.Domain.Entities;
using Daycare.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Daycare.Application.Features.Billing.Invoices.GenerateInvoices;

public record GenerateInvoicesRequest(
    int Year,
    int Month
);

public class GenerateInvoicesValidator : AbstractValidator<GenerateInvoicesRequest>
{
    public GenerateInvoicesValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100).WithMessage("Year must be between 2000 and 2100.");
        RuleFor(x => x.Month).InclusiveBetween(1, 12).WithMessage("Month must be between 1 and 12.");
    }
}

public class GenerateInvoicesUseCase : UseCase<GenerateInvoicesRequest, GenerateInvoicesResultDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly DaycareOptions _daycareOptions;

    public GenerateInvoicesUseCase(
        IApplicationDbContext dbContext,
        IDateTimeProvider dateTimeProvider,
        IOptions<DaycareOptions> daycareOptions,
        IValidator<GenerateInvoicesRequest> validator)
        : base(validator)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
        _daycareOptions = daycareOptions.Value;
    }

    protected override async Task<GenerateInvoicesResultDto> HandleAsync(
        GenerateInvoicesRequest request,
        CancellationToken ct)
    {
        var periodStart = new DateOnly(request.Year, request.Month, 1);
        var periodEnd = periodStart.AddMonths(1).AddDays(-1);

        var activeChildren = await _dbContext.Children
            .Include(c => c.FeePlan)
            .Include(c => c.Guardians)
            .Where(c => c.Status == ChildStatus.Active)
            .OrderBy(c => c.LastName)
            .ThenBy(c => c.FirstName)
            .ToListAsync(ct);

        var createdInvoiceIds = new List<Guid>();
        var skipped = new List<SkippedChildDto>();

        var issueDate = _dateTimeProvider.LocalToday;
        var dueDate = issueDate.AddDays(_daycareOptions.InvoiceDueDays);

        foreach (var child in activeChildren)
        {
            var childFullName = $"{child.FirstName} {child.LastName}";

            // BR-18: Check fee plan
            if (child.FeePlan == null || !child.FeePlanId.HasValue)
            {
                skipped.Add(new SkippedChildDto(child.Id, childFullName, "NO_FEE_PLAN"));
                continue;
            }

            // BR-18: Check existing non-void invoice for this period
            var existingInvoice = await _dbContext.Invoices
                .AnyAsync(i => i.ChildId == child.Id &&
                               i.PeriodStart == periodStart &&
                               i.Status != InvoiceStatus.Void, ct);

            if (existingInvoice)
            {
                skipped.Add(new SkippedChildDto(child.Id, childFullName, "INVOICE_ALREADY_EXISTS"));
                continue;
            }

            // BR-18: Payer guardian: first guardian with is_payer = true (primary contact first, then oldest link)
            var payerLink = child.Guardians
                .Where(cg => cg.IsPayer)
                .OrderByDescending(cg => cg.IsPrimaryContact)
                .ThenBy(cg => cg.CreatedAt)
                .FirstOrDefault();

            if (payerLink == null)
            {
                skipped.Add(new SkippedChildDto(child.Id, childFullName, "NO_PAYER_GUARDIAN"));
                continue;
            }

            // Generate invoice number from sequence
            var seq = await _dbContext.GetNextInvoiceSequenceAsync(ct);
            var invoiceNumber = $"INV-{request.Year:D4}{request.Month:D2}-{seq:D5}";

            var invoice = new Invoice(
                invoiceNumber,
                child.Id,
                payerLink.GuardianId,
                periodStart,
                periodEnd,
                issueDate,
                dueDate,
                _daycareOptions.Currency);

            // BR-19: Tuition line
            invoice.AddLine(
                InvoiceLineType.Tuition,
                $"{child.FeePlan.Name} - {periodStart:MMMM yyyy}",
                1,
                child.FeePlan.MonthlyAmount,
                0);

            // BR-19: Late fee line
            var attendanceRecords = await _dbContext.AttendanceRecords
                .Where(a => a.ChildId == child.Id &&
                            a.Date >= periodStart &&
                            a.Date <= periodEnd &&
                            a.LateMinutes > 0)
                .ToListAsync(ct);

            var totalBlocks = attendanceRecords.Sum(r =>
                (int)Math.Ceiling((double)r.LateMinutes / _daycareOptions.LateFeeBlockMinutes));

            if (totalBlocks > 0)
            {
                invoice.AddLine(
                    InvoiceLineType.LateFee,
                    $"Late pickup fees ({totalBlocks} x {_daycareOptions.LateFeeBlockMinutes} min blocks)",
                    totalBlocks,
                    _daycareOptions.LateFeePerBlock,
                    1);
            }

            _dbContext.Invoices.Add(invoice);
            createdInvoiceIds.Add(invoice.Id);
        }

        await _dbContext.SaveChangesAsync(ct);

        return new GenerateInvoicesResultDto(createdInvoiceIds.Count, createdInvoiceIds, skipped);
    }
}


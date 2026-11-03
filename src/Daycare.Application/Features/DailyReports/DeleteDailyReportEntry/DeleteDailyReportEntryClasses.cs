using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Domain.Exceptions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.DailyReports.DeleteDailyReportEntry;

public record DeleteDailyReportEntryRequest(Guid EntryId);

public class DeleteDailyReportEntryValidator : AbstractValidator<DeleteDailyReportEntryRequest>
{
    public DeleteDailyReportEntryValidator()
    {
        RuleFor(x => x.EntryId).NotEmpty().WithMessage("Entry ID is required.");
    }
}

public class DeleteDailyReportEntryUseCase : UseCase<DeleteDailyReportEntryRequest, bool>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAccessGuard _accessGuard;

    public DeleteDailyReportEntryUseCase(
        IApplicationDbContext dbContext,
        IDateTimeProvider dateTimeProvider,
        IAccessGuard accessGuard,
        IValidator<DeleteDailyReportEntryRequest> validator)
        : base(validator)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
        _accessGuard = accessGuard;
    }

    protected override async Task<bool> HandleAsync(
        DeleteDailyReportEntryRequest request,
        CancellationToken ct)
    {
        var entry = await _dbContext.DailyReportEntries
            .Include(e => e.DailyReport)
            .FirstOrDefaultAsync(e => e.Id == request.EntryId, ct);

        if (entry == null)
        {
            throw new NotFoundException("DailyReportEntry", request.EntryId);
        }

        await _accessGuard.EnsureCanWriteChildDataAsync(entry.DailyReport.ChildId, ct);

        // BR-14: Deleting allowed only on same local day
        if (entry.DailyReport.Date != _dateTimeProvider.LocalToday)
        {
            throw new DomainRuleException("ENTRY_LOCKED", "Daily report entries can only be deleted on the same day.");
        }

        _dbContext.DailyReportEntries.Remove(entry);
        await _dbContext.SaveChangesAsync(ct);

        return true;
    }
}


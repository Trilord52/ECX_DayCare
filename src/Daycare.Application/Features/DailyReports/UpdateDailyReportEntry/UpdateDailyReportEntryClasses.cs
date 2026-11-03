using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.DailyReports.DTOs;
using Daycare.Domain.Enums;
using Daycare.Domain.Exceptions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.DailyReports.UpdateDailyReportEntry;

public record UpdateDailyReportEntryRequest(
    Guid EntryId,
    DateTime OccurredAt,
    DateTime? EndedAt = null,
    MealType? MealType = null,
    AmountEaten? AmountEaten = null,
    DiaperType? DiaperType = null,
    string? Description = null
);

public class UpdateDailyReportEntryValidator : AbstractValidator<UpdateDailyReportEntryRequest>
{
    public UpdateDailyReportEntryValidator()
    {
        RuleFor(x => x.EntryId).NotEmpty().WithMessage("Entry ID is required.");
        RuleFor(x => x.Description).MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.");
    }
}

public class UpdateDailyReportEntryUseCase : UseCase<UpdateDailyReportEntryRequest, DailyReportEntryDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAccessGuard _accessGuard;

    public UpdateDailyReportEntryUseCase(
        IApplicationDbContext dbContext,
        IDateTimeProvider dateTimeProvider,
        IAccessGuard accessGuard,
        IValidator<UpdateDailyReportEntryRequest> validator)
        : base(validator)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
        _accessGuard = accessGuard;
    }

    protected override async Task<DailyReportEntryDto> HandleAsync(
        UpdateDailyReportEntryRequest request,
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

        // BR-14: Editing allowed only on same local day
        if (entry.DailyReport.Date != _dateTimeProvider.LocalToday)
        {
            throw new DomainRuleException("ENTRY_LOCKED", "Daily report entries can only be edited on the same day.");
        }

        // BR-15: Validate based on entry type
        if (entry.EntryType == EntryType.Meal && !request.MealType.HasValue)
        {
            throw new DomainRuleException("VALIDATION_FAILED", "Meal type is required for meals.");
        }

        if (entry.EntryType == EntryType.Diaper && !request.DiaperType.HasValue)
        {
            throw new DomainRuleException("VALIDATION_FAILED", "Diaper type is required for diaper entries.");
        }

        if ((entry.EntryType == EntryType.Activity ||
             entry.EntryType == EntryType.Medication ||
             entry.EntryType == EntryType.Note) &&
            string.IsNullOrWhiteSpace(request.Description))
        {
            throw new DomainRuleException("VALIDATION_FAILED", "Description is required for activity, medication, and note entries.");
        }

        if (entry.EntryType == EntryType.Nap && request.EndedAt.HasValue && request.EndedAt.Value <= request.OccurredAt)
        {
            throw new DomainRuleException("VALIDATION_FAILED", "Nap end time must be after start time.");
        }

        var occurredAtUtc = request.OccurredAt.Kind == DateTimeKind.Utc
            ? request.OccurredAt
            : DateTime.SpecifyKind(request.OccurredAt, DateTimeKind.Utc);

        var endedAtUtc = request.EndedAt.HasValue
            ? (request.EndedAt.Value.Kind == DateTimeKind.Utc
                ? request.EndedAt.Value
                : DateTime.SpecifyKind(request.EndedAt.Value, DateTimeKind.Utc))
            : (DateTime?)null;

        entry.Update(
            occurredAtUtc,
            endedAtUtc,
            request.MealType,
            request.AmountEaten,
            request.DiaperType,
            request.Description);

        await _dbContext.SaveChangesAsync(ct);

        return new DailyReportEntryDto(
            entry.Id,
            entry.DailyReportId,
            entry.EntryType,
            entry.OccurredAt,
            entry.EndedAt,
            entry.MealType,
            entry.AmountEaten,
            entry.DiaperType,
            entry.Description,
            entry.CreatedByUserId,
            entry.CreatedByName);
    }
}


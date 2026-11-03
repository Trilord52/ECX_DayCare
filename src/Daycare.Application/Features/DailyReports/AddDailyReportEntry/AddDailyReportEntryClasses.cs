using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.DailyReports.DTOs;
using Daycare.Domain.Entities;
using Daycare.Domain.Enums;
using Daycare.Domain.Exceptions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.DailyReports.AddDailyReportEntry;

public record AddDailyReportEntryRequest(
    Guid ChildId,
    DateOnly Date,
    EntryType EntryType,
    DateTime OccurredAt,
    DateTime? EndedAt = null,
    MealType? MealType = null,
    AmountEaten? AmountEaten = null,
    DiaperType? DiaperType = null,
    string? Description = null
);

public class AddDailyReportEntryValidator : AbstractValidator<AddDailyReportEntryRequest>
{
    public AddDailyReportEntryValidator()
    {
        RuleFor(x => x.ChildId).NotEmpty().WithMessage("Child ID is required.");

        // BR-15: Entry validation by type
        When(x => x.EntryType == EntryType.Meal, () =>
        {
            RuleFor(x => x.MealType).NotNull().WithMessage("Meal type is required for meals.");
        });

        When(x => x.EntryType == EntryType.Diaper, () =>
        {
            RuleFor(x => x.DiaperType).NotNull().WithMessage("Diaper type is required for diaper entries.");
        });

        When(x => x.EntryType == EntryType.Activity ||
                  x.EntryType == EntryType.Medication ||
                  x.EntryType == EntryType.Note, () =>
        {
            RuleFor(x => x.Description).NotEmpty().WithMessage("Description is required for activity, medication, and note entries.");
        });

        When(x => x.EntryType == EntryType.Nap && x.EndedAt.HasValue, () =>
        {
            RuleFor(x => x).Must(x => x.EndedAt > x.OccurredAt)
                .WithMessage("Nap end time must be after start time.");
        });

        RuleFor(x => x.Description).MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.");
    }
}

public class AddDailyReportEntryUseCase : UseCase<AddDailyReportEntryRequest, DailyReportEntryDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAccessGuard _accessGuard;

    public AddDailyReportEntryUseCase(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        IAccessGuard accessGuard,
        IValidator<AddDailyReportEntryRequest> validator)
        : base(validator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _accessGuard = accessGuard;
    }

    protected override async Task<DailyReportEntryDto> HandleAsync(
        AddDailyReportEntryRequest request,
        CancellationToken ct)
    {
        await _accessGuard.EnsureCanWriteChildDataAsync(request.ChildId, ct);

        // BR-14: Written only for today
        if (request.Date != _dateTimeProvider.LocalToday)
        {
            throw new DomainRuleException("ENTRY_LOCKED", "Daily report entries may only be added for today.");
        }

        // BR-14: Child must have a Present record today
        var isPresentToday = await _dbContext.AttendanceRecords
            .AnyAsync(a => a.ChildId == request.ChildId && a.Date == request.Date && a.Status == AttendanceStatus.Present, ct);

        if (!isPresentToday)
        {
            throw new DomainRuleException("NOT_CHECKED_IN", "Child must have a Present attendance record for today.");
        }

        var report = await _dbContext.DailyReports
            .FirstOrDefaultAsync(r => r.ChildId == request.ChildId && r.Date == request.Date, ct);

        if (report == null)
        {
            var child = await _dbContext.Children
                .FirstOrDefaultAsync(c => c.Id == request.ChildId, ct);

            if (child == null)
            {
                throw new NotFoundException("Child", request.ChildId);
            }

            if (!child.ClassroomId.HasValue)
            {
                throw new DomainRuleException("CHILD_INACTIVE", "Child is not assigned to a classroom.");
            }

            report = new DailyReport(
                request.ChildId,
                child.ClassroomId.Value,
                request.Date);

            _dbContext.DailyReports.Add(report);
            await _dbContext.SaveChangesAsync(ct);
        }

        var userId = _currentUser.UserId ?? throw new UnauthorizedException("Authenticated user ID is required.");
        var userName = _currentUser.FullName ?? _currentUser.Email ?? "Staff";

        var occurredAtUtc = request.OccurredAt.Kind == DateTimeKind.Utc
            ? request.OccurredAt
            : DateTime.SpecifyKind(request.OccurredAt, DateTimeKind.Utc);

        var endedAtUtc = request.EndedAt.HasValue
            ? (request.EndedAt.Value.Kind == DateTimeKind.Utc
                ? request.EndedAt.Value
                : DateTime.SpecifyKind(request.EndedAt.Value, DateTimeKind.Utc))
            : (DateTime?)null;

        var entry = new DailyReportEntry(
            report.Id,
            request.EntryType,
            occurredAtUtc,
            userId,
            userName,
            endedAtUtc,
            request.MealType,
            request.AmountEaten,
            request.DiaperType,
            request.Description);

        _dbContext.DailyReportEntries.Add(entry);
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


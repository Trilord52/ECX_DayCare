using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.DailyReports.DTOs;
using Daycare.Domain.Entities;
using Daycare.Domain.Enums;
using Daycare.Domain.Exceptions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.DailyReports.UpsertDailyReportHeader;

public record UpsertDailyReportHeaderRequest(
    Guid ChildId,
    DateOnly Date,
    Mood? Mood = null,
    string? Notes = null
);

public class UpsertDailyReportHeaderValidator : AbstractValidator<UpsertDailyReportHeaderRequest>
{
    public UpsertDailyReportHeaderValidator()
    {
        RuleFor(x => x.ChildId).NotEmpty().WithMessage("Child ID is required.");
        RuleFor(x => x.Notes).MaximumLength(2000).WithMessage("Notes must not exceed 2000 characters.");
    }
}

public class UpsertDailyReportHeaderUseCase : UseCase<UpsertDailyReportHeaderRequest, DailyReportDetailDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAccessGuard _accessGuard;

    public UpsertDailyReportHeaderUseCase(
        IApplicationDbContext dbContext,
        IDateTimeProvider dateTimeProvider,
        IAccessGuard accessGuard,
        IValidator<UpsertDailyReportHeaderRequest> validator)
        : base(validator)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
        _accessGuard = accessGuard;
    }

    protected override async Task<DailyReportDetailDto> HandleAsync(
        UpsertDailyReportHeaderRequest request,
        CancellationToken ct)
    {
        await _accessGuard.EnsureCanWriteChildDataAsync(request.ChildId, ct);

        // BR-14: Written only for today
        if (request.Date != _dateTimeProvider.LocalToday)
        {
            throw new DomainRuleException("ENTRY_LOCKED", "Daily reports may only be written for today.");
        }

        // BR-14: Child must have a Present record today
        var isPresentToday = await _dbContext.AttendanceRecords
            .AnyAsync(a => a.ChildId == request.ChildId && a.Date == request.Date && a.Status == AttendanceStatus.Present, ct);

        if (!isPresentToday)
        {
            throw new DomainRuleException("NOT_CHECKED_IN", "Child must have a Present attendance record for today.");
        }

        var report = await _dbContext.DailyReports
            .Include(r => r.Entries)
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
                request.Date,
                request.Mood,
                request.Notes);

            _dbContext.DailyReports.Add(report);
        }
        else
        {
            report.Update(request.Mood, request.Notes);
        }

        await _dbContext.SaveChangesAsync(ct);

        var entryDtos = report.Entries
            .OrderBy(e => e.OccurredAt)
            .Select(e => new DailyReportEntryDto(
                e.Id,
                e.DailyReportId,
                e.EntryType,
                e.OccurredAt,
                e.EndedAt,
                e.MealType,
                e.AmountEaten,
                e.DiaperType,
                e.Description,
                e.CreatedByUserId,
                e.CreatedByName))
            .ToList();

        return new DailyReportDetailDto(
            report.Id,
            report.ChildId,
            report.ClassroomId,
            report.Date,
            report.Mood,
            report.Notes,
            entryDtos);
    }
}


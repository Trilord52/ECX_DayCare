using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.DailyReports.DTOs;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.DailyReports.GetDailyReportDetail;

public record GetDailyReportDetailRequest(
    Guid ChildId,
    DateOnly Date
);

public class GetDailyReportDetailValidator : AbstractValidator<GetDailyReportDetailRequest>
{
    public GetDailyReportDetailValidator()
    {
        RuleFor(x => x.ChildId).NotEmpty().WithMessage("Child ID is required.");
    }
}

public class GetDailyReportDetailUseCase : UseCase<GetDailyReportDetailRequest, DailyReportDetailDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAccessGuard _accessGuard;

    public GetDailyReportDetailUseCase(
        IApplicationDbContext dbContext,
        IAccessGuard accessGuard,
        IValidator<GetDailyReportDetailRequest> validator)
        : base(validator)
    {
        _dbContext = dbContext;
        _accessGuard = accessGuard;
    }

    protected override async Task<DailyReportDetailDto> HandleAsync(
        GetDailyReportDetailRequest request,
        CancellationToken ct)
    {
        await _accessGuard.EnsureCanViewChildAsync(request.ChildId, ct);

        var report = await _dbContext.DailyReports
            .AsNoTracking()
            .Include(r => r.Entries)
            .FirstOrDefaultAsync(r => r.ChildId == request.ChildId && r.Date == request.Date, ct);

        if (report == null)
        {
            var child = await _dbContext.Children
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == request.ChildId, ct);

            if (child == null)
            {
                throw new NotFoundException("Child", request.ChildId);
            }

            return new DailyReportDetailDto(
                null,
                request.ChildId,
                child.ClassroomId,
                request.Date,
                null,
                null,
                []);
        }

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


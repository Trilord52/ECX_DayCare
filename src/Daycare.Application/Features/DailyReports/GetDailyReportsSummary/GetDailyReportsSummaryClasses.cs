using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Features.DailyReports.DTOs;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.DailyReports.GetDailyReportsSummary;

public record GetDailyReportsSummaryRequest(
    Guid ChildId,
    DateOnly? From = null,
    DateOnly? To = null
);

public class GetDailyReportsSummaryValidator : AbstractValidator<GetDailyReportsSummaryRequest>
{
    public GetDailyReportsSummaryValidator()
    {
        RuleFor(x => x.ChildId).NotEmpty().WithMessage("Child ID is required.");
        RuleFor(x => x).Must(x => !x.From.HasValue || !x.To.HasValue || x.From <= x.To)
            .WithMessage("From date must be on or before To date.");
    }
}

public class GetDailyReportsSummaryUseCase : UseCase<GetDailyReportsSummaryRequest, List<DailyReportSummaryDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAccessGuard _accessGuard;

    public GetDailyReportsSummaryUseCase(
        IApplicationDbContext dbContext,
        IAccessGuard accessGuard,
        IValidator<GetDailyReportsSummaryRequest> validator)
        : base(validator)
    {
        _dbContext = dbContext;
        _accessGuard = accessGuard;
    }

    protected override async Task<List<DailyReportSummaryDto>> HandleAsync(
        GetDailyReportsSummaryRequest request,
        CancellationToken ct)
    {
        await _accessGuard.EnsureCanViewChildAsync(request.ChildId, ct);

        var query = _dbContext.DailyReports
            .AsNoTracking()
            .Where(r => r.ChildId == request.ChildId);

        if (request.From.HasValue)
        {
            query = query.Where(r => r.Date >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(r => r.Date <= request.To.Value);
        }

        return await query
            .OrderByDescending(r => r.Date)
            .Select(r => new DailyReportSummaryDto(
                r.Date,
                r.Mood,
                r.Entries.Count))
            .ToListAsync(ct);
    }
}


using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Features.Billing.FeePlans.DTOs;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Billing.FeePlans.GetFeePlans;

public record GetFeePlansRequest();

public class GetFeePlansValidator : AbstractValidator<GetFeePlansRequest>
{
}

public class GetFeePlansUseCase : UseCase<GetFeePlansRequest, List<FeePlanDto>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetFeePlansUseCase(
        IApplicationDbContext dbContext,
        IValidator<GetFeePlansRequest> validator)
        : base(validator)
    {
        _dbContext = dbContext;
    }

    protected override async Task<List<FeePlanDto>> HandleAsync(
        GetFeePlansRequest request,
        CancellationToken ct)
    {
        return await _dbContext.FeePlans
            .AsNoTracking()
            .OrderBy(fp => fp.Name)
            .Select(fp => new FeePlanDto(
                fp.Id,
                fp.Name,
                fp.Description,
                fp.MonthlyAmount,
                fp.IsActive))
            .ToListAsync(ct);
    }
}


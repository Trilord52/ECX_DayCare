using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Billing.FeePlans.DTOs;
using Daycare.Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Billing.FeePlans.CreateFeePlan;

public record CreateFeePlanRequest(
    string Name,
    string? Description,
    decimal MonthlyAmount
);

public class CreateFeePlanValidator : AbstractValidator<CreateFeePlanRequest>
{
    public CreateFeePlanValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100).WithMessage("Fee plan name is required and must not exceed 100 characters.");
        RuleFor(x => x.Description).MaximumLength(500).WithMessage("Description must not exceed 500 characters.");
        RuleFor(x => x.MonthlyAmount).GreaterThanOrEqualTo(0).WithMessage("Monthly amount cannot be negative.");
    }
}

public class CreateFeePlanUseCase : UseCase<CreateFeePlanRequest, FeePlanDto>
{
    private readonly IApplicationDbContext _dbContext;

    public CreateFeePlanUseCase(
        IApplicationDbContext dbContext,
        IValidator<CreateFeePlanRequest> validator)
        : base(validator)
    {
        _dbContext = dbContext;
    }

    protected override async Task<FeePlanDto> HandleAsync(
        CreateFeePlanRequest request,
        CancellationToken ct)
    {
        var trimmedName = request.Name.Trim();
        var exists = await _dbContext.FeePlans
            .AnyAsync(fp => fp.Name.ToLower() == trimmedName.ToLower(), ct);

        if (exists)
        {
            throw new ConflictException("Fee plan with this name already exists.");
        }

        var feePlan = new FeePlan(trimmedName, request.Description, request.MonthlyAmount, isActive: true);
        _dbContext.FeePlans.Add(feePlan);
        await _dbContext.SaveChangesAsync(ct);

        return new FeePlanDto(
            feePlan.Id,
            feePlan.Name,
            feePlan.Description,
            feePlan.MonthlyAmount,
            feePlan.IsActive);
    }
}


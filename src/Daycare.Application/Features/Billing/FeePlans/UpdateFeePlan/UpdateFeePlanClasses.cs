using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Billing.FeePlans.DTOs;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Billing.FeePlans.UpdateFeePlan;

public record UpdateFeePlanRequest(
    Guid Id,
    string Name,
    string? Description,
    decimal MonthlyAmount,
    bool IsActive
);

public class UpdateFeePlanValidator : AbstractValidator<UpdateFeePlanRequest>
{
    public UpdateFeePlanValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Fee plan ID is required.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100).WithMessage("Fee plan name is required and must not exceed 100 characters.");
        RuleFor(x => x.Description).MaximumLength(500).WithMessage("Description must not exceed 500 characters.");
        RuleFor(x => x.MonthlyAmount).GreaterThanOrEqualTo(0).WithMessage("Monthly amount cannot be negative.");
    }
}

public class UpdateFeePlanUseCase : UseCase<UpdateFeePlanRequest, FeePlanDto>
{
    private readonly IApplicationDbContext _dbContext;

    public UpdateFeePlanUseCase(
        IApplicationDbContext dbContext,
        IValidator<UpdateFeePlanRequest> validator)
        : base(validator)
    {
        _dbContext = dbContext;
    }

    protected override async Task<FeePlanDto> HandleAsync(
        UpdateFeePlanRequest request,
        CancellationToken ct)
    {
        var feePlan = await _dbContext.FeePlans
            .FirstOrDefaultAsync(fp => fp.Id == request.Id, ct);

        if (feePlan == null)
        {
            throw new NotFoundException("FeePlan", request.Id);
        }

        var trimmedName = request.Name.Trim();
        var nameInUse = await _dbContext.FeePlans
            .AnyAsync(fp => fp.Id != request.Id && fp.Name.ToLower() == trimmedName.ToLower(), ct);

        if (nameInUse)
        {
            throw new ConflictException("Another fee plan with this name already exists.");
        }

        feePlan.Update(trimmedName, request.Description, request.MonthlyAmount, request.IsActive);
        await _dbContext.SaveChangesAsync(ct);

        return new FeePlanDto(
            feePlan.Id,
            feePlan.Name,
            feePlan.Description,
            feePlan.MonthlyAmount,
            feePlan.IsActive);
    }
}


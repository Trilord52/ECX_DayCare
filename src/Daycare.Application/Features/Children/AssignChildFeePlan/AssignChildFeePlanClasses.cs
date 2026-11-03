using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Children.AssignChildFeePlan;

public record AssignChildFeePlanRequest(Guid ChildId, Guid? FeePlanId);

public record AssignChildFeePlanResponse(bool Success = true);

public class AssignChildFeePlanUseCase : UseCase<AssignChildFeePlanRequest, AssignChildFeePlanResponse>
{
    private readonly IApplicationDbContext _dbContext;

    public AssignChildFeePlanUseCase(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    protected override async Task<AssignChildFeePlanResponse> HandleAsync(
        AssignChildFeePlanRequest request,
        CancellationToken ct)
    {
        var child = await _dbContext.Children
            .FirstOrDefaultAsync(c => c.Id == request.ChildId, ct);

        if (child == null)
        {
            throw new NotFoundException("Child", request.ChildId);
        }

        if (request.FeePlanId.HasValue)
        {
            var feePlan = await _dbContext.FeePlans
                .FirstOrDefaultAsync(f => f.Id == request.FeePlanId.Value, ct);

            if (feePlan == null)
            {
                throw new NotFoundException("FeePlan", request.FeePlanId.Value);
            }
        }

        child.AssignFeePlan(request.FeePlanId);
        await _dbContext.SaveChangesAsync(ct);

        return new AssignChildFeePlanResponse(true);
    }
}

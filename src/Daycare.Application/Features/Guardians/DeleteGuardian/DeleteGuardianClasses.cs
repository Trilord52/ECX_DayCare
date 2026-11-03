using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Domain.Enums;
using Daycare.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Guardians.DeleteGuardian;

public record DeleteGuardianRequest(Guid Id);

public record DeleteGuardianResponse(bool Success = true);

public class DeleteGuardianUseCase : UseCase<DeleteGuardianRequest, DeleteGuardianResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IIdentityService _identityService;
    private readonly ProfilePhotoManager _photoManager;

    public DeleteGuardianUseCase(
        IApplicationDbContext dbContext,
        IDateTimeProvider dateTimeProvider,
        IIdentityService identityService,
        ProfilePhotoManager photoManager)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
        _identityService = identityService;
        _photoManager = photoManager;
    }

    protected override async Task<DeleteGuardianResponse> HandleAsync(
        DeleteGuardianRequest request,
        CancellationToken ct)
    {
        var guardian = await _dbContext.Guardians
            .FirstOrDefaultAsync(g => g.Id == request.Id, ct);

        if (guardian == null)
        {
            throw new NotFoundException("Guardian", request.Id);
        }

        // BR-07: Soft-deleting a guardian is rejected if that guardian is the only pickup guardian of any active child
        var activeChildrenWhereSolePickup = await _dbContext.ChildGuardians
            .Where(cg => cg.GuardianId == request.Id && cg.CanPickUp && cg.Child.Status == ChildStatus.Active)
            .Where(cg => !_dbContext.ChildGuardians.Any(other => other.ChildId == cg.ChildId && other.GuardianId != request.Id && other.CanPickUp))
            .Select(cg => cg.Child.FirstName + " " + cg.Child.LastName)
            .ToListAsync(ct);

        if (activeChildrenWhereSolePickup.Any())
        {
            throw new DomainRuleException(
                "NO_PICKUP_GUARDIAN",
                $"Cannot delete guardian because they are the sole authorized pickup contact for: {string.Join(", ", activeChildrenWhereSolePickup)}.");
        }

        var previousKey = guardian.RemovePhoto();
        guardian.SoftDelete(_dateTimeProvider.UtcNow);

        if (guardian.UserId.HasValue)
        {
            await _identityService.DeactivateUserAsync(guardian.UserId.Value, ct);
        }

        await _dbContext.SaveChangesAsync(ct);

        if (previousKey != null)
        {
            await _photoManager.DeleteStoredPhotoAsync(previousKey);
        }

        return new DeleteGuardianResponse(true);
    }
}

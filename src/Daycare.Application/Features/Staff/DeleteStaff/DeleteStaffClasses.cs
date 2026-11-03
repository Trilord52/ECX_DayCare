using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Staff.DeleteStaff;

public record DeleteStaffRequest(Guid Id);

public record DeleteStaffResponse(bool Success = true);

public class DeleteStaffUseCase : UseCase<DeleteStaffRequest, DeleteStaffResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IIdentityService _identityService;
    private readonly ProfilePhotoManager _photoManager;

    public DeleteStaffUseCase(
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

    protected override async Task<DeleteStaffResponse> HandleAsync(DeleteStaffRequest request, CancellationToken ct)
    {
        var staff = await _dbContext.Staff
            .FirstOrDefaultAsync(s => s.Id == request.Id, ct);

        if (staff == null)
        {
            throw new NotFoundException("Staff member", request.Id);
        }

        var previousKey = staff.RemovePhoto();
        staff.SoftDelete(_dateTimeProvider.UtcNow);
        staff.Deactivate();

        if (staff.UserId.HasValue)
        {
            await _identityService.DeactivateUserAsync(staff.UserId.Value, ct);
        }

        await _dbContext.SaveChangesAsync(ct);

        if (previousKey != null)
        {
            await _photoManager.DeleteStoredPhotoAsync(previousKey);
        }

        return new DeleteStaffResponse(true);
    }
}

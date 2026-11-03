using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Children.DeleteChild;

public record DeleteChildRequest(Guid Id);

public record DeleteChildResponse(bool Success = true);

public class DeleteChildUseCase : UseCase<DeleteChildRequest, DeleteChildResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ProfilePhotoManager _photoManager;

    public DeleteChildUseCase(
        IApplicationDbContext dbContext,
        IDateTimeProvider dateTimeProvider,
        ProfilePhotoManager photoManager)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
        _photoManager = photoManager;
    }

    protected override async Task<DeleteChildResponse> HandleAsync(DeleteChildRequest request, CancellationToken ct)
    {
        var child = await _dbContext.Children
            .FirstOrDefaultAsync(c => c.Id == request.Id, ct);

        if (child == null)
        {
            throw new NotFoundException("Child", request.Id);
        }

        var previousKey = child.RemovePhoto();
        child.SoftDelete(_dateTimeProvider.UtcNow);
        await _dbContext.SaveChangesAsync(ct);

        if (previousKey != null)
        {
            await _photoManager.DeleteStoredPhotoAsync(previousKey);
        }

        return new DeleteChildResponse(true);
    }
}

using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Children.Photos;

public record SetChildPhotoRequest(Guid ChildId, Stream Content, long? ContentLength);

public class SetChildPhotoUseCase : UseCase<SetChildPhotoRequest, PhotoUploadResultDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAccessGuard _accessGuard;
    private readonly ProfilePhotoManager _photoManager;

    public SetChildPhotoUseCase(
        IApplicationDbContext dbContext,
        IAccessGuard accessGuard,
        ProfilePhotoManager photoManager)
    {
        _dbContext = dbContext;
        _accessGuard = accessGuard;
        _photoManager = photoManager;
    }

    protected override async Task<PhotoUploadResultDto> HandleAsync(SetChildPhotoRequest request, CancellationToken ct)
    {
        _accessGuard.EnsureAdmin();

        var child = await _dbContext.Children
            .FirstOrDefaultAsync(c => c.Id == request.ChildId, ct);

        if (child == null)
        {
            throw new NotFoundException("Child", request.ChildId);
        }

        var (photo, newKey) = await _photoManager.UploadAndProcessAsync(
            "children",
            child.Id,
            request.Content,
            request.ContentLength,
            ct);

        var previousKey = child.SetPhoto(photo);

        try
        {
            await _dbContext.SaveChangesAsync(ct);
        }
        catch
        {
            await _photoManager.DeleteStoredPhotoAsync(newKey);
            throw;
        }

        if (previousKey != null)
        {
            await _photoManager.DeleteStoredPhotoAsync(previousKey);
        }

        return new PhotoUploadResultDto(true, photo.UpdatedAt);
    }
}

public record RemoveChildPhotoRequest(Guid ChildId);

public class RemoveChildPhotoUseCase : UseCase<RemoveChildPhotoRequest, PhotoUploadResultDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAccessGuard _accessGuard;
    private readonly ProfilePhotoManager _photoManager;

    public RemoveChildPhotoUseCase(
        IApplicationDbContext dbContext,
        IAccessGuard accessGuard,
        ProfilePhotoManager photoManager)
    {
        _dbContext = dbContext;
        _accessGuard = accessGuard;
        _photoManager = photoManager;
    }

    protected override async Task<PhotoUploadResultDto> HandleAsync(RemoveChildPhotoRequest request, CancellationToken ct)
    {
        _accessGuard.EnsureAdmin();

        var child = await _dbContext.Children
            .FirstOrDefaultAsync(c => c.Id == request.ChildId, ct);

        if (child == null)
        {
            throw new NotFoundException("Child", request.ChildId);
        }

        var previousKey = child.RemovePhoto();
        await _dbContext.SaveChangesAsync(ct);

        if (previousKey != null)
        {
            await _photoManager.DeleteStoredPhotoAsync(previousKey);
        }

        return new PhotoUploadResultDto(false, null);
    }
}

public record GetChildPhotoRequest(Guid ChildId);

public class GetChildPhotoUseCase : UseCase<GetChildPhotoRequest, PhotoDownloadResult>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAccessGuard _accessGuard;
    private readonly ProfilePhotoManager _photoManager;

    public GetChildPhotoUseCase(
        IApplicationDbContext dbContext,
        IAccessGuard accessGuard,
        ProfilePhotoManager photoManager)
    {
        _dbContext = dbContext;
        _accessGuard = accessGuard;
        _photoManager = photoManager;
    }

    protected override async Task<PhotoDownloadResult> HandleAsync(GetChildPhotoRequest request, CancellationToken ct)
    {
        await _accessGuard.EnsureCanViewChildAsync(request.ChildId, ct);

        var child = await _dbContext.Children
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.ChildId, ct);

        if (child == null)
        {
            throw new NotFoundException("Child", request.ChildId);
        }

        if (child.Photo == null)
        {
            throw new NotFoundException("Profile photo not found.");
        }

        return await _photoManager.GetPhotoAsync(child.Photo, ct);
    }
}


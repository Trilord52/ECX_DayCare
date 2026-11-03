using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Guardians.Photos;

public record SetGuardianPhotoRequest(Guid GuardianId, Stream Content, long? ContentLength);

public class SetGuardianPhotoUseCase : UseCase<SetGuardianPhotoRequest, PhotoUploadResultDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAccessGuard _accessGuard;
    private readonly ProfilePhotoManager _photoManager;

    public SetGuardianPhotoUseCase(
        IApplicationDbContext dbContext,
        IAccessGuard accessGuard,
        ProfilePhotoManager photoManager)
    {
        _dbContext = dbContext;
        _accessGuard = accessGuard;
        _photoManager = photoManager;
    }

    protected override async Task<PhotoUploadResultDto> HandleAsync(SetGuardianPhotoRequest request, CancellationToken ct)
    {
        _accessGuard.EnsureAdmin();

        var guardian = await _dbContext.Guardians
            .FirstOrDefaultAsync(g => g.Id == request.GuardianId, ct);

        if (guardian == null)
        {
            throw new NotFoundException("Guardian", request.GuardianId);
        }

        var (photo, newKey) = await _photoManager.UploadAndProcessAsync(
            "guardians",
            guardian.Id,
            request.Content,
            request.ContentLength,
            ct);

        var previousKey = guardian.SetPhoto(photo);

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

public record RemoveGuardianPhotoRequest(Guid GuardianId);

public class RemoveGuardianPhotoUseCase : UseCase<RemoveGuardianPhotoRequest, PhotoUploadResultDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAccessGuard _accessGuard;
    private readonly ProfilePhotoManager _photoManager;

    public RemoveGuardianPhotoUseCase(
        IApplicationDbContext dbContext,
        IAccessGuard accessGuard,
        ProfilePhotoManager photoManager)
    {
        _dbContext = dbContext;
        _accessGuard = accessGuard;
        _photoManager = photoManager;
    }

    protected override async Task<PhotoUploadResultDto> HandleAsync(RemoveGuardianPhotoRequest request, CancellationToken ct)
    {
        _accessGuard.EnsureAdmin();

        var guardian = await _dbContext.Guardians
            .FirstOrDefaultAsync(g => g.Id == request.GuardianId, ct);

        if (guardian == null)
        {
            throw new NotFoundException("Guardian", request.GuardianId);
        }

        var previousKey = guardian.RemovePhoto();
        await _dbContext.SaveChangesAsync(ct);

        if (previousKey != null)
        {
            await _photoManager.DeleteStoredPhotoAsync(previousKey);
        }

        return new PhotoUploadResultDto(false, null);
    }
}

public record GetGuardianPhotoRequest(Guid GuardianId);

public class GetGuardianPhotoUseCase : UseCase<GetGuardianPhotoRequest, PhotoDownloadResult>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAccessGuard _accessGuard;
    private readonly ProfilePhotoManager _photoManager;

    public GetGuardianPhotoUseCase(
        IApplicationDbContext dbContext,
        IAccessGuard accessGuard,
        ProfilePhotoManager photoManager)
    {
        _dbContext = dbContext;
        _accessGuard = accessGuard;
        _photoManager = photoManager;
    }

    protected override async Task<PhotoDownloadResult> HandleAsync(GetGuardianPhotoRequest request, CancellationToken ct)
    {
        await _accessGuard.EnsureCanViewGuardianPhotoAsync(request.GuardianId, ct);

        var guardian = await _dbContext.Guardians
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == request.GuardianId, ct);

        if (guardian == null)
        {
            throw new NotFoundException("Guardian", request.GuardianId);
        }

        if (guardian.Photo == null)
        {
            throw new NotFoundException("Profile photo not found.");
        }

        return await _photoManager.GetPhotoAsync(guardian.Photo, ct);
    }
}


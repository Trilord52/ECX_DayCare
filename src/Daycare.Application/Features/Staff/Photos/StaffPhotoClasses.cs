using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Staff.Photos;

public record SetStaffPhotoRequest(Guid StaffId, Stream Content, long? ContentLength);

public class SetStaffPhotoUseCase : UseCase<SetStaffPhotoRequest, PhotoUploadResultDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAccessGuard _accessGuard;
    private readonly ProfilePhotoManager _photoManager;

    public SetStaffPhotoUseCase(
        IApplicationDbContext dbContext,
        IAccessGuard accessGuard,
        ProfilePhotoManager photoManager)
    {
        _dbContext = dbContext;
        _accessGuard = accessGuard;
        _photoManager = photoManager;
    }

    protected override async Task<PhotoUploadResultDto> HandleAsync(SetStaffPhotoRequest request, CancellationToken ct)
    {
        _accessGuard.EnsureAdmin();

        var staff = await _dbContext.Staff
            .FirstOrDefaultAsync(s => s.Id == request.StaffId, ct);

        if (staff == null)
        {
            throw new NotFoundException("Staff member", request.StaffId);
        }

        var (photo, newKey) = await _photoManager.UploadAndProcessAsync(
            "staff",
            staff.Id,
            request.Content,
            request.ContentLength,
            ct);

        var previousKey = staff.SetPhoto(photo);

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

public record RemoveStaffPhotoRequest(Guid StaffId);

public class RemoveStaffPhotoUseCase : UseCase<RemoveStaffPhotoRequest, PhotoUploadResultDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAccessGuard _accessGuard;
    private readonly ProfilePhotoManager _photoManager;

    public RemoveStaffPhotoUseCase(
        IApplicationDbContext dbContext,
        IAccessGuard accessGuard,
        ProfilePhotoManager photoManager)
    {
        _dbContext = dbContext;
        _accessGuard = accessGuard;
        _photoManager = photoManager;
    }

    protected override async Task<PhotoUploadResultDto> HandleAsync(RemoveStaffPhotoRequest request, CancellationToken ct)
    {
        _accessGuard.EnsureAdmin();

        var staff = await _dbContext.Staff
            .FirstOrDefaultAsync(s => s.Id == request.StaffId, ct);

        if (staff == null)
        {
            throw new NotFoundException("Staff member", request.StaffId);
        }

        var previousKey = staff.RemovePhoto();
        await _dbContext.SaveChangesAsync(ct);

        if (previousKey != null)
        {
            await _photoManager.DeleteStoredPhotoAsync(previousKey);
        }

        return new PhotoUploadResultDto(false, null);
    }
}

public record GetStaffPhotoRequest(Guid StaffId);

public class GetStaffPhotoUseCase : UseCase<GetStaffPhotoRequest, PhotoDownloadResult>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAccessGuard _accessGuard;
    private readonly ProfilePhotoManager _photoManager;

    public GetStaffPhotoUseCase(
        IApplicationDbContext dbContext,
        IAccessGuard accessGuard,
        ProfilePhotoManager photoManager)
    {
        _dbContext = dbContext;
        _accessGuard = accessGuard;
        _photoManager = photoManager;
    }

    protected override async Task<PhotoDownloadResult> HandleAsync(GetStaffPhotoRequest request, CancellationToken ct)
    {
        await _accessGuard.EnsureCanViewStaffPhotoAsync(request.StaffId, ct);

        var staff = await _dbContext.Staff
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.StaffId, ct);

        if (staff == null)
        {
            throw new NotFoundException("Staff member", request.StaffId);
        }

        if (staff.Photo == null)
        {
            throw new NotFoundException("Profile photo not found.");
        }

        return await _photoManager.GetPhotoAsync(staff.Photo, ct);
    }
}


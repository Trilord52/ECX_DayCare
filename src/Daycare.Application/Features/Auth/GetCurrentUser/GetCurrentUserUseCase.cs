using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Auth.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Auth.GetCurrentUser;

public class GetCurrentUserUseCase : UseCase<GetCurrentUserRequest, CurrentUserDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly IIdentityService _identityService;
    private readonly IApplicationDbContext _dbContext;

    public GetCurrentUserUseCase(
        ICurrentUser currentUser,
        IIdentityService identityService,
        IApplicationDbContext dbContext)
    {
        _currentUser = currentUser;
        _identityService = identityService;
        _dbContext = dbContext;
    }

    protected override async Task<CurrentUserDto> HandleAsync(GetCurrentUserRequest request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
        {
            throw new UnauthorizedException("User is not authenticated.", "UNAUTHORIZED");
        }

        var user = await _identityService.GetUserByIdAsync(_currentUser.UserId.Value, ct);
        if (user == null || !user.IsActive)
        {
            throw new UnauthorizedException("User not found or inactive.", "USER_NOT_FOUND");
        }

        bool hasPhoto = false;
        DateTime? photoUpdatedAt = null;

        if (user.GuardianId.HasValue)
        {
            var photo = await _dbContext.Guardians
                .Where(g => g.Id == user.GuardianId.Value)
                .Select(g => g.Photo)
                .FirstOrDefaultAsync(ct);

            if (photo != null)
            {
                hasPhoto = true;
                photoUpdatedAt = photo.UpdatedAt;
            }
        }
        else if (user.StaffId.HasValue)
        {
            var photo = await _dbContext.Staff
                .Where(s => s.Id == user.StaffId.Value)
                .Select(s => s.Photo)
                .FirstOrDefaultAsync(ct);

            if (photo != null)
            {
                hasPhoto = true;
                photoUpdatedAt = photo.UpdatedAt;
            }
        }

        return new CurrentUserDto(
            user.Id,
            user.Email,
            user.FullName,
            user.Role,
            user.MustChangePassword,
            user.GuardianId,
            user.StaffId,
            hasPhoto,
            photoUpdatedAt);
    }
}

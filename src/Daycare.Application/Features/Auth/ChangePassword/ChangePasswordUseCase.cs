using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using FluentValidation;

namespace Daycare.Application.Features.Auth.ChangePassword;

public class ChangePasswordUseCase : UseCase<ChangePasswordRequest, ChangePasswordResponse>
{
    private readonly ICurrentUser _currentUser;
    private readonly IIdentityService _identityService;

    public ChangePasswordUseCase(
        ICurrentUser currentUser,
        IIdentityService identityService,
        IValidator<ChangePasswordRequest> validator)
        : base(validator)
    {
        _currentUser = currentUser;
        _identityService = identityService;
    }

    protected override async Task<ChangePasswordResponse> HandleAsync(ChangePasswordRequest request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
        {
            throw new UnauthorizedException("User is not authenticated.", "UNAUTHORIZED");
        }

        var (success, errorCode, errorMessage) = await _identityService.ChangePasswordAsync(
            _currentUser.UserId.Value,
            request.CurrentPassword,
            request.NewPassword,
            ct);

        if (!success)
        {
            if (errorCode == "INVALID_PASSWORD" || errorCode == "INVALID_CREDENTIALS")
            {
                throw new ValidationFailedException("currentPassword", errorMessage ?? "Current password is incorrect.");
            }

            throw new ValidationFailedException("newPassword", errorMessage ?? "Failed to change password.");
        }

        return new ChangePasswordResponse(true);
    }
}

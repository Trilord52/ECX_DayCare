using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Auth.DTOs;
using FluentValidation;

namespace Daycare.Application.Features.Auth.Login;

public class LoginUseCase : UseCase<LoginRequest, LoginResponse>
{
    private readonly IIdentityService _identityService;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginUseCase(
        IIdentityService identityService,
        IJwtTokenGenerator jwtTokenGenerator,
        IValidator<LoginRequest> validator)
        : base(validator)
    {
        _identityService = identityService;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    protected override async Task<LoginResponse> HandleAsync(LoginRequest request, CancellationToken ct)
    {
        var (success, errorCode, errorMessage, user) = await _identityService.AuthenticateAsync(
            request.Email.Trim().ToLowerInvariant(),
            request.Password,
            ct);

        if (!success || user == null)
        {
            throw new UnauthorizedException(
                errorMessage ?? "Invalid email or password.",
                errorCode ?? "INVALID_CREDENTIALS");
        }

        var (token, expiresAt) = _jwtTokenGenerator.GenerateToken(user);

        var currentUserDto = new CurrentUserDto(
            user.Id,
            user.Email,
            user.FullName,
            user.Role,
            user.MustChangePassword,
            user.GuardianId,
            user.StaffId);

        return new LoginResponse(token, expiresAt, currentUserDto);
    }
}

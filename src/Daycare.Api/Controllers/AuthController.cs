using Daycare.Application.Features.Auth.ChangePassword;
using Daycare.Application.Features.Auth.DTOs;
using Daycare.Application.Features.Auth.GetCurrentUser;
using Daycare.Application.Features.Auth.Login;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Daycare.Api.Controllers;

public class AuthController : BaseApiController
{
    private readonly LoginUseCase _loginUseCase;
    private readonly GetCurrentUserUseCase _getCurrentUserUseCase;
    private readonly ChangePasswordUseCase _changePasswordUseCase;

    public AuthController(
        LoginUseCase loginUseCase,
        GetCurrentUserUseCase getCurrentUserUseCase,
        ChangePasswordUseCase changePasswordUseCase)
    {
        _loginUseCase = loginUseCase;
        _getCurrentUserUseCase = getCurrentUserUseCase;
        _changePasswordUseCase = changePasswordUseCase;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var response = await _loginUseCase.ExecuteAsync(request, ct);
        return Ok(response);
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(CurrentUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentUserDto>> Me(CancellationToken ct)
    {
        var response = await _getCurrentUserUseCase.ExecuteAsync(new GetCurrentUserRequest(), ct);
        return Ok(response);
    }

    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(ChangePasswordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ChangePasswordResponse>> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken ct)
    {
        var response = await _changePasswordUseCase.ExecuteAsync(request, ct);
        return Ok(response);
    }
}

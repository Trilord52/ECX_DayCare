using Daycare.Application.Features.Auth.DTOs;

namespace Daycare.Application.Features.Auth.Login;

public record LoginRequest(string Email, string Password);

public record LoginResponse(string AccessToken, DateTime ExpiresAt, CurrentUserDto User);

namespace Daycare.Application.Features.Auth.ChangePassword;

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public record ChangePasswordResponse(bool Success = true);

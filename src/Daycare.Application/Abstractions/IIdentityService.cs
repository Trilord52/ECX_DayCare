namespace Daycare.Application.Abstractions;

public record ApplicationUserDto(
    Guid Id,
    string Email,
    string FullName,
    string Role,
    bool MustChangePassword,
    Guid? GuardianId,
    Guid? StaffId,
    bool IsActive
);

public interface IIdentityService
{
    Task<(bool Success, string? ErrorCode, string? ErrorMessage, ApplicationUserDto? User)> AuthenticateAsync(
        string email,
        string password,
        CancellationToken ct = default);

    Task<(bool Success, string? ErrorCode, string? ErrorMessage)> ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken ct = default);

    Task<ApplicationUserDto?> GetUserByIdAsync(
        Guid userId,
        CancellationToken ct = default);

    Task<(bool Success, string? ErrorCode, string? ErrorMessage, Guid? UserId, string? TemporaryPassword)> CreateUserAsync(
        string email,
        string fullName,
        string role,
        CancellationToken ct = default);

    Task<(bool Success, string? ErrorCode, string? ErrorMessage, string? TemporaryPassword)> ResetPasswordAsync(
        Guid userId,
        CancellationToken ct = default);

    Task<(bool Success, string? ErrorCode, string? ErrorMessage)> DeactivateUserAsync(
        Guid userId,
        CancellationToken ct = default);

    Task<bool> ValidateUserActiveAsync(
        Guid userId,
        CancellationToken ct = default);
}

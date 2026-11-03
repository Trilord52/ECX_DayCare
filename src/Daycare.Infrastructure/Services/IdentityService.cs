using Daycare.Application.Abstractions;
using Daycare.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Infrastructure.Services;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IApplicationDbContext _dbContext;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IApplicationDbContext dbContext)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _dbContext = dbContext;
    }

    public async Task<(bool Success, string? ErrorCode, string? ErrorMessage, ApplicationUserDto? User)> AuthenticateAsync(
        string email,
        string password,
        CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null || !user.IsActive)
        {
            return (false, "INVALID_CREDENTIALS", "Invalid email or password.", null);
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            return (false, "ACCOUNT_LOCKED", "Account is locked due to multiple failed login attempts. Please try again later.", null);
        }

        var signInResult = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);

        if (signInResult.IsLockedOut)
        {
            return (false, "ACCOUNT_LOCKED", "Account is locked due to multiple failed login attempts. Please try again later.", null);
        }

        if (!signInResult.Succeeded)
        {
            return (false, "INVALID_CREDENTIALS", "Invalid email or password.", null);
        }

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "Parent";

        Guid? guardianId = null;
        Guid? staffId = null;

        if (role == "Parent")
        {
            guardianId = await _dbContext.Guardians
                .AsNoTracking()
                .Where(g => g.UserId == user.Id)
                .Select(g => (Guid?)g.Id)
                .FirstOrDefaultAsync(ct);
        }
        else if (role == "Staff")
        {
            staffId = await _dbContext.Staff
                .AsNoTracking()
                .Where(s => s.UserId == user.Id)
                .Select(s => (Guid?)s.Id)
                .FirstOrDefaultAsync(ct);
        }

        var userDto = new ApplicationUserDto(
            user.Id,
            user.Email ?? string.Empty,
            user.FullName,
            role,
            user.MustChangePassword,
            guardianId,
            staffId,
            user.IsActive);

        return (true, null, null, userDto);
    }

    public async Task<(bool Success, string? ErrorCode, string? ErrorMessage)> ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return (false, "NOT_FOUND", "User not found.");
        }

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!result.Succeeded)
        {
            var firstError = result.Errors.FirstOrDefault();
            var errorCode = firstError?.Code == "PasswordMismatch" ? "INVALID_PASSWORD" : "VALIDATION_FAILED";
            var errorMessage = firstError?.Description ?? "Failed to change password.";
            return (false, errorCode, errorMessage);
        }

        user.MustChangePassword = false;
        await _userManager.UpdateAsync(user);

        return (true, null, null);
    }

    public async Task<ApplicationUserDto?> GetUserByIdAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "Parent";

        Guid? guardianId = null;
        Guid? staffId = null;

        if (role == "Parent")
        {
            guardianId = await _dbContext.Guardians
                .AsNoTracking()
                .Where(g => g.UserId == user.Id)
                .Select(g => (Guid?)g.Id)
                .FirstOrDefaultAsync(ct);
        }
        else if (role == "Staff")
        {
            staffId = await _dbContext.Staff
                .AsNoTracking()
                .Where(s => s.UserId == user.Id)
                .Select(s => (Guid?)s.Id)
                .FirstOrDefaultAsync(ct);
        }

        return new ApplicationUserDto(
            user.Id,
            user.Email ?? string.Empty,
            user.FullName,
            role,
            user.MustChangePassword,
            guardianId,
            staffId,
            user.IsActive);
    }

    public async Task<(bool Success, string? ErrorCode, string? ErrorMessage, Guid? UserId, string? TemporaryPassword)> CreateUserAsync(
        string email,
        string fullName,
        string role,
        CancellationToken ct = default)
    {
        var existing = await _userManager.FindByEmailAsync(email);
        if (existing != null)
        {
            return (false, "EMAIL_IN_USE", "Email is already in use by another account.", null, null);
        }

        var tempPassword = $"Temp@{System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000, 999999)}!";

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            MustChangePassword = true,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, tempPassword);
        if (!result.Succeeded)
        {
            var msg = result.Errors.FirstOrDefault()?.Description ?? "Failed to create user account.";
            return (false, "VALIDATION_FAILED", msg, null, null);
        }

        await _userManager.AddToRoleAsync(user, role);

        return (true, null, null, user.Id, tempPassword);
    }

    public async Task<(bool Success, string? ErrorCode, string? ErrorMessage, string? TemporaryPassword)> ResetPasswordAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return (false, "NOT_FOUND", "User not found.", null);
        }

        var tempPassword = $"Temp@{System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000, 999999)}!";
        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, resetToken, tempPassword);

        if (!result.Succeeded)
        {
            var msg = result.Errors.FirstOrDefault()?.Description ?? "Failed to reset password.";
            return (false, "VALIDATION_FAILED", msg, null);
        }

        user.MustChangePassword = true;
        await _userManager.UpdateAsync(user);

        return (true, null, null, tempPassword);
    }

    public async Task<(bool Success, string? ErrorCode, string? ErrorMessage)> DeactivateUserAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return (false, "NOT_FOUND", "User not found.");
        }

        user.IsActive = false;
        await _userManager.UpdateAsync(user);

        return (true, null, null);
    }

    public async Task<bool> ValidateUserActiveAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        return user != null && user.IsActive;
    }
}

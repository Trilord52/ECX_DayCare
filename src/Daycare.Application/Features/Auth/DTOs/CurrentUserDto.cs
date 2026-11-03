namespace Daycare.Application.Features.Auth.DTOs;

public record CurrentUserDto(
    Guid Id,
    string Email,
    string FullName,
    string Role,
    bool MustChangePassword,
    Guid? GuardianId,
    Guid? StaffId,
    bool HasPhoto = false,
    DateTime? PhotoUpdatedAt = null
);

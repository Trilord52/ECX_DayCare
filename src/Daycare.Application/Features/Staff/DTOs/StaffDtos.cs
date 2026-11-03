namespace Daycare.Application.Features.Staff.DTOs;

public record ClassroomBriefDto(
    Guid Id,
    string Name
);

public record StaffDto(
    Guid Id,
    Guid? UserId,
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    string Phone,
    string JobTitle,
    DateOnly HireDate,
    bool IsActive,
    IReadOnlyList<ClassroomBriefDto> AssignedClassrooms,
    bool HasPhoto = false,
    DateTime? PhotoUpdatedAt = null
);

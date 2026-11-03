namespace Daycare.Application.Features.Guardians.DTOs;

public record ChildBriefDto(
    Guid Id,
    string FullName,
    string Status
);

public record GuardianDto(
    Guid Id,
    Guid? UserId,
    string FirstName,
    string LastName,
    string FullName,
    string Phone,
    string? Email,
    string? Address,
    IReadOnlyList<ChildBriefDto> LinkedChildren,
    bool HasPhoto = false,
    DateTime? PhotoUpdatedAt = null
);

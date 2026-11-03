namespace Daycare.Application.Features.Classrooms.DTOs;

public record ClassroomDto(
    Guid Id,
    string Name,
    string? Description,
    int MinAgeMonths,
    int MaxAgeMonths,
    int Capacity,
    int MaxChildrenPerStaff,
    bool IsActive,
    int EnrolledCount,
    int StaffCount
);

public record StaffBriefDto(
    Guid Id,
    string FullName,
    string JobTitle
);

public record ClassroomDetailDto(
    Guid Id,
    string Name,
    string? Description,
    int MinAgeMonths,
    int MaxAgeMonths,
    int Capacity,
    int MaxChildrenPerStaff,
    bool IsActive,
    int EnrolledCount,
    int StaffCount,
    IReadOnlyList<StaffBriefDto> AssignedStaff
);

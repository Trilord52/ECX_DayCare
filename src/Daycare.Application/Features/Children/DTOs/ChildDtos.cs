using Daycare.Domain.Enums;

namespace Daycare.Application.Features.Children.DTOs;

public record ChildListItemDto(
    Guid Id,
    string FirstName,
    string LastName,
    string FullName,
    DateOnly DateOfBirth,
    int AgeInMonths,
    Gender Gender,
    Guid? ClassroomId,
    string? ClassroomName,
    ChildStatus Status,
    DateOnly EnrollmentDate,
    string? Allergies,
    bool HasPhoto = false,
    DateTime? PhotoUpdatedAt = null
);

public record ChildGuardianDto(
    Guid GuardianId,
    string FullName,
    Relationship Relationship,
    string Phone,
    string? Email,
    bool IsPrimaryContact,
    bool CanPickUp,
    bool IsPayer,
    bool IsEmergencyContact,
    bool HasPhoto = false,
    DateTime? PhotoUpdatedAt = null
);

public record ChildDetailDto(
    Guid Id,
    string FirstName,
    string LastName,
    string FullName,
    DateOnly DateOfBirth,
    int AgeInMonths,
    Gender Gender,
    Guid? ClassroomId,
    string? ClassroomName,
    Guid? FeePlanId,
    string? FeePlanName,
    ChildStatus Status,
    DateOnly EnrollmentDate,
    string? Allergies,
    string? MedicalNotes,
    string? DietaryNotes,
    IReadOnlyList<ChildGuardianDto> Guardians,
    bool HasPhoto = false,
    DateTime? PhotoUpdatedAt = null
);

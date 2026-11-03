using Daycare.Domain.Enums;

namespace Daycare.Application.Features.Attendance.DTOs;

public record AttendanceRecordDto(
    Guid Id,
    Guid ChildId,
    string ChildName,
    Guid ClassroomId,
    string ClassroomName,
    DateOnly Date,
    AttendanceStatus Status,
    DateTime? CheckInAt,
    string? CheckInActor,
    Guid? DroppedOffByGuardianId,
    string? DroppedOffByName,
    DateTime? CheckOutAt,
    string? CheckOutActor,
    Guid? PickedUpByGuardianId,
    string? PickedUpByName,
    bool? IdVerified,
    int LateMinutes,
    string? AbsenceReason,
    string? Notes
);

public record RosterGuardianDto(
    Guid GuardianId,
    string FullName,
    Relationship Relationship,
    string Phone,
    bool CanPickUp,
    bool HasPhoto = false,
    DateTime? PhotoUpdatedAt = null
);

public record RosterItemDto(
    Guid ChildId,
    string FullName,
    int AgeInMonths,
    string? Allergies,
    string? MedicalNotes,
    string Status, // NotArrived, Present, CheckedOut, Absent
    Guid? AttendanceId,
    DateTime? CheckInAt,
    DateTime? CheckOutAt,
    int LateMinutes,
    IReadOnlyList<RosterGuardianDto> Guardians,
    bool HasPhoto = false,
    DateTime? PhotoUpdatedAt = null
);

public record RatioItemDto(
    Guid ClassroomId,
    string ClassroomName,
    int PresentCount,
    int StaffCount,
    int MaxChildrenPerStaff,
    int AllowedCount,
    bool IsOverLimit
);

public record CheckInResponse(
    AttendanceRecordDto Record,
    bool RatioWarning,
    RatioItemDto Ratio
);

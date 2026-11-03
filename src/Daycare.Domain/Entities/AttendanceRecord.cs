using Daycare.Domain.Common;
using Daycare.Domain.Enums;
using Daycare.Domain.Exceptions;

namespace Daycare.Domain.Entities;

public class AttendanceRecord : BaseEntity
{
    public Guid ChildId { get; private set; }
    public Guid ClassroomId { get; private set; }
    public DateOnly Date { get; private set; }
    public AttendanceStatus Status { get; private set; }
    public DateTime? CheckInAt { get; private set; }
    public Guid? CheckedInByGuardianId { get; private set; }
    public Guid? CheckedInByUserId { get; private set; }
    public string? CheckedInByName { get; private set; }
    public DateTime? CheckOutAt { get; private set; }
    public Guid? CheckedOutByGuardianId { get; private set; }
    public Guid? CheckedOutByUserId { get; private set; }
    public string? CheckedOutByName { get; private set; }
    public bool? PickupIdVerified { get; private set; }
    public int LateMinutes { get; private set; }
    public string? AbsenceReason { get; private set; }
    public string? Notes { get; private set; }

    // Navigations
    public Child Child { get; private set; } = null!;
    public Classroom Classroom { get; private set; } = null!;
    public Guardian? CheckedInByGuardian { get; private set; }
    public Guardian? CheckedOutByGuardian { get; private set; }

    private AttendanceRecord() { }

    public static AttendanceRecord CreateCheckIn(
        Guid childId,
        Guid classroomId,
        DateOnly date,
        DateTime checkInAt,
        Guid? guardianId,
        Guid userId,
        string userName,
        string? notes = null)
    {
        if (checkInAt.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Check-in timestamp must be UTC.", nameof(checkInAt));

        return new AttendanceRecord
        {
            ChildId = childId,
            ClassroomId = classroomId,
            Date = date,
            Status = AttendanceStatus.Present,
            CheckInAt = checkInAt,
            CheckedInByGuardianId = guardianId,
            CheckedInByUserId = userId,
            CheckedInByName = userName,
            Notes = notes?.Trim(),
            LateMinutes = 0
        };
    }

    public static AttendanceRecord CreateAbsent(
        Guid childId,
        Guid classroomId,
        DateOnly date,
        string reason,
        Guid? userId = null,
        string? userName = null,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainRuleException("VALIDATION_FAILED", "Absence reason is required.");

        return new AttendanceRecord
        {
            ChildId = childId,
            ClassroomId = classroomId,
            Date = date,
            Status = AttendanceStatus.Absent,
            CheckInAt = null,
            CheckOutAt = null,
            AbsenceReason = reason.Trim(),
            CheckedInByUserId = userId,
            CheckedInByName = userName,
            Notes = notes?.Trim(),
            LateMinutes = 0
        };
    }

    public void CheckOut(
        DateTime checkOutAt,
        Guid guardianId,
        Guid userId,
        string userName,
        bool idVerified,
        int lateMinutes)
    {
        if (Status != AttendanceStatus.Present || CheckInAt == null)
            throw new DomainRuleException("NOT_CHECKED_IN", "Child is not checked in.");
        if (CheckOutAt != null)
            throw new DomainRuleException("ALREADY_CHECKED_OUT", "Child has already been checked out.");
        if (!idVerified)
            throw new DomainRuleException("ID_NOT_VERIFIED", "Pickup ID must be verified.");
        if (checkOutAt < CheckInAt.Value)
            throw new DomainRuleException("VALIDATION_FAILED", "Check-out time cannot be earlier than check-in time.");

        CheckOutAt = checkOutAt;
        CheckedOutByGuardianId = guardianId;
        CheckedOutByUserId = userId;
        CheckedOutByName = userName;
        PickupIdVerified = true;
        LateMinutes = Math.Max(0, lateMinutes);
    }
}

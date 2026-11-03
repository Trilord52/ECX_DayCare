using Daycare.Domain.Entities;
using Daycare.Domain.Enums;
using Daycare.Domain.Exceptions;
using Shouldly;
using Xunit;

namespace Daycare.Domain.Tests;

public class AttendanceRecordTests
{
    private readonly Guid _childId = Guid.NewGuid();
    private readonly Guid _classroomId = Guid.NewGuid();
    private readonly Guid _guardianId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly DateOnly _date = new(2026, 10, 8);
    private readonly DateTime _checkInAt = new(2026, 10, 8, 5, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void CheckIn_ShouldInitializePresentRecord()
    {
        var record = AttendanceRecord.CreateCheckIn(
            _childId, _classroomId, _date, _checkInAt, _guardianId, _userId, "Staff Name", "Morning note");

        record.ChildId.ShouldBe(_childId);
        record.ClassroomId.ShouldBe(_classroomId);
        record.Date.ShouldBe(_date);
        record.Status.ShouldBe(AttendanceStatus.Present);
        record.CheckInAt.ShouldBe(_checkInAt);
        record.CheckedInByGuardianId.ShouldBe(_guardianId);
        record.CheckedInByUserId.ShouldBe(_userId);
        record.CheckedInByName.ShouldBe("Staff Name");
        record.Notes.ShouldBe("Morning note");
        record.CheckOutAt.ShouldBeNull();
        record.PickupIdVerified.ShouldBeNull();
    }

    [Fact]
    public void CheckOut_WithVerifiedId_ShouldSucceed()
    {
        var record = AttendanceRecord.CreateCheckIn(
            _childId, _classroomId, _date, _checkInAt, _guardianId, _userId, "Staff Name");
        var checkOutAt = _checkInAt.AddHours(8);

        record.CheckOut(checkOutAt, _guardianId, _userId, "Staff Name", idVerified: true, lateMinutes: 15);

        record.CheckOutAt.ShouldBe(checkOutAt);
        record.CheckedOutByGuardianId.ShouldBe(_guardianId);
        record.CheckedOutByUserId.ShouldBe(_userId);
        record.CheckedOutByName.ShouldBe("Staff Name");
        record.PickupIdVerified.ShouldBe(true);
        record.LateMinutes.ShouldBe(15);
    }

    [Fact]
    public void CheckOut_WithoutVerifiedId_ShouldThrowIdNotVerified()
    {
        var record = AttendanceRecord.CreateCheckIn(
            _childId, _classroomId, _date, _checkInAt, _guardianId, _userId, "Staff Name");
        var checkOutAt = _checkInAt.AddHours(8);

        var ex = Should.Throw<DomainRuleException>(() =>
            record.CheckOut(checkOutAt, _guardianId, _userId, "Staff Name", idVerified: false, lateMinutes: 0));

        ex.Code.ShouldBe("ID_NOT_VERIFIED");
    }

    [Fact]
    public void CheckOut_WhenAlreadyCheckedOut_ShouldThrowAlreadyCheckedOut()
    {
        var record = AttendanceRecord.CreateCheckIn(
            _childId, _classroomId, _date, _checkInAt, _guardianId, _userId, "Staff Name");
        record.CheckOut(_checkInAt.AddHours(4), _guardianId, _userId, "Staff Name", idVerified: true, lateMinutes: 0);

        var ex = Should.Throw<DomainRuleException>(() =>
            record.CheckOut(_checkInAt.AddHours(5), _guardianId, _userId, "Staff Name", idVerified: true, lateMinutes: 0));

        ex.Code.ShouldBe("ALREADY_CHECKED_OUT");
    }

    [Fact]
    public void CheckOut_BeforeCheckIn_ShouldThrowValidationFailed()
    {
        var record = AttendanceRecord.CreateCheckIn(
            _childId, _classroomId, _date, _checkInAt, _guardianId, _userId, "Staff Name");
        var checkOutAt = _checkInAt.AddHours(-1);

        var ex = Should.Throw<DomainRuleException>(() =>
            record.CheckOut(checkOutAt, _guardianId, _userId, "Staff Name", idVerified: true, lateMinutes: 0));

        ex.Code.ShouldBe("VALIDATION_FAILED");
    }

    [Fact]
    public void CreateAbsent_ShouldInitializeAbsentRecord()
    {
        var record = AttendanceRecord.CreateAbsent(
            _childId, _classroomId, _date, "Sick with flu", _userId, "Staff Name");

        record.Status.ShouldBe(AttendanceStatus.Absent);
        record.CheckInAt.ShouldBeNull();
        record.CheckOutAt.ShouldBeNull();
        record.AbsenceReason.ShouldBe("Sick with flu");
    }
}

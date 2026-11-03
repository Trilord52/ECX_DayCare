using Daycare.Application.Abstractions;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Attendance.RecordAbsence;
using Daycare.Application.Tests.Helpers;
using Daycare.Domain.Common;
using Daycare.Domain.Entities;
using Daycare.Domain.Enums;
using Daycare.Domain.Exceptions;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Daycare.Application.Tests.Attendance;

public class RecordAbsenceUseCaseTests
{
    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly IAccessGuard _accessGuard = Substitute.For<IAccessGuard>();
    private readonly RecordAbsenceValidator _validator;

    private readonly Guid _classroomId = Guid.NewGuid();
    private readonly Classroom _classroom;
    private readonly DateOnly _today = new(2026, 4, 1);

    public RecordAbsenceUseCaseTests()
    {
        _classroom = new Classroom("Preschool", null, 36, 72, 20, 10, isActive: true);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(_classroom, _classroomId);

        _dateTimeProvider.LocalToday.Returns(_today);
        _currentUser.UserId.Returns(Guid.NewGuid());
        _currentUser.Email.Returns("staff@daycare.local");
        _validator = new RecordAbsenceValidator(_dateTimeProvider);
    }

    private Child CreateChild(bool hasClassroom = true, ChildStatus status = ChildStatus.Active)
    {
        var childId = Guid.NewGuid();
        var child = new Child("Natnael", "Tadesse", new DateOnly(2023, 5, 1), Gender.Male, new DateOnly(2026, 1, 1), hasClassroom ? _classroomId : null);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(child, childId);

        if (status == ChildStatus.Withdrawn)
        {
            child.Withdraw();
        }

        if (hasClassroom)
        {
            typeof(Child).GetProperty(nameof(Child.Classroom))!.SetValue(child, _classroom);
        }

        return child;
    }

    [Fact]
    public async Task RecordAbsence_WhenFutureDate_ShouldThrowValidationFailed()
    {
        var child = CreateChild();
        var useCase = new RecordAbsenceUseCase(_dbContext, _currentUser, _accessGuard, _validator);
        var request = new RecordAbsenceRequest(child.Id, _today.AddDays(1), "Doctor appointment");

        var ex = await Should.ThrowAsync<ValidationFailedException>(() => useCase.ExecuteAsync(request));
        ex.Code.ShouldBe("VALIDATION_FAILED");
        ex.Errors.ShouldContainKey("date");
    }

    [Fact]
    public async Task RecordAbsence_WhenMoreThan30DaysPast_ShouldThrowValidationFailed()
    {
        var child = CreateChild();
        var useCase = new RecordAbsenceUseCase(_dbContext, _currentUser, _accessGuard, _validator);
        var request = new RecordAbsenceRequest(child.Id, _today.AddDays(-31), "Was sick");

        var ex = await Should.ThrowAsync<ValidationFailedException>(() => useCase.ExecuteAsync(request));
        ex.Code.ShouldBe("VALIDATION_FAILED");
        ex.Errors.ShouldContainKey("date");
    }

    [Fact]
    public async Task RecordAbsence_WhenChildInactive_ShouldThrowChildInactive()
    {
        var child = CreateChild(status: ChildStatus.Withdrawn);

        var childSet = MockDbSetHelper.CreateMockDbSet([child]);
        var attendanceSet = MockDbSetHelper.CreateMockDbSet<AttendanceRecord>([]);
        _dbContext.Children.Returns(childSet);
        _dbContext.AttendanceRecords.Returns(attendanceSet);

        var useCase = new RecordAbsenceUseCase(_dbContext, _currentUser, _accessGuard, _validator);
        var request = new RecordAbsenceRequest(child.Id, _today, "Sick");

        var ex = await Should.ThrowAsync<DomainRuleException>(() => useCase.ExecuteAsync(request));
        ex.Code.ShouldBe("CHILD_INACTIVE");
    }

    [Fact]
    public async Task RecordAbsence_WhenAlreadyHasRecordForDate_ShouldThrowAlreadyCheckedIn()
    {
        var child = CreateChild();
        var existingRecord = AttendanceRecord.CreateAbsent(child.Id, _classroomId, _today, "Already absent", Guid.NewGuid(), "Staff");

        var childSet = MockDbSetHelper.CreateMockDbSet([child]);
        var attendanceSet = MockDbSetHelper.CreateMockDbSet([existingRecord]);
        _dbContext.Children.Returns(childSet);
        _dbContext.AttendanceRecords.Returns(attendanceSet);

        var useCase = new RecordAbsenceUseCase(_dbContext, _currentUser, _accessGuard, _validator);
        var request = new RecordAbsenceRequest(child.Id, _today, "Sick");

        var ex = await Should.ThrowAsync<DomainRuleException>(() => useCase.ExecuteAsync(request));
        ex.Code.ShouldBe("ALREADY_CHECKED_IN");
    }

    [Fact]
    public async Task RecordAbsence_WhenValid_ShouldCreateAbsentRecord()
    {
        var child = CreateChild();

        var childSet = MockDbSetHelper.CreateMockDbSet([child]);
        var attendanceSet = MockDbSetHelper.CreateMockDbSet<AttendanceRecord>([]);
        _dbContext.Children.Returns(childSet);
        _dbContext.AttendanceRecords.Returns(attendanceSet);

        var useCase = new RecordAbsenceUseCase(_dbContext, _currentUser, _accessGuard, _validator);
        var request = new RecordAbsenceRequest(child.Id, _today, "Family trip");

        var result = await useCase.ExecuteAsync(request);

        result.Status.ShouldBe(AttendanceStatus.Absent);
        result.AbsenceReason.ShouldBe("Family trip");
        result.ChildId.ShouldBe(child.Id);
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

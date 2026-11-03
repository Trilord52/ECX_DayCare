using Daycare.Application.Abstractions;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Attendance.GetRoster;
using Daycare.Application.Tests.Helpers;
using Daycare.Domain.Common;
using Daycare.Domain.Entities;
using Daycare.Domain.Enums;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Daycare.Application.Tests.Attendance;

public class GetRosterUseCaseTests
{
    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    private readonly Guid _classroomId = Guid.NewGuid();
    private readonly Classroom _classroom;
    private readonly DateOnly _today = new(2026, 4, 1);

    public GetRosterUseCaseTests()
    {
        _classroom = new Classroom("Toddlers", null, 12, 36, 12, 5, isActive: true);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(_classroom, _classroomId);

        _dateTimeProvider.LocalToday.Returns(_today);
        _currentUser.Role.Returns("Admin");
    }

    [Fact]
    public async Task GetRoster_WhenClassroomNotFound_ShouldThrowNotFound()
    {
        var classroomSet = MockDbSetHelper.CreateMockDbSet<Classroom>([]);
        _dbContext.Classrooms.Returns(classroomSet);

        var useCase = new GetRosterUseCase(_dbContext, _currentUser, _dateTimeProvider);
        var request = new GetRosterRequest(Guid.NewGuid());

        await Should.ThrowAsync<NotFoundException>(() => useCase.ExecuteAsync(request));
    }

    [Fact]
    public async Task GetRoster_WhenStaffNotAssigned_ShouldThrowForbidden()
    {
        var staffId = Guid.NewGuid();
        _currentUser.Role.Returns("Staff");
        _currentUser.StaffId.Returns(staffId);

        var classroomSet = MockDbSetHelper.CreateMockDbSet([_classroom]);
        var staffClassroomSet = MockDbSetHelper.CreateMockDbSet<StaffClassroom>([]);
        _dbContext.Classrooms.Returns(classroomSet);
        _dbContext.StaffClassrooms.Returns(staffClassroomSet);

        var useCase = new GetRosterUseCase(_dbContext, _currentUser, _dateTimeProvider);
        var request = new GetRosterRequest(_classroomId);

        await Should.ThrowAsync<ForbiddenException>(() => useCase.ExecuteAsync(request));
    }

    [Fact]
    public async Task GetRoster_ShouldMapChildStatusesCorrectly()
    {
        var child1 = new Child("Abel", "Bekele", new DateOnly(2024, 1, 1), Gender.Male, new DateOnly(2026, 1, 1), _classroomId);
        var child1Id = Guid.NewGuid();
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(child1, child1Id);

        var child2 = new Child("Sara", "Haile", new DateOnly(2024, 2, 1), Gender.Female, new DateOnly(2026, 1, 1), _classroomId);
        var child2Id = Guid.NewGuid();
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(child2, child2Id);

        var child3 = new Child("Natnael", "Tadesse", new DateOnly(2024, 3, 1), Gender.Male, new DateOnly(2026, 1, 1), _classroomId);
        var child3Id = Guid.NewGuid();
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(child3, child3Id);

        var child4 = new Child("Dawit", "Alemu", new DateOnly(2024, 4, 1), Gender.Male, new DateOnly(2026, 1, 1), _classroomId);
        var child4Id = Guid.NewGuid();
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(child4, child4Id);

        // child1: Present
        var record1 = AttendanceRecord.CreateCheckIn(child1Id, _classroomId, _today, DateTime.UtcNow, null, Guid.NewGuid(), "Staff");
        // child2: CheckedOut
        var record2 = AttendanceRecord.CreateCheckIn(child2Id, _classroomId, _today, DateTime.UtcNow, null, Guid.NewGuid(), "Staff");
        record2.CheckOut(DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid(), "Staff", true, 0);
        // child3: Absent
        var record3 = AttendanceRecord.CreateAbsent(child3Id, _classroomId, _today, "Sick", Guid.NewGuid(), "Staff");
        // child4: No record (NotArrived)

        var classroomSet = MockDbSetHelper.CreateMockDbSet([_classroom]);
        var childrenSet = MockDbSetHelper.CreateMockDbSet([child1, child2, child3, child4]);
        var attendanceSet = MockDbSetHelper.CreateMockDbSet([record1, record2, record3]);
        _dbContext.Classrooms.Returns(classroomSet);
        _dbContext.Children.Returns(childrenSet);
        _dbContext.AttendanceRecords.Returns(attendanceSet);

        var useCase = new GetRosterUseCase(_dbContext, _currentUser, _dateTimeProvider);
        var request = new GetRosterRequest(_classroomId);

        var roster = await useCase.ExecuteAsync(request);

        roster.Count.ShouldBe(4);
        roster.First(r => r.ChildId == child1Id).Status.ShouldBe("Present");
        roster.First(r => r.ChildId == child2Id).Status.ShouldBe("CheckedOut");
        roster.First(r => r.ChildId == child3Id).Status.ShouldBe("Absent");
        roster.First(r => r.ChildId == child4Id).Status.ShouldBe("NotArrived");
    }
}

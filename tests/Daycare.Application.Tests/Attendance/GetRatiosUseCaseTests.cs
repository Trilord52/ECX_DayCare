using Daycare.Application.Abstractions;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Attendance.GetRatios;
using Daycare.Application.Tests.Helpers;
using Daycare.Domain.Common;
using Daycare.Domain.Entities;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Daycare.Application.Tests.Attendance;

public class GetRatiosUseCaseTests
{
    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    private readonly DateOnly _today = new(2026, 4, 1);

    public GetRatiosUseCaseTests()
    {
        _dateTimeProvider.LocalToday.Returns(_today);
        _currentUser.Role.Returns("Admin");
    }

    [Fact]
    public async Task GetRatios_WhenParentRequests_ShouldThrowForbidden()
    {
        _currentUser.Role.Returns("Parent");

        var useCase = new GetRatiosUseCase(_dbContext, _currentUser, _dateTimeProvider);

        await Should.ThrowAsync<ForbiddenException>(() => useCase.ExecuteAsync(new GetRatiosRequest()));
    }

    [Fact]
    public async Task GetRatios_ShouldComputeRatioAndOverLimitFlag()
    {
        var classroomId = Guid.NewGuid();
        // maxChildrenPerStaff: 4
        var classroom = new Classroom("Infants", null, 3, 12, 10, 4, isActive: true);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(classroom, classroomId);

        // 5 children present
        var records = Enumerable.Range(1, 5).Select(_ =>
        {
            var childId = Guid.NewGuid();
            var r = AttendanceRecord.CreateCheckIn(childId, classroomId, _today, DateTime.UtcNow, null, Guid.NewGuid(), "Staff");
            return r;
        }).ToList();

        // 1 active staff assigned (capacity allowed is 1 * 4 = 4 -> 5 present is over limit!)
        var staffId = Guid.NewGuid();
        var staff = new Staff("Helen", "Mekonnen", "+251911000003", "helen@test.local", "Lead Teacher", new DateOnly(2025, 1, 1), isActive: true);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(staff, staffId);

        var staffClassroom = new StaffClassroom(staffId, classroomId, DateTime.UtcNow);
        typeof(StaffClassroom).GetProperty(nameof(StaffClassroom.Staff))!.SetValue(staffClassroom, staff);

        var classroomSet = MockDbSetHelper.CreateMockDbSet([classroom]);
        var attendanceSet = MockDbSetHelper.CreateMockDbSet(records);
        var staffClassroomSet = MockDbSetHelper.CreateMockDbSet([staffClassroom]);
        _dbContext.Classrooms.Returns(classroomSet);
        _dbContext.AttendanceRecords.Returns(attendanceSet);
        _dbContext.StaffClassrooms.Returns(staffClassroomSet);

        var useCase = new GetRatiosUseCase(_dbContext, _currentUser, _dateTimeProvider);
        var result = await useCase.ExecuteAsync(new GetRatiosRequest());

        result.Count.ShouldBe(1);
        var ratio = result[0];
        ratio.ClassroomId.ShouldBe(classroomId);
        ratio.PresentCount.ShouldBe(5);
        ratio.StaffCount.ShouldBe(1);
        ratio.MaxChildrenPerStaff.ShouldBe(4);
        ratio.AllowedCount.ShouldBe(4);
        ratio.IsOverLimit.ShouldBeTrue();
    }

    [Fact]
    public async Task GetRatios_WhenZeroStaffAndChildrenPresent_ShouldBeOverRatio()
    {
        var classroomId = Guid.NewGuid();
        var classroom = new Classroom("Infants", null, 3, 12, 10, 4, isActive: true);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(classroom, classroomId);

        var record = AttendanceRecord.CreateCheckIn(Guid.NewGuid(), classroomId, _today, DateTime.UtcNow, null, Guid.NewGuid(), "Staff");

        var classroomSet = MockDbSetHelper.CreateMockDbSet([classroom]);
        var attendanceSet = MockDbSetHelper.CreateMockDbSet([record]);
        var staffClassroomSet = MockDbSetHelper.CreateMockDbSet<StaffClassroom>([]);
        _dbContext.Classrooms.Returns(classroomSet);
        _dbContext.AttendanceRecords.Returns(attendanceSet);
        _dbContext.StaffClassrooms.Returns(staffClassroomSet);

        var useCase = new GetRatiosUseCase(_dbContext, _currentUser, _dateTimeProvider);
        var result = await useCase.ExecuteAsync(new GetRatiosRequest());

        result.Count.ShouldBe(1);
        result[0].StaffCount.ShouldBe(0);
        result[0].PresentCount.ShouldBe(1);
        result[0].IsOverLimit.ShouldBeTrue();
    }

    [Fact]
    public async Task GetRatios_WhenZeroStaffAndNoChildrenPresent_ShouldNotBeOverRatio()
    {
        var classroomId = Guid.NewGuid();
        var classroom = new Classroom("Infants", null, 3, 12, 10, 4, isActive: true);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(classroom, classroomId);

        var classroomSet = MockDbSetHelper.CreateMockDbSet([classroom]);
        var attendanceSet = MockDbSetHelper.CreateMockDbSet<AttendanceRecord>([]);
        var staffClassroomSet = MockDbSetHelper.CreateMockDbSet<StaffClassroom>([]);
        _dbContext.Classrooms.Returns(classroomSet);
        _dbContext.AttendanceRecords.Returns(attendanceSet);
        _dbContext.StaffClassrooms.Returns(staffClassroomSet);

        var useCase = new GetRatiosUseCase(_dbContext, _currentUser, _dateTimeProvider);
        var result = await useCase.ExecuteAsync(new GetRatiosRequest());

        result.Count.ShouldBe(1);
        result[0].StaffCount.ShouldBe(0);
        result[0].PresentCount.ShouldBe(0);
        result[0].IsOverLimit.ShouldBeFalse();
    }

    [Fact]
    public async Task GetRatios_AtExactCapacityBoundary_ShouldNotBeOverRatio()
    {
        var classroomId = Guid.NewGuid();
        // maxChildrenPerStaff: 4
        var classroom = new Classroom("Toddlers", null, 12, 36, 12, 4, isActive: true);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(classroom, classroomId);

        // 2 staff -> capacity = 2 * 4 = 8
        var staff1Id = Guid.NewGuid();
        var staff1 = new Staff("Staff1", "One", "+251911000001", "s1@test.local", "Teacher", new DateOnly(2025, 1, 1), isActive: true);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(staff1, staff1Id);
        var sc1 = new StaffClassroom(staff1Id, classroomId, DateTime.UtcNow);
        typeof(StaffClassroom).GetProperty(nameof(StaffClassroom.Staff))!.SetValue(sc1, staff1);

        var staff2Id = Guid.NewGuid();
        var staff2 = new Staff("Staff2", "Two", "+251911000002", "s2@test.local", "Teacher", new DateOnly(2025, 1, 1), isActive: true);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(staff2, staff2Id);
        var sc2 = new StaffClassroom(staff2Id, classroomId, DateTime.UtcNow);
        typeof(StaffClassroom).GetProperty(nameof(StaffClassroom.Staff))!.SetValue(sc2, staff2);

        // Exactly 8 present children (staff * max = 8)
        var records = Enumerable.Range(1, 8).Select(_ =>
            AttendanceRecord.CreateCheckIn(Guid.NewGuid(), classroomId, _today, DateTime.UtcNow, null, Guid.NewGuid(), "Staff")
        ).ToList();

        var classroomSet = MockDbSetHelper.CreateMockDbSet([classroom]);
        var attendanceSet = MockDbSetHelper.CreateMockDbSet(records);
        var staffClassroomSet = MockDbSetHelper.CreateMockDbSet([sc1, sc2]);
        _dbContext.Classrooms.Returns(classroomSet);
        _dbContext.AttendanceRecords.Returns(attendanceSet);
        _dbContext.StaffClassrooms.Returns(staffClassroomSet);

        var useCase = new GetRatiosUseCase(_dbContext, _currentUser, _dateTimeProvider);
        var result = await useCase.ExecuteAsync(new GetRatiosRequest());

        result.Count.ShouldBe(1);
        result[0].StaffCount.ShouldBe(2);
        result[0].PresentCount.ShouldBe(8);
        result[0].AllowedCount.ShouldBe(8);
        result[0].IsOverLimit.ShouldBeFalse();
    }

    [Fact]
    public async Task GetRatios_OneAboveCapacityBoundary_ShouldBeOverRatio()
    {
        var classroomId = Guid.NewGuid();
        // maxChildrenPerStaff: 4
        var classroom = new Classroom("Toddlers", null, 12, 36, 12, 4, isActive: true);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(classroom, classroomId);

        // 2 staff -> capacity = 2 * 4 = 8
        var staff1Id = Guid.NewGuid();
        var staff1 = new Staff("Staff1", "One", "+251911000001", "s1@test.local", "Teacher", new DateOnly(2025, 1, 1), isActive: true);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(staff1, staff1Id);
        var sc1 = new StaffClassroom(staff1Id, classroomId, DateTime.UtcNow);
        typeof(StaffClassroom).GetProperty(nameof(StaffClassroom.Staff))!.SetValue(sc1, staff1);

        var staff2Id = Guid.NewGuid();
        var staff2 = new Staff("Staff2", "Two", "+251911000002", "s2@test.local", "Teacher", new DateOnly(2025, 1, 1), isActive: true);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(staff2, staff2Id);
        var sc2 = new StaffClassroom(staff2Id, classroomId, DateTime.UtcNow);
        typeof(StaffClassroom).GetProperty(nameof(StaffClassroom.Staff))!.SetValue(sc2, staff2);

        // 9 present children (staff * max + 1 = 9)
        var records = Enumerable.Range(1, 9).Select(_ =>
            AttendanceRecord.CreateCheckIn(Guid.NewGuid(), classroomId, _today, DateTime.UtcNow, null, Guid.NewGuid(), "Staff")
        ).ToList();

        var classroomSet = MockDbSetHelper.CreateMockDbSet([classroom]);
        var attendanceSet = MockDbSetHelper.CreateMockDbSet(records);
        var staffClassroomSet = MockDbSetHelper.CreateMockDbSet([sc1, sc2]);
        _dbContext.Classrooms.Returns(classroomSet);
        _dbContext.AttendanceRecords.Returns(attendanceSet);
        _dbContext.StaffClassrooms.Returns(staffClassroomSet);

        var useCase = new GetRatiosUseCase(_dbContext, _currentUser, _dateTimeProvider);
        var result = await useCase.ExecuteAsync(new GetRatiosRequest());

        result.Count.ShouldBe(1);
        result[0].StaffCount.ShouldBe(2);
        result[0].PresentCount.ShouldBe(9);
        result[0].AllowedCount.ShouldBe(8);
        result[0].IsOverLimit.ShouldBeTrue();
    }
}

using Daycare.Application.Abstractions;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Attendance.CheckIn;
using Daycare.Application.Tests.Helpers;
using Daycare.Domain.Common;
using Daycare.Domain.Entities;
using Daycare.Domain.Enums;
using Daycare.Domain.Exceptions;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Daycare.Application.Tests.Attendance;

public class CheckInUseCaseTests
{
    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly IAccessGuard _accessGuard = Substitute.For<IAccessGuard>();
    private readonly CheckInValidator _validator = new();

    private readonly Guid _classroomId = Guid.NewGuid();
    private readonly Classroom _classroom;
    private readonly DateOnly _today = new(2026, 4, 1);
    private readonly DateTime _nowUtc = new(2026, 4, 1, 6, 30, 0, DateTimeKind.Utc);

    public CheckInUseCaseTests()
    {
        _classroom = new Classroom("Toddlers", null, 12, 36, 12, 5, isActive: true);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(_classroom, _classroomId);

        _dateTimeProvider.LocalToday.Returns(_today);
        _dateTimeProvider.UtcNow.Returns(_nowUtc);
        _currentUser.UserId.Returns(Guid.NewGuid());
        _currentUser.Email.Returns("staff@daycare.local");
        _currentUser.Role.Returns("Staff");
    }

    private (Child child, Guardian guardian, ChildGuardian childGuardian) CreateValidChild(
        bool canPickUp = true,
        bool isEmergency = true,
        ChildStatus status = ChildStatus.Active,
        bool hasClassroom = true)
    {
        var childId = Guid.NewGuid();
        var guardianId = Guid.NewGuid();

        var child = new Child("Abel", "Bekele", new DateOnly(2024, 1, 1), Gender.Male, new DateOnly(2026, 1, 1), hasClassroom ? _classroomId : null);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(child, childId);

        if (status == ChildStatus.Withdrawn)
        {
            child.Withdraw();
        }

        if (hasClassroom)
        {
            typeof(Child).GetProperty(nameof(Child.Classroom))!.SetValue(child, _classroom);
        }

        var guardian = new Guardian("Almaz", "Kebede", "+251911000001", "almaz@test.local");
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(guardian, guardianId);

        var cg = new ChildGuardian(childId, guardianId, Relationship.Mother, isPrimaryContact: true, canPickUp: canPickUp, isPayer: true, isEmergencyContact: isEmergency);
        typeof(ChildGuardian).GetProperty(nameof(ChildGuardian.Guardian))!.SetValue(cg, guardian);

        child.Guardians.Add(cg);

        return (child, guardian, cg);
    }

    [Fact]
    public async Task CheckIn_WhenChildInactive_ShouldThrowChildInactive()
    {
        var (child, _, _) = CreateValidChild(status: ChildStatus.Withdrawn);

        var childSet = MockDbSetHelper.CreateMockDbSet([child]);
        var attendanceSet = MockDbSetHelper.CreateMockDbSet<AttendanceRecord>([]);
        _dbContext.Children.Returns(childSet);
        _dbContext.AttendanceRecords.Returns(attendanceSet);

        var useCase = new CheckInUseCase(_dbContext, _currentUser, _dateTimeProvider, _accessGuard, _validator);
        var request = new CheckInRequest(child.Id);

        var ex = await Should.ThrowAsync<DomainRuleException>(() => useCase.ExecuteAsync(request));
        ex.Code.ShouldBe("CHILD_INACTIVE");
    }

    [Fact]
    public async Task CheckIn_WhenNoClassroom_ShouldThrowNoClassroom()
    {
        var (child, _, _) = CreateValidChild(hasClassroom: false);

        var childSet = MockDbSetHelper.CreateMockDbSet([child]);
        var attendanceSet = MockDbSetHelper.CreateMockDbSet<AttendanceRecord>([]);
        _dbContext.Children.Returns(childSet);
        _dbContext.AttendanceRecords.Returns(attendanceSet);

        var useCase = new CheckInUseCase(_dbContext, _currentUser, _dateTimeProvider, _accessGuard, _validator);
        var request = new CheckInRequest(child.Id);

        var ex = await Should.ThrowAsync<DomainRuleException>(() => useCase.ExecuteAsync(request));
        ex.Code.ShouldBe("NO_CLASSROOM");
    }

    [Fact]
    public async Task CheckIn_WhenNoPickupGuardian_ShouldThrowNoPickupGuardian()
    {
        var (child, _, _) = CreateValidChild(canPickUp: false, isEmergency: true);

        var childSet = MockDbSetHelper.CreateMockDbSet([child]);
        var attendanceSet = MockDbSetHelper.CreateMockDbSet<AttendanceRecord>([]);
        _dbContext.Children.Returns(childSet);
        _dbContext.AttendanceRecords.Returns(attendanceSet);

        var useCase = new CheckInUseCase(_dbContext, _currentUser, _dateTimeProvider, _accessGuard, _validator);
        var request = new CheckInRequest(child.Id);

        var ex = await Should.ThrowAsync<DomainRuleException>(() => useCase.ExecuteAsync(request));
        ex.Code.ShouldBe("NO_PICKUP_GUARDIAN");
    }

    [Fact]
    public async Task CheckIn_WhenNoEmergencyContact_ShouldThrowNoEmergencyContact()
    {
        var (child, _, _) = CreateValidChild(canPickUp: true, isEmergency: false);

        var childSet = MockDbSetHelper.CreateMockDbSet([child]);
        var attendanceSet = MockDbSetHelper.CreateMockDbSet<AttendanceRecord>([]);
        _dbContext.Children.Returns(childSet);
        _dbContext.AttendanceRecords.Returns(attendanceSet);

        var useCase = new CheckInUseCase(_dbContext, _currentUser, _dateTimeProvider, _accessGuard, _validator);
        var request = new CheckInRequest(child.Id);

        var ex = await Should.ThrowAsync<DomainRuleException>(() => useCase.ExecuteAsync(request));
        ex.Code.ShouldBe("NO_EMERGENCY_CONTACT");
    }

    [Fact]
    public async Task CheckIn_WhenAlreadyCheckedInToday_ShouldThrowAlreadyCheckedIn()
    {
        var (child, _, _) = CreateValidChild();

        var existingRecord = AttendanceRecord.CreateCheckIn(
            child.Id, _classroomId, _today, _nowUtc.AddHours(-1), null, Guid.NewGuid(), "Staff");

        var childSet = MockDbSetHelper.CreateMockDbSet([child]);
        var attendanceSet = MockDbSetHelper.CreateMockDbSet([existingRecord]);
        _dbContext.Children.Returns(childSet);
        _dbContext.AttendanceRecords.Returns(attendanceSet);

        var useCase = new CheckInUseCase(_dbContext, _currentUser, _dateTimeProvider, _accessGuard, _validator);
        var request = new CheckInRequest(child.Id);

        var ex = await Should.ThrowAsync<DomainRuleException>(() => useCase.ExecuteAsync(request));
        ex.Code.ShouldBe("ALREADY_CHECKED_IN");
    }

    [Fact]
    public async Task CheckIn_WhenDropoffGuardianNotLinked_ShouldThrowDropoffNotAuthorized()
    {
        var (child, _, _) = CreateValidChild();
        var unlinkedGuardianId = Guid.NewGuid();

        var childSet = MockDbSetHelper.CreateMockDbSet([child]);
        var attendanceSet = MockDbSetHelper.CreateMockDbSet<AttendanceRecord>([]);
        _dbContext.Children.Returns(childSet);
        _dbContext.AttendanceRecords.Returns(attendanceSet);

        var useCase = new CheckInUseCase(_dbContext, _currentUser, _dateTimeProvider, _accessGuard, _validator);
        var request = new CheckInRequest(child.Id, DroppedOffByGuardianId: unlinkedGuardianId);

        var ex = await Should.ThrowAsync<DomainRuleException>(() => useCase.ExecuteAsync(request));
        ex.Code.ShouldBe("DROPOFF_NOT_AUTHORIZED");
    }

    [Fact]
    public async Task CheckIn_WhenValid_ShouldCreateRecordAndComputeRatio()
    {
        var (child, guardian, _) = CreateValidChild();

        var childSet = MockDbSetHelper.CreateMockDbSet([child]);
        var attendanceSet = MockDbSetHelper.CreateMockDbSet<AttendanceRecord>([]);
        var staffClassroomSet = MockDbSetHelper.CreateMockDbSet<StaffClassroom>([]);
        _dbContext.Children.Returns(childSet);
        _dbContext.AttendanceRecords.Returns(attendanceSet);
        _dbContext.StaffClassrooms.Returns(staffClassroomSet);

        var useCase = new CheckInUseCase(_dbContext, _currentUser, _dateTimeProvider, _accessGuard, _validator);
        var request = new CheckInRequest(child.Id, DroppedOffByGuardianId: guardian.Id, Notes: "Morning check-in");

        _currentUser.FullName.Returns("Tigist Alemu");

        var response = await useCase.ExecuteAsync(request);

        response.ShouldNotBeNull();
        response.Record.ChildId.ShouldBe(child.Id);
        response.Record.Status.ShouldBe(AttendanceStatus.Present);
        response.Record.CheckInActor.ShouldBe("Tigist Alemu");
        response.Record.DroppedOffByGuardianId.ShouldBe(guardian.Id);
        response.Record.Notes.ShouldBe("Morning check-in");
        response.RatioWarning.ShouldBeTrue();
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckIn_WhenCurrentUserUserIdMissing_ShouldThrowUnauthorized()
    {
        var (child, _, _) = CreateValidChild();
        _currentUser.UserId.Returns((Guid?)null);

        var childSet = MockDbSetHelper.CreateMockDbSet([child]);
        var attendanceSet = MockDbSetHelper.CreateMockDbSet<AttendanceRecord>([]);
        _dbContext.Children.Returns(childSet);
        _dbContext.AttendanceRecords.Returns(attendanceSet);

        var useCase = new CheckInUseCase(_dbContext, _currentUser, _dateTimeProvider, _accessGuard, _validator);
        var request = new CheckInRequest(child.Id);

        await Should.ThrowAsync<UnauthorizedException>(() => useCase.ExecuteAsync(request));
    }
}

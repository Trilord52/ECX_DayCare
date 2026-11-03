using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Attendance.CheckOut;
using Daycare.Application.Tests.Helpers;
using Daycare.Domain.Common;
using Daycare.Domain.Entities;
using Daycare.Domain.Enums;
using Daycare.Domain.Exceptions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Daycare.Application.Tests.Attendance;

public class CheckOutUseCaseTests
{
    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly IAccessGuard _accessGuard = Substitute.For<IAccessGuard>();
    private readonly IOptions<DaycareOptions> _options;
    private readonly CheckOutValidator _validator = new();

    private readonly Guid _classroomId = Guid.NewGuid();
    private readonly Classroom _classroom;
    private readonly DateOnly _today = new(2026, 4, 1);
    private readonly DateTime _nowUtc = new(2026, 4, 1, 14, 0, 0, DateTimeKind.Utc);

    public CheckOutUseCaseTests()
    {
        _classroom = new Classroom("Toddlers", null, 12, 36, 12, 5, isActive: true);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(_classroom, _classroomId);

        _options = Options.Create(new DaycareOptions
        {
            ClosingTime = "18:00",
            TimeZoneId = "Africa/Addis_Ababa"
        });

        _dateTimeProvider.LocalToday.Returns(_today);
        _dateTimeProvider.UtcNow.Returns(_nowUtc);
        _dateTimeProvider.ToLocal(Arg.Any<DateTime>()).Returns(new DateTime(2026, 4, 1, 17, 0, 0)); // Default before 18:00
        _currentUser.UserId.Returns(Guid.NewGuid());
        _currentUser.Email.Returns("staff@daycare.local");
        _currentUser.Role.Returns("Staff");
    }

    private (AttendanceRecord record, Child child, Guardian authorizedGuardian, Guardian unauthorizedGuardian) CreateActiveRecord()
    {
        var childId = Guid.NewGuid();
        var recordId = Guid.NewGuid();
        var authGuardianId = Guid.NewGuid();
        var unauthGuardianId = Guid.NewGuid();

        var child = new Child("Abel", "Bekele", new DateOnly(2024, 1, 1), Gender.Male, new DateOnly(2026, 1, 1), _classroomId);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(child, childId);

        var authGuardian = new Guardian("Almaz", "Kebede", "+251911000001", "almaz@test.local");
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(authGuardian, authGuardianId);

        var cgAuth = new ChildGuardian(childId, authGuardianId, Relationship.Mother, isPrimaryContact: true, canPickUp: true, isPayer: true, isEmergencyContact: true);
        typeof(ChildGuardian).GetProperty(nameof(ChildGuardian.Guardian))!.SetValue(cgAuth, authGuardian);
        child.Guardians.Add(cgAuth);

        var unauthGuardian = new Guardian("Yohannes", "Bekele", "+251911000002", "yohannes@test.local");
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(unauthGuardian, unauthGuardianId);

        var cgUnauth = new ChildGuardian(childId, unauthGuardianId, Relationship.Relative, isPrimaryContact: false, canPickUp: false, isPayer: false, isEmergencyContact: false);
        typeof(ChildGuardian).GetProperty(nameof(ChildGuardian.Guardian))!.SetValue(cgUnauth, unauthGuardian);
        child.Guardians.Add(cgUnauth);

        var record = AttendanceRecord.CreateCheckIn(childId, _classroomId, _today, _nowUtc.AddHours(-3), authGuardianId, Guid.NewGuid(), "Staff");
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(record, recordId);
        typeof(AttendanceRecord).GetProperty(nameof(AttendanceRecord.Child))!.SetValue(record, child);
        typeof(AttendanceRecord).GetProperty(nameof(AttendanceRecord.Classroom))!.SetValue(record, _classroom);

        return (record, child, authGuardian, unauthGuardian);
    }

    [Fact]
    public async Task CheckOut_WhenNotPresent_ShouldThrowNotCheckedIn()
    {
        var (record, _, authGuardian, _) = CreateActiveRecord();
        typeof(AttendanceRecord).GetProperty(nameof(AttendanceRecord.Status))!.SetValue(record, AttendanceStatus.Absent);

        var attendanceSet = MockDbSetHelper.CreateMockDbSet([record]);
        _dbContext.AttendanceRecords.Returns(attendanceSet);

        var useCase = new CheckOutUseCase(_dbContext, _currentUser, _dateTimeProvider, _accessGuard, _options, _validator);
        var request = new CheckOutRequest(record.Id, authGuardian.Id, IdVerified: true);

        var ex = await Should.ThrowAsync<DomainRuleException>(() => useCase.ExecuteAsync(request));
        ex.Code.ShouldBe("NOT_CHECKED_IN");
    }

    [Fact]
    public async Task CheckOut_WhenAlreadyCheckedOut_ShouldThrowAlreadyCheckedOut()
    {
        var (record, _, authGuardian, _) = CreateActiveRecord();
        record.CheckOut(_nowUtc.AddHours(-1), authGuardian.Id, Guid.NewGuid(), "Staff", idVerified: true, lateMinutes: 0);

        var attendanceSet = MockDbSetHelper.CreateMockDbSet([record]);
        _dbContext.AttendanceRecords.Returns(attendanceSet);

        var useCase = new CheckOutUseCase(_dbContext, _currentUser, _dateTimeProvider, _accessGuard, _options, _validator);
        var request = new CheckOutRequest(record.Id, authGuardian.Id, IdVerified: true);

        var ex = await Should.ThrowAsync<DomainRuleException>(() => useCase.ExecuteAsync(request));
        ex.Code.ShouldBe("ALREADY_CHECKED_OUT");
    }

    [Fact]
    public async Task CheckOut_WhenIdNotVerified_ShouldThrowIdNotVerified()
    {
        var (record, _, authGuardian, _) = CreateActiveRecord();

        var attendanceSet = MockDbSetHelper.CreateMockDbSet([record]);
        _dbContext.AttendanceRecords.Returns(attendanceSet);

        var useCase = new CheckOutUseCase(_dbContext, _currentUser, _dateTimeProvider, _accessGuard, _options, _validator);
        var request = new CheckOutRequest(record.Id, authGuardian.Id, IdVerified: false);

        var ex = await Should.ThrowAsync<DomainRuleException>(() => useCase.ExecuteAsync(request));
        ex.Code.ShouldBe("ID_NOT_VERIFIED");
    }

    [Fact]
    public async Task CheckOut_WhenGuardianNotAuthorizedForPickup_ShouldThrowPickupNotAuthorized()
    {
        var (record, _, _, unauthGuardian) = CreateActiveRecord();

        var attendanceSet = MockDbSetHelper.CreateMockDbSet([record]);
        _dbContext.AttendanceRecords.Returns(attendanceSet);

        var useCase = new CheckOutUseCase(_dbContext, _currentUser, _dateTimeProvider, _accessGuard, _options, _validator);
        var request = new CheckOutRequest(record.Id, unauthGuardian.Id, IdVerified: true);

        var ex = await Should.ThrowAsync<DomainRuleException>(() => useCase.ExecuteAsync(request));
        ex.Code.ShouldBe("PICKUP_NOT_AUTHORIZED");
    }

    [Fact]
    public async Task CheckOut_WhenOnTime_ShouldHaveZeroLateMinutes()
    {
        var (record, child, authGuardian, _) = CreateActiveRecord();
        _dateTimeProvider.ToLocal(Arg.Any<DateTime>()).Returns(new DateTime(2026, 4, 1, 16, 45, 0));

        var attendanceSet = MockDbSetHelper.CreateMockDbSet([record]);
        _dbContext.AttendanceRecords.Returns(attendanceSet);

        var useCase = new CheckOutUseCase(_dbContext, _currentUser, _dateTimeProvider, _accessGuard, _options, _validator);
        var request = new CheckOutRequest(record.Id, authGuardian.Id, IdVerified: true);

        _currentUser.FullName.Returns("Tigist Alemu");

        var result = await useCase.ExecuteAsync(request);

        result.Status.ShouldBe(AttendanceStatus.Present);
        result.CheckOutAt.ShouldNotBeNull();
        result.CheckOutActor.ShouldBe("Tigist Alemu");
        result.LateMinutes.ShouldBe(0);
        result.IdVerified.ShouldBe(true);
        result.PickedUpByGuardianId.ShouldBe(authGuardian.Id);
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckOut_WhenLateAfterClosingTime_ShouldCalculateLateMinutes()
    {
        var (record, child, authGuardian, _) = CreateActiveRecord();
        // 18:45 local time is 45 minutes past 18:00
        _dateTimeProvider.ToLocal(Arg.Any<DateTime>()).Returns(new DateTime(2026, 4, 1, 18, 45, 0));

        var attendanceSet = MockDbSetHelper.CreateMockDbSet([record]);
        _dbContext.AttendanceRecords.Returns(attendanceSet);

        var useCase = new CheckOutUseCase(_dbContext, _currentUser, _dateTimeProvider, _accessGuard, _options, _validator);
        var request = new CheckOutRequest(record.Id, authGuardian.Id, IdVerified: true);

        var result = await useCase.ExecuteAsync(request);

        result.Status.ShouldBe(AttendanceStatus.Present);
        result.CheckOutAt.ShouldNotBeNull();
        result.LateMinutes.ShouldBe(45);
        result.IdVerified.ShouldBe(true);
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckOut_WhenExactlyAtClosingTime_ShouldHaveZeroLateMinutes()
    {
        var (record, child, authGuardian, _) = CreateActiveRecord();
        // Exactly 18:00 local time
        _dateTimeProvider.ToLocal(Arg.Any<DateTime>()).Returns(new DateTime(2026, 4, 1, 18, 0, 0));

        var attendanceSet = MockDbSetHelper.CreateMockDbSet([record]);
        _dbContext.AttendanceRecords.Returns(attendanceSet);

        var useCase = new CheckOutUseCase(_dbContext, _currentUser, _dateTimeProvider, _accessGuard, _options, _validator);
        var request = new CheckOutRequest(record.Id, authGuardian.Id, IdVerified: true);

        var result = await useCase.ExecuteAsync(request);

        result.LateMinutes.ShouldBe(0);
    }

    [Fact]
    public async Task CheckOut_WhenGuardianNotLinkedToChild_ShouldThrowPickupNotAuthorized()
    {
        var (record, _, _, _) = CreateActiveRecord();
        var unlinkedGuardianId = Guid.NewGuid();

        var attendanceSet = MockDbSetHelper.CreateMockDbSet([record]);
        _dbContext.AttendanceRecords.Returns(attendanceSet);

        var useCase = new CheckOutUseCase(_dbContext, _currentUser, _dateTimeProvider, _accessGuard, _options, _validator);
        var request = new CheckOutRequest(record.Id, unlinkedGuardianId, IdVerified: true);

        var ex = await Should.ThrowAsync<DomainRuleException>(() => useCase.ExecuteAsync(request));
        ex.Code.ShouldBe("PICKUP_NOT_AUTHORIZED");
    }

    [Fact]
    public async Task CheckOut_WhenCurrentUserUserIdMissing_ShouldThrowUnauthorized()
    {
        var (record, _, authGuardian, _) = CreateActiveRecord();
        _currentUser.UserId.Returns((Guid?)null);

        var attendanceSet = MockDbSetHelper.CreateMockDbSet([record]);
        _dbContext.AttendanceRecords.Returns(attendanceSet);

        var useCase = new CheckOutUseCase(_dbContext, _currentUser, _dateTimeProvider, _accessGuard, _options, _validator);
        var request = new CheckOutRequest(record.Id, authGuardian.Id, IdVerified: true);

        await Should.ThrowAsync<UnauthorizedException>(() => useCase.ExecuteAsync(request));
    }
}

using Daycare.Application.Abstractions;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.DailyReports.AddDailyReportEntry;
using Daycare.Application.Features.DailyReports.DeleteDailyReportEntry;
using Daycare.Application.Features.DailyReports.GetDailyReportDetail;
using Daycare.Application.Features.DailyReports.GetDailyReportsSummary;
using Daycare.Application.Features.DailyReports.UpdateDailyReportEntry;
using Daycare.Application.Features.DailyReports.UpsertDailyReportHeader;
using Daycare.Application.Tests.Helpers;
using Daycare.Domain.Common;
using Daycare.Domain.Entities;
using Daycare.Domain.Enums;
using Daycare.Domain.Exceptions;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Daycare.Application.Tests.DailyReports;

public class DailyReportsUseCaseTests
{
    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly IAccessGuard _accessGuard = Substitute.For<IAccessGuard>();

    private readonly Guid _classroomId = Guid.NewGuid();
    private readonly Guid _childId = Guid.NewGuid();
    private readonly Classroom _classroom;
    private readonly Child _child;
    private readonly DateOnly _today = new(2026, 4, 1);
    private readonly DateTime _nowUtc = new(2026, 4, 1, 9, 0, 0, DateTimeKind.Utc);

    public DailyReportsUseCaseTests()
    {
        _classroom = new Classroom("Toddlers", null, 12, 36, 12, 5, isActive: true);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(_classroom, _classroomId);

        _child = new Child("Abel", "Bekele", new DateOnly(2024, 1, 1), Gender.Male, new DateOnly(2026, 1, 1), _classroomId);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(_child, _childId);
        typeof(Child).GetProperty(nameof(Child.Classroom))!.SetValue(_child, _classroom);

        _dateTimeProvider.LocalToday.Returns(_today);
        _dateTimeProvider.UtcNow.Returns(_nowUtc);
        _currentUser.UserId.Returns(Guid.NewGuid());
        _currentUser.FullName.Returns("Staff Member");
        _currentUser.Role.Returns("Staff");
    }

    [Fact]
    public async Task UpsertDailyReportHeader_WhenDateNotToday_ShouldThrowEntryLocked()
    {
        // Arrange
        var yesterday = _today.AddDays(-1);
        var validator = new UpsertDailyReportHeaderValidator();
        var useCase = new UpsertDailyReportHeaderUseCase(_dbContext, _dateTimeProvider, _accessGuard, validator);

        // Act & Assert
        var ex = await Should.ThrowAsync<DomainRuleException>(() =>
            useCase.ExecuteAsync(new UpsertDailyReportHeaderRequest(_childId, yesterday, Mood.Happy), CancellationToken.None));

        ex.Code.ShouldBe("ENTRY_LOCKED");
    }

    [Fact]
    public async Task UpsertDailyReportHeader_WhenChildNotCheckedInToday_ShouldThrowNotCheckedIn()
    {
        // Arrange
        var attendanceSet = MockDbSetHelper.CreateMockDbSet<AttendanceRecord>([]);
        _dbContext.AttendanceRecords.Returns(attendanceSet);

        var validator = new UpsertDailyReportHeaderValidator();
        var useCase = new UpsertDailyReportHeaderUseCase(_dbContext, _dateTimeProvider, _accessGuard, validator);

        // Act & Assert
        var ex = await Should.ThrowAsync<DomainRuleException>(() =>
            useCase.ExecuteAsync(new UpsertDailyReportHeaderRequest(_childId, _today, Mood.Happy), CancellationToken.None));

        ex.Code.ShouldBe("NOT_CHECKED_IN");
    }

    [Fact]
    public async Task UpsertDailyReportHeader_WhenChildCheckedIn_ShouldCreateOrUpdateHeader()
    {
        // Arrange
        var attendance = AttendanceRecord.CreateCheckIn(_childId, _classroomId, _today, _nowUtc, null, Guid.NewGuid(), "Staff");
        var attendanceSet = MockDbSetHelper.CreateMockDbSet([attendance]);
        _dbContext.AttendanceRecords.Returns(attendanceSet);

        var dailyReports = new List<DailyReport>();
        var reportSet = MockDbSetHelper.CreateMockDbSet(dailyReports);
        _dbContext.DailyReports.Returns(reportSet);

        var childrenSet = MockDbSetHelper.CreateMockDbSet([_child]);
        _dbContext.Children.Returns(childrenSet);

        var validator = new UpsertDailyReportHeaderValidator();
        var useCase = new UpsertDailyReportHeaderUseCase(_dbContext, _dateTimeProvider, _accessGuard, validator);

        // Act
        var result = await useCase.ExecuteAsync(
            new UpsertDailyReportHeaderRequest(_childId, _today, Mood.Happy, "Great day"), CancellationToken.None);

        // Assert
        result.Mood.ShouldBe(Mood.Happy);
        result.Notes.ShouldBe("Great day");
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddDailyReportEntry_WhenDateNotToday_ShouldThrowEntryLocked()
    {
        // Arrange
        var yesterday = _today.AddDays(-1);
        var validator = new AddDailyReportEntryValidator();
        var useCase = new AddDailyReportEntryUseCase(_dbContext, _currentUser, _dateTimeProvider, _accessGuard, validator);

        var request = new AddDailyReportEntryRequest(
            _childId, yesterday, EntryType.Meal, _nowUtc, MealType: MealType.Lunch);

        // Act & Assert
        var ex = await Should.ThrowAsync<DomainRuleException>(() =>
            useCase.ExecuteAsync(request, CancellationToken.None));

        ex.Code.ShouldBe("ENTRY_LOCKED");
    }

    [Fact]
    public async Task AddDailyReportEntry_WhenChildNotCheckedInToday_ShouldThrowNotCheckedIn()
    {
        // Arrange
        var attendanceSet = MockDbSetHelper.CreateMockDbSet<AttendanceRecord>([]);
        _dbContext.AttendanceRecords.Returns(attendanceSet);

        var validator = new AddDailyReportEntryValidator();
        var useCase = new AddDailyReportEntryUseCase(_dbContext, _currentUser, _dateTimeProvider, _accessGuard, validator);

        var request = new AddDailyReportEntryRequest(
            _childId, _today, EntryType.Meal, _nowUtc, MealType: MealType.Lunch);

        // Act & Assert
        var ex = await Should.ThrowAsync<DomainRuleException>(() =>
            useCase.ExecuteAsync(request, CancellationToken.None));

        ex.Code.ShouldBe("NOT_CHECKED_IN");
    }

    [Theory]
    [InlineData(EntryType.Meal, null, null, null, "Meal type is required for meals.")]
    [InlineData(EntryType.Diaper, null, null, null, "Diaper type is required for diaper entries.")]
    [InlineData(EntryType.Activity, null, null, null, "Description is required for activity, medication, and note entries.")]
    [InlineData(EntryType.Medication, null, null, null, "Description is required for activity, medication, and note entries.")]
    [InlineData(EntryType.Note, null, null, null, "Description is required for activity, medication, and note entries.")]
    public async Task AddDailyReportEntry_BR15_ValidationRules(
        EntryType entryType,
        MealType? mealType,
        DiaperType? diaperType,
        string? description,
        string expectedError)
    {
        // Arrange
        var validator = new AddDailyReportEntryValidator();
        var useCase = new AddDailyReportEntryUseCase(_dbContext, _currentUser, _dateTimeProvider, _accessGuard, validator);

        var request = new AddDailyReportEntryRequest(
            _childId, _today, entryType, _nowUtc,
            MealType: mealType, DiaperType: diaperType, Description: description);

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationFailedException>(() =>
            useCase.ExecuteAsync(request, CancellationToken.None));

        ex.Errors.Values.SelectMany(v => v).ShouldContain(expectedError);
    }

    [Fact]
    public async Task AddDailyReportEntry_WhenNapEndedAtBeforeOccurredAt_ShouldFailValidation()
    {
        // Arrange
        var validator = new AddDailyReportEntryValidator();
        var useCase = new AddDailyReportEntryUseCase(_dbContext, _currentUser, _dateTimeProvider, _accessGuard, validator);

        var request = new AddDailyReportEntryRequest(
            _childId, _today, EntryType.Nap, _nowUtc,
            EndedAt: _nowUtc.AddMinutes(-30));

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationFailedException>(() =>
            useCase.ExecuteAsync(request, CancellationToken.None));

        ex.Errors.Values.SelectMany(v => v).ShouldContain("Nap end time must be after start time.");
    }

    [Fact]
    public async Task UpdateDailyReportEntry_WhenEntryFromPastDay_ShouldThrowEntryLocked()
    {
        // Arrange
        var pastDate = _today.AddDays(-1);
        var report = new DailyReport(_childId, _classroomId, pastDate);
        var entry = new DailyReportEntry(
            report.Id, EntryType.Meal, _nowUtc.AddDays(-1), Guid.NewGuid(), "Staff", mealType: MealType.Lunch);
        typeof(DailyReportEntry).GetProperty(nameof(DailyReportEntry.DailyReport))!.SetValue(entry, report);

        var entrySet = MockDbSetHelper.CreateMockDbSet([entry]);
        _dbContext.DailyReportEntries.Returns(entrySet);

        var validator = new UpdateDailyReportEntryValidator();
        var useCase = new UpdateDailyReportEntryUseCase(_dbContext, _dateTimeProvider, _accessGuard, validator);

        var request = new UpdateDailyReportEntryRequest(entry.Id, _nowUtc, MealType: MealType.Lunch);

        // Act & Assert
        var ex = await Should.ThrowAsync<DomainRuleException>(() =>
            useCase.ExecuteAsync(request, CancellationToken.None));

        ex.Code.ShouldBe("ENTRY_LOCKED");
    }

    [Fact]
    public async Task DeleteDailyReportEntry_WhenEntryFromPastDay_ShouldThrowEntryLocked()
    {
        // Arrange
        var pastDate = _today.AddDays(-1);
        var report = new DailyReport(_childId, _classroomId, pastDate);
        var entry = new DailyReportEntry(
            report.Id, EntryType.Meal, _nowUtc.AddDays(-1), Guid.NewGuid(), "Staff", mealType: MealType.Lunch);
        typeof(DailyReportEntry).GetProperty(nameof(DailyReportEntry.DailyReport))!.SetValue(entry, report);

        var entrySet = MockDbSetHelper.CreateMockDbSet([entry]);
        _dbContext.DailyReportEntries.Returns(entrySet);

        var validator = new DeleteDailyReportEntryValidator();
        var useCase = new DeleteDailyReportEntryUseCase(_dbContext, _dateTimeProvider, _accessGuard, validator);

        // Act & Assert
        var ex = await Should.ThrowAsync<DomainRuleException>(() =>
            useCase.ExecuteAsync(new DeleteDailyReportEntryRequest(entry.Id), CancellationToken.None));

        ex.Code.ShouldBe("ENTRY_LOCKED");
    }

    [Fact]
    public async Task DeleteDailyReportEntry_WhenSameDay_ShouldSucceed()
    {
        // Arrange
        var report = new DailyReport(_childId, _classroomId, _today);
        var entry = new DailyReportEntry(
            report.Id, EntryType.Meal, _nowUtc, Guid.NewGuid(), "Staff", mealType: MealType.Lunch);
        typeof(DailyReportEntry).GetProperty(nameof(DailyReportEntry.DailyReport))!.SetValue(entry, report);

        var entrySet = MockDbSetHelper.CreateMockDbSet([entry]);
        _dbContext.DailyReportEntries.Returns(entrySet);

        var validator = new DeleteDailyReportEntryValidator();
        var useCase = new DeleteDailyReportEntryUseCase(_dbContext, _dateTimeProvider, _accessGuard, validator);

        // Act
        var result = await useCase.ExecuteAsync(new DeleteDailyReportEntryRequest(entry.Id), CancellationToken.None);

        // Assert
        result.ShouldBeTrue();
        await _dbContext.SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDailyReportDetail_WhenCallerUnauthorized_ShouldThrowForbidden()
    {
        // Arrange
        _accessGuard.EnsureCanViewChildAsync(_childId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new ForbiddenException("Parents may only view children linked to their guardian record.")));

        var validator = new GetDailyReportDetailValidator();
        var useCase = new GetDailyReportDetailUseCase(_dbContext, _accessGuard, validator);

        // Act & Assert
        await Should.ThrowAsync<ForbiddenException>(() =>
            useCase.ExecuteAsync(new GetDailyReportDetailRequest(_childId, _today), CancellationToken.None));
    }

    [Fact]
    public async Task AddDailyReportEntry_WhenCallerUnauthorized_ShouldThrowForbidden()
    {
        // Arrange
        _accessGuard.EnsureCanWriteChildDataAsync(_childId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new ForbiddenException("Staff may only modify records for children in assigned classrooms.")));

        var validator = new AddDailyReportEntryValidator();
        var useCase = new AddDailyReportEntryUseCase(_dbContext, _currentUser, _dateTimeProvider, _accessGuard, validator);

        var request = new AddDailyReportEntryRequest(
            _childId, _today, EntryType.Meal, _nowUtc, MealType: MealType.Lunch);

        // Act & Assert
        await Should.ThrowAsync<ForbiddenException>(() =>
            useCase.ExecuteAsync(request, CancellationToken.None));
    }
}

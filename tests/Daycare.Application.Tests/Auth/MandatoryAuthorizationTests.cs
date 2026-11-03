using Daycare.Application.Abstractions;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Billing.Invoices.GetInvoiceById;
using Daycare.Application.Features.Billing.Invoices.GetInvoices;
using Daycare.Application.Features.Children.GetChildren;
using Daycare.Application.Features.DailyReports.AddDailyReportEntry;
using Daycare.Application.Features.DailyReports.GetDailyReportDetail;
using Daycare.Application.Features.Incidents.GetIncidentById;
using Daycare.Application.Tests.Helpers;
using Daycare.Domain.Common;
using Daycare.Domain.Entities;
using Daycare.Domain.Enums;
using Daycare.Domain.Exceptions;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Daycare.Application.Tests.Auth;

/// <summary>
/// Mandatory Authorization Tests:
/// - A Parent cannot view another family's child, report, incident, or invoice.
/// - A Staff member cannot view or write for a child outside their classrooms and cannot read invoices.
/// - A Staff member cannot add daily report entries for a child who is not checked in.
/// </summary>
public class MandatoryAuthorizationTests
{
    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly IAccessGuard _accessGuard = Substitute.For<IAccessGuard>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    private readonly Guid _childId = Guid.NewGuid();
    private readonly Guid _otherChildId = Guid.NewGuid();
    private readonly Guid _classroomId = Guid.NewGuid();
    private readonly DateOnly _today = new(2026, 4, 15);
    private readonly DateTime _nowUtc = new(2026, 4, 15, 10, 0, 0, DateTimeKind.Utc);

    public MandatoryAuthorizationTests()
    {
        _dateTimeProvider.LocalToday.Returns(_today);
        _dateTimeProvider.UtcNow.Returns(_nowUtc);
    }

    [Fact]
    public async Task Parent_CannotViewAnotherFamilysChild_ShouldThrowForbidden()
    {
        // Arrange
        _accessGuard.EnsureCanViewChildAsync(_otherChildId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new ForbiddenException("Parents may only view children linked to their guardian record.")));

        var useCase = new GetChildByIdUseCase(_accessGuard, _dbContext, _dateTimeProvider);

        // Act & Assert
        var ex = await Should.ThrowAsync<ForbiddenException>(() =>
            useCase.ExecuteAsync(new GetChildByIdRequest(_otherChildId)));
        ex.Message.ShouldContain("Parents may only view children linked to their guardian record");
    }

    [Fact]
    public async Task Parent_CannotViewAnotherFamilysReport_ShouldThrowForbidden()
    {
        // Arrange
        _accessGuard.EnsureCanViewChildAsync(_otherChildId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new ForbiddenException("Parents may only view children linked to their guardian record.")));

        var validator = new GetDailyReportDetailValidator();
        var useCase = new GetDailyReportDetailUseCase(_dbContext, _accessGuard, validator);

        // Act & Assert
        var ex = await Should.ThrowAsync<ForbiddenException>(() =>
            useCase.ExecuteAsync(new GetDailyReportDetailRequest(_otherChildId, _today)));
        ex.Message.ShouldContain("Parents may only view children linked to their guardian record");
    }

    [Fact]
    public async Task Parent_CannotViewAnotherFamilysIncident_ShouldThrowForbidden()
    {
        // Arrange
        var incident = new Incident(_otherChildId, _classroomId, _nowUtc, IncidentType.Injury, IncidentSeverity.Minor, "Scraped knee", Guid.NewGuid(), "Staff");
        var child = new Child("Other", "Child", new DateOnly(2024, 1, 1), Gender.Male, new DateOnly(2026, 1, 1));
        var classroom = new Classroom("Toddlers", null, 12, 30, 12, 6);
        typeof(Incident).GetProperty(nameof(Incident.Child))!.SetValue(incident, child);
        typeof(Incident).GetProperty(nameof(Incident.Classroom))!.SetValue(incident, classroom);

        var incidentsSet = MockDbSetHelper.CreateMockDbSet([incident]);
        _dbContext.Incidents.Returns(incidentsSet);

        _accessGuard.EnsureCanViewChildAsync(_otherChildId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new ForbiddenException("Parents may only view children linked to their guardian record.")));

        var validator = new GetIncidentByIdValidator();
        var useCase = new GetIncidentByIdUseCase(_dbContext, _accessGuard, validator);

        // Act & Assert
        var ex = await Should.ThrowAsync<ForbiddenException>(() =>
            useCase.ExecuteAsync(new GetIncidentByIdRequest(incident.Id)));
        ex.Message.ShouldContain("Parents may only view children linked to their guardian record");
    }

    [Fact]
    public async Task Parent_CannotViewAnotherFamilysInvoice_ShouldThrowForbidden()
    {
        // Arrange
        var parent1GuardianId = Guid.NewGuid();
        var parent2GuardianId = Guid.NewGuid();

        _currentUser.Role.Returns("Parent");
        _currentUser.GuardianId.Returns(parent2GuardianId); // current user is Parent 2

        var invoice = new Invoice("INV-202604-00001", _childId, parent1GuardianId, new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30), new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 10), "ETB");
        var child = new Child("Kid", "One", new DateOnly(2024, 1, 1), Gender.Male, new DateOnly(2026, 1, 1));
        var guardian = new Guardian("Parent", "One", "+251911111111", "p1@daycare.local");
        typeof(Invoice).GetProperty(nameof(Invoice.Child))!.SetValue(invoice, child);
        typeof(Invoice).GetProperty(nameof(Invoice.Guardian))!.SetValue(invoice, guardian);

        var invoicesSet = MockDbSetHelper.CreateMockDbSet([invoice]);
        _dbContext.Invoices.Returns(invoicesSet);

        var validator = new GetInvoiceByIdValidator();
        var useCase = new GetInvoiceByIdUseCase(_dbContext, _currentUser, _dateTimeProvider, validator);

        // Act & Assert
        var ex = await Should.ThrowAsync<ForbiddenException>(() =>
            useCase.ExecuteAsync(new GetInvoiceByIdRequest(invoice.Id)));
        ex.Message.ShouldContain("Parents may only view their own invoices");
    }

    [Fact]
    public async Task Staff_CannotViewChildOutsideAssignedClassrooms_ShouldThrowForbidden()
    {
        // Arrange
        _accessGuard.EnsureCanViewChildAsync(_otherChildId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new ForbiddenException("Staff may only view children in classrooms assigned to them.")));

        var useCase = new GetChildByIdUseCase(_accessGuard, _dbContext, _dateTimeProvider);

        // Act & Assert
        var ex = await Should.ThrowAsync<ForbiddenException>(() =>
            useCase.ExecuteAsync(new GetChildByIdRequest(_otherChildId)));
        ex.Message.ShouldContain("classrooms assigned to them");
    }

    [Fact]
    public async Task Staff_CannotWriteForChildOutsideAssignedClassrooms_ShouldThrowForbidden()
    {
        // Arrange
        _currentUser.Role.Returns("Staff");
        _currentUser.UserId.Returns(Guid.NewGuid());

        _accessGuard.EnsureCanWriteChildDataAsync(_otherChildId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new ForbiddenException("Staff may only modify records for children in assigned classrooms.")));

        var attendanceRecord = AttendanceRecord.CreateCheckIn(_otherChildId, _classroomId, _today, _nowUtc, null, Guid.NewGuid(), "Staff");
        var attendanceSet = MockDbSetHelper.CreateMockDbSet([attendanceRecord]);
        _dbContext.AttendanceRecords.Returns(attendanceSet);

        var validator = new AddDailyReportEntryValidator();
        var useCase = new AddDailyReportEntryUseCase(_dbContext, _currentUser, _dateTimeProvider, _accessGuard, validator);

        // Act & Assert
        var ex = await Should.ThrowAsync<ForbiddenException>(() =>
            useCase.ExecuteAsync(new AddDailyReportEntryRequest(_otherChildId, _today, EntryType.Meal, _nowUtc, MealType: MealType.Lunch)));
        ex.Message.ShouldContain("assigned classrooms");
    }

    [Fact]
    public async Task Staff_CannotReadInvoices_ShouldThrowForbidden()
    {
        // Arrange
        _currentUser.Role.Returns("Staff");

        var getInvoicesValidator = new GetInvoicesValidator();
        var getInvoicesUseCase = new GetInvoicesUseCase(_dbContext, _currentUser, _dateTimeProvider, getInvoicesValidator);

        var getInvoiceByIdValidator = new GetInvoiceByIdValidator();
        var getInvoiceByIdUseCase = new GetInvoiceByIdUseCase(_dbContext, _currentUser, _dateTimeProvider, getInvoiceByIdValidator);

        // Act & Assert
        await Should.ThrowAsync<ForbiddenException>(() =>
            getInvoicesUseCase.ExecuteAsync(new GetInvoicesRequest()));

        await Should.ThrowAsync<ForbiddenException>(() =>
            getInvoiceByIdUseCase.ExecuteAsync(new GetInvoiceByIdRequest(Guid.NewGuid())));
    }

    [Fact]
    public async Task Staff_CannotAddDailyReportEntry_WhenChildNotCheckedIn_ShouldThrowNotCheckedIn()
    {
        // Arrange
        _currentUser.Role.Returns("Staff");
        _currentUser.UserId.Returns(Guid.NewGuid());

        var emptyAttendanceSet = MockDbSetHelper.CreateMockDbSet<AttendanceRecord>([]);
        _dbContext.AttendanceRecords.Returns(emptyAttendanceSet);

        var validator = new AddDailyReportEntryValidator();
        var useCase = new AddDailyReportEntryUseCase(_dbContext, _currentUser, _dateTimeProvider, _accessGuard, validator);

        // Act & Assert
        var ex = await Should.ThrowAsync<DomainRuleException>(() =>
            useCase.ExecuteAsync(new AddDailyReportEntryRequest(_childId, _today, EntryType.Meal, _nowUtc, MealType: MealType.Lunch)));
        ex.Code.ShouldBe("NOT_CHECKED_IN");
    }
}

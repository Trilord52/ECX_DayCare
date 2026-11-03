using Daycare.Application.Abstractions;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Reports.AttendanceReport;
using Daycare.Application.Features.Reports.BillingReport;
using Daycare.Application.Tests.Helpers;
using Daycare.Domain.Common;
using Daycare.Domain.Entities;
using Daycare.Domain.Enums;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Daycare.Application.Tests.Reports;

public class ReportsUseCaseTests
{
    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    private readonly Guid _classroomId = Guid.NewGuid();
    private readonly Classroom _classroom;
    private readonly DateOnly _today = new(2026, 4, 15);
    private readonly DateTime _nowUtc = new(2026, 4, 15, 10, 0, 0, DateTimeKind.Utc);

    public ReportsUseCaseTests()
    {
        _dateTimeProvider.LocalToday.Returns(_today);
        _dateTimeProvider.UtcNow.Returns(_nowUtc);

        _classroom = new Classroom("Toddlers", "Toddler room", 12, 30, 12, 6, true);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(_classroom, _classroomId);
    }

    [Fact]
    public async Task GetAttendanceReport_WhenRoleNotAdmin_ShouldThrowForbidden()
    {
        // Arrange
        _currentUser.Role.Returns("Staff");
        var validator = new GetAttendanceReportValidator();
        var useCase = new GetAttendanceReportUseCase(_dbContext, _currentUser, _dateTimeProvider, validator);

        // Act & Assert
        await Should.ThrowAsync<ForbiddenException>(() =>
            useCase.ExecuteAsync(new GetAttendanceReportRequest()));
    }

    [Fact]
    public async Task GetAttendanceReport_WhenAdmin_ShouldFilterAndCalculateMetrics()
    {
        // Arrange
        _currentUser.Role.Returns("Admin");

        var child = new Child("Abel", "Abebe", new DateOnly(2024, 1, 1), Gender.Male, new DateOnly(2026, 1, 1), _classroomId);
        typeof(Child).GetProperty(nameof(Child.Classroom))!.SetValue(child, _classroom);

        var att1 = AttendanceRecord.CreateCheckIn(child.Id, _classroomId, new DateOnly(2026, 4, 10), _nowUtc, null, Guid.NewGuid(), "Staff");
        att1.CheckOut(_nowUtc.AddHours(8), Guid.NewGuid(), Guid.NewGuid(), "Staff", true, 25);
        typeof(AttendanceRecord).GetProperty(nameof(AttendanceRecord.Child))!.SetValue(att1, child);
        typeof(AttendanceRecord).GetProperty(nameof(AttendanceRecord.Classroom))!.SetValue(att1, _classroom);

        var att2 = AttendanceRecord.CreateAbsent(child.Id, _classroomId, new DateOnly(2026, 4, 11), "Fever", Guid.NewGuid(), "Staff");
        typeof(AttendanceRecord).GetProperty(nameof(AttendanceRecord.Child))!.SetValue(att2, child);
        typeof(AttendanceRecord).GetProperty(nameof(AttendanceRecord.Classroom))!.SetValue(att2, _classroom);

        // Outside date range
        var att3 = AttendanceRecord.CreateCheckIn(child.Id, _classroomId, new DateOnly(2026, 3, 1), _nowUtc.AddMonths(-1), null, Guid.NewGuid(), "Staff");
        typeof(AttendanceRecord).GetProperty(nameof(AttendanceRecord.Child))!.SetValue(att3, child);
        typeof(AttendanceRecord).GetProperty(nameof(AttendanceRecord.Classroom))!.SetValue(att3, _classroom);

        var attendanceSet = MockDbSetHelper.CreateMockDbSet([att1, att2, att3]);
        _dbContext.AttendanceRecords.Returns(attendanceSet);

        var validator = new GetAttendanceReportValidator();
        var useCase = new GetAttendanceReportUseCase(_dbContext, _currentUser, _dateTimeProvider, validator);

        // Act
        var result = await useCase.ExecuteAsync(new GetAttendanceReportRequest(
            From: new DateOnly(2026, 4, 1),
            To: new DateOnly(2026, 4, 30),
            ClassroomId: _classroomId));

        // Assert
        result.TotalRecords.ShouldBe(2);
        result.TotalPresent.ShouldBe(1);
        result.TotalAbsences.ShouldBe(1);
        result.TotalLatePickups.ShouldBe(1);
        result.TotalLateMinutes.ShouldBe(25);
        result.Items.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetBillingReport_WhenRoleNotAdmin_ShouldThrowForbidden()
    {
        // Arrange
        _currentUser.Role.Returns("Staff");
        var validator = new GetBillingReportValidator();
        var useCase = new GetBillingReportUseCase(_dbContext, _currentUser, _dateTimeProvider, validator);

        // Act & Assert
        await Should.ThrowAsync<ForbiddenException>(() =>
            useCase.ExecuteAsync(new GetBillingReportRequest()));
    }

    [Fact]
    public async Task GetBillingReport_WhenAdmin_ShouldFilterAndCalculateMetrics()
    {
        // Arrange
        _currentUser.Role.Returns("Admin");

        var child = new Child("Abel", "Abebe", new DateOnly(2024, 1, 1), Gender.Male, new DateOnly(2026, 1, 1), _classroomId);
        var guardian = new Guardian("Parent", "One", "+251911111111", "p1@daycare.local");

        var inv1 = new Invoice("INV-202604-00001", child.Id, guardian.Id, new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30), new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 10), "ETB");
        inv1.AddLine(InvoiceLineType.Tuition, "Tuition", 1, 4000m, 0);
        inv1.ApplyPayment(4000m);
        typeof(Invoice).GetProperty(nameof(Invoice.Child))!.SetValue(inv1, child);
        typeof(Invoice).GetProperty(nameof(Invoice.Guardian))!.SetValue(inv1, guardian);

        var inv2 = new Invoice("INV-202604-00002", child.Id, guardian.Id, new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30), new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 10), "ETB");
        inv2.AddLine(InvoiceLineType.Tuition, "Tuition", 1, 3000m, 0);
        typeof(Invoice).GetProperty(nameof(Invoice.Child))!.SetValue(inv2, child);
        typeof(Invoice).GetProperty(nameof(Invoice.Guardian))!.SetValue(inv2, guardian);

        // Void invoice - should be excluded
        var inv3 = new Invoice("INV-202604-00003", child.Id, guardian.Id, new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30), new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 10), "ETB");
        inv3.AddLine(InvoiceLineType.Tuition, "Tuition", 1, 5000m, 0);
        inv3.Void("Cancelled", _nowUtc);
        typeof(Invoice).GetProperty(nameof(Invoice.Child))!.SetValue(inv3, child);
        typeof(Invoice).GetProperty(nameof(Invoice.Guardian))!.SetValue(inv3, guardian);

        var invoicesSet = MockDbSetHelper.CreateMockDbSet([inv1, inv2, inv3]);
        _dbContext.Invoices.Returns(invoicesSet);

        var validator = new GetBillingReportValidator();
        var useCase = new GetBillingReportUseCase(_dbContext, _currentUser, _dateTimeProvider, validator);

        // Act
        var result = await useCase.ExecuteAsync(new GetBillingReportRequest(
            From: new DateOnly(2026, 4, 1),
            To: new DateOnly(2026, 4, 30)));

        // Assert
        result.TotalInvoices.ShouldBe(2);
        result.TotalBilled.ShouldBe(7000m);
        result.TotalCollected.ShouldBe(4000m);
        result.TotalOutstanding.ShouldBe(3000m);
        result.OverdueInvoicesCount.ShouldBe(1); // inv2 due date 2026-04-10 < today 2026-04-15
        result.Currency.ShouldBe("ETB");
        result.Items.Count.ShouldBe(2);
    }
}

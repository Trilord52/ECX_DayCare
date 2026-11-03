using Daycare.Application.Abstractions;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Dashboard.AdminDashboard;
using Daycare.Application.Features.Dashboard.StaffDashboard;
using Daycare.Application.Tests.Helpers;
using Daycare.Domain.Common;
using Daycare.Domain.Entities;
using Daycare.Domain.Enums;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Daycare.Application.Tests.Dashboard;

public class DashboardUseCaseTests
{
    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    private readonly Guid _classroomId = Guid.NewGuid();
    private readonly Classroom _classroom;
    private readonly DateOnly _today = new(2026, 4, 15);
    private readonly DateTime _nowUtc = new(2026, 4, 15, 10, 0, 0, DateTimeKind.Utc);

    public DashboardUseCaseTests()
    {
        _dateTimeProvider.LocalToday.Returns(_today);
        _dateTimeProvider.UtcNow.Returns(_nowUtc);

        _classroom = new Classroom("Toddlers", "Toddler room", 12, 30, 12, 6, true);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(_classroom, _classroomId);
    }

    [Fact]
    public async Task GetAdminDashboard_WhenRoleIsNotAdmin_ShouldThrowForbidden()
    {
        // Arrange
        _currentUser.Role.Returns("Staff");
        var useCase = new GetAdminDashboardUseCase(_dbContext, _currentUser, _dateTimeProvider);

        // Act & Assert
        await Should.ThrowAsync<ForbiddenException>(() =>
            useCase.ExecuteAsync(new GetAdminDashboardRequest()));
    }

    [Fact]
    public async Task GetAdminDashboard_WhenRoleIsAdmin_ShouldReturnAccurateMetrics()
    {
        // Arrange
        _currentUser.Role.Returns("Admin");

        var child1 = new Child("Abel", "Abebe", new DateOnly(2024, 1, 1), Gender.Male, new DateOnly(2026, 1, 1), _classroomId);
        var child2 = new Child("Sara", "Abebe", new DateOnly(2024, 2, 1), Gender.Female, new DateOnly(2026, 1, 1), _classroomId);
        var child3 = new Child("Noah", "Henok", new DateOnly(2024, 3, 1), Gender.Male, new DateOnly(2026, 1, 1), _classroomId);

        var childrenSet = MockDbSetHelper.CreateMockDbSet([child1, child2, child3]);
        _dbContext.Children.Returns(childrenSet);

        // child1 is Present in building, child2 is Absent, child3 is NotArrived
        var att1 = AttendanceRecord.CreateCheckIn(child1.Id, _classroomId, _today, _nowUtc, null, Guid.NewGuid(), "Staff");
        var att2 = AttendanceRecord.CreateAbsent(child2.Id, _classroomId, _today, "Cold", Guid.NewGuid(), "Staff");

        var attendanceSet = MockDbSetHelper.CreateMockDbSet([att1, att2]);
        _dbContext.AttendanceRecords.Returns(attendanceSet);

        var classroomsSet = MockDbSetHelper.CreateMockDbSet([_classroom]);
        _dbContext.Classrooms.Returns(classroomsSet);

        var staffMember = new Staff("Tigist", "Alemu", "+251911111111", "staff@daycare.local", "Teacher", new DateOnly(2024, 1, 1), Guid.NewGuid());
        var staffClassroom = new StaffClassroom(staffMember.Id, _classroomId, _nowUtc);
        typeof(StaffClassroom).GetProperty(nameof(StaffClassroom.Staff))!.SetValue(staffClassroom, staffMember);

        var staffClassroomsSet = MockDbSetHelper.CreateMockDbSet([staffClassroom]);
        _dbContext.StaffClassrooms.Returns(staffClassroomsSet);

        var incident = new Incident(child1.Id, _classroomId, _nowUtc, IncidentType.Injury, IncidentSeverity.Minor, "Scraped elbow", Guid.NewGuid(), "Staff");
        typeof(Incident).GetProperty(nameof(Incident.Child))!.SetValue(incident, child1);

        var incidentsSet = MockDbSetHelper.CreateMockDbSet([incident]);
        _dbContext.Incidents.Returns(incidentsSet);

        var invoice = new Invoice("INV-202604-00001", child1.Id, Guid.NewGuid(), new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30), new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 10), "ETB");
        invoice.AddLine(InvoiceLineType.Tuition, "Tuition", 1, 4500m, 0);

        var invoicesSet = MockDbSetHelper.CreateMockDbSet([invoice]);
        var paymentsSet0 = MockDbSetHelper.CreateMockDbSet<Payment>([]);
        _dbContext.Invoices.Returns(invoicesSet);
        _dbContext.Payments.Returns(paymentsSet0);

        var useCase = new GetAdminDashboardUseCase(_dbContext, _currentUser, _dateTimeProvider);

        // Act
        var result = await useCase.ExecuteAsync(new GetAdminDashboardRequest());

        // Assert
        result.Date.ShouldBe(_today);
        result.EnrolledChildren.ShouldBe(3);
        result.PresentToday.ShouldBe(1);
        result.InBuildingNow.ShouldBe(1);
        result.AbsentToday.ShouldBe(1);
        result.NotArrivedToday.ShouldBe(1);
        result.UnacknowledgedIncidents.ShouldBe(1);
        result.Ratios.Count.ShouldBe(1);
        result.Ratios[0].PresentCount.ShouldBe(1);
        result.Ratios[0].StaffCount.ShouldBe(1);
        result.Ratios[0].IsOverLimit.ShouldBeFalse();
        result.Billing.Invoiced.ShouldBe(4500m);
        result.Billing.Outstanding.ShouldBe(4500m);
        result.Billing.Currency.ShouldBe("ETB");
        result.AttendanceTrend.Count.ShouldBe(14);
    }

    [Fact]
    public async Task GetAdminDashboard_WhenNoInvoicesThisMonthButUnpaidInvoicesLastMonth_ShouldReturnCorrectBillingSummary()
    {
        // Arrange
        _currentUser.Role.Returns("Admin");

        var emptyChildren = MockDbSetHelper.CreateMockDbSet<Child>([]);
        var emptyAttendance = MockDbSetHelper.CreateMockDbSet<AttendanceRecord>([]);
        var emptyClassrooms = MockDbSetHelper.CreateMockDbSet<Classroom>([]);
        var emptyStaffClassrooms = MockDbSetHelper.CreateMockDbSet<StaffClassroom>([]);
        var emptyIncidents = MockDbSetHelper.CreateMockDbSet<Incident>([]);

        _dbContext.Children.Returns(emptyChildren);
        _dbContext.AttendanceRecords.Returns(emptyAttendance);
        _dbContext.Classrooms.Returns(emptyClassrooms);
        _dbContext.StaffClassrooms.Returns(emptyStaffClassrooms);
        _dbContext.Incidents.Returns(emptyIncidents);

        var childId = Guid.NewGuid();
        var guardianId = Guid.NewGuid();

        // Invoice 1: Last month (March), Due March 10 (overdue), total 4500, paid 1000, balance 3500
        var inv1 = new Invoice("INV-202603-00001", childId, guardianId, new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31), new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 10), "ETB");
        inv1.AddLine(InvoiceLineType.Tuition, "March Tuition", 1, 4500m, 0);
        inv1.ApplyPayment(1000m);

        // Invoice 2: Last month (March), Due April 25 (future), total 3000, paid 0, balance 3000
        var inv2 = new Invoice("INV-202603-00002", childId, guardianId, new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31), new DateOnly(2026, 3, 1), new DateOnly(2026, 4, 25), "ETB");
        inv2.AddLine(InvoiceLineType.Tuition, "March Tuition 2", 1, 3000m, 0);

        var testInvoicesSet = MockDbSetHelper.CreateMockDbSet([inv1, inv2]);
        _dbContext.Invoices.Returns(testInvoicesSet);

        // Payment 1: April 5 payment of 500 ETB
        var pay1 = new Payment(inv1.Id, 500m, PaymentMethod.Cash, new DateTime(2026, 4, 5, 10, 0, 0, DateTimeKind.Utc), Guid.NewGuid(), "Admin");
        // Payment 2: March 5 payment of 1000 ETB
        var pay2 = new Payment(inv1.Id, 1000m, PaymentMethod.Cash, new DateTime(2026, 3, 5, 10, 0, 0, DateTimeKind.Utc), Guid.NewGuid(), "Admin");

        var testPaymentsSet = MockDbSetHelper.CreateMockDbSet([pay1, pay2]);
        _dbContext.Payments.Returns(testPaymentsSet);

        var useCase = new GetAdminDashboardUseCase(_dbContext, _currentUser, _dateTimeProvider);

        // Act
        var result = await useCase.ExecuteAsync(new GetAdminDashboardRequest());

        // Assert
        result.Billing.Month.ShouldBe("2026-04");
        result.Billing.Invoiced.ShouldBe(0m); // No invoices for current month (April)
        result.Billing.Collected.ShouldBe(500m); // Only payments received in April
        result.Billing.Outstanding.ShouldBe(6500m); // 3500 + 3000 unpaid balance across any month
        result.Billing.OverdueCount.ShouldBe(1); // Only inv1 is past due with balance > 0
    }

    [Fact]
    public async Task GetAdminDashboard_WithVoidedInvoiceAndVoidedPayment_ShouldExcludeThemFromBillingSummary()
    {
        // Arrange
        _currentUser.Role.Returns("Admin");

        var emptyChildren2 = MockDbSetHelper.CreateMockDbSet<Child>([]);
        var emptyAttendance2 = MockDbSetHelper.CreateMockDbSet<AttendanceRecord>([]);
        var emptyClassrooms2 = MockDbSetHelper.CreateMockDbSet<Classroom>([]);
        var emptyStaffClassrooms2 = MockDbSetHelper.CreateMockDbSet<StaffClassroom>([]);
        var emptyIncidents2 = MockDbSetHelper.CreateMockDbSet<Incident>([]);

        _dbContext.Children.Returns(emptyChildren2);
        _dbContext.AttendanceRecords.Returns(emptyAttendance2);
        _dbContext.Classrooms.Returns(emptyClassrooms2);
        _dbContext.StaffClassrooms.Returns(emptyStaffClassrooms2);
        _dbContext.Incidents.Returns(emptyIncidents2);

        var childId = Guid.NewGuid();
        var guardianId = Guid.NewGuid();
        var adminId = Guid.NewGuid();

        // Active invoice in April: 4500 ETB, overdue (due April 10 < April 15)
        var activeInv = new Invoice("INV-202604-00001", childId, guardianId, new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30), new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 10), "ETB");
        activeInv.AddLine(InvoiceLineType.Tuition, "Tuition", 1, 4500m, 0);

        // Voided invoice in April: 3000 ETB
        var voidedInv = new Invoice("INV-202604-00002", childId, guardianId, new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30), new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 10), "ETB");
        voidedInv.AddLine(InvoiceLineType.Tuition, "Tuition", 1, 3000m, 0);
        voidedInv.Void("Cancelled enrollment", _nowUtc);

        var testInvoicesSet2 = MockDbSetHelper.CreateMockDbSet([activeInv, voidedInv]);
        _dbContext.Invoices.Returns(testInvoicesSet2);

        // Active payment: 1000 ETB in April
        var activePay = new Payment(activeInv.Id, 1000m, PaymentMethod.Cash, new DateTime(2026, 4, 2, 10, 0, 0, DateTimeKind.Utc), adminId, "Admin");

        // Voided payment: 500 ETB in April
        var voidedPay = new Payment(activeInv.Id, 500m, PaymentMethod.Cash, new DateTime(2026, 4, 3, 10, 0, 0, DateTimeKind.Utc), adminId, "Admin");
        voidedPay.Void("Mistaken entry", adminId, "Admin", _nowUtc);

        var testPaymentsSet2 = MockDbSetHelper.CreateMockDbSet([activePay, voidedPay]);
        _dbContext.Payments.Returns(testPaymentsSet2);

        var useCase = new GetAdminDashboardUseCase(_dbContext, _currentUser, _dateTimeProvider);

        // Act
        var result = await useCase.ExecuteAsync(new GetAdminDashboardRequest());

        // Assert
        result.Billing.Invoiced.ShouldBe(4500m); // Excludes voided invoice (3000)
        result.Billing.Collected.ShouldBe(1000m); // Excludes voided payment (500)
        result.Billing.Outstanding.ShouldBe(4500m); // Excludes voided invoice balance
        result.Billing.OverdueCount.ShouldBe(1); // Excludes voided invoice
    }

    [Fact]
    public async Task GetStaffDashboard_WhenRoleIsNotStaffOrAdmin_ShouldThrowForbidden()
    {
        // Arrange
        _currentUser.Role.Returns("Parent");
        var useCase = new GetStaffDashboardUseCase(_dbContext, _currentUser, _dateTimeProvider);

        // Act & Assert
        await Should.ThrowAsync<ForbiddenException>(() =>
            useCase.ExecuteAsync(new GetStaffDashboardRequest()));
    }

    [Fact]
    public async Task GetStaffDashboard_WhenStaffAssignedToClassroom_ShouldReturnScopedClassroomMetrics()
    {
        // Arrange
        var staffUserId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        _currentUser.Role.Returns("Staff");
        _currentUser.UserId.Returns(staffUserId);
        _currentUser.StaffId.Returns(staffId);

        var staffMember = new Staff("Tigist", "Alemu", "+251911111111", "staff@daycare.local", "Teacher", new DateOnly(2024, 1, 1), staffUserId);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(staffMember, staffId);

        var staffClassroom = new StaffClassroom(staffId, _classroomId, _nowUtc);
        typeof(StaffClassroom).GetProperty(nameof(StaffClassroom.Staff))!.SetValue(staffClassroom, staffMember);

        var child1 = new Child("Abel", "Abebe", new DateOnly(2024, 1, 1), Gender.Male, new DateOnly(2026, 1, 1), _classroomId);
        var child2 = new Child("Sara", "Abebe", new DateOnly(2024, 2, 1), Gender.Female, new DateOnly(2026, 1, 1), _classroomId);

        var childrenSet = MockDbSetHelper.CreateMockDbSet([child1, child2]);
        _dbContext.Children.Returns(childrenSet);

        var att1 = AttendanceRecord.CreateCheckIn(child1.Id, _classroomId, _today, _nowUtc, null, staffUserId, "Tigist");
        var attendanceSet = MockDbSetHelper.CreateMockDbSet([att1]);
        _dbContext.AttendanceRecords.Returns(attendanceSet);

        var classroomsSet = MockDbSetHelper.CreateMockDbSet([_classroom]);
        _dbContext.Classrooms.Returns(classroomsSet);

        var staffClassroomsSet = MockDbSetHelper.CreateMockDbSet([staffClassroom]);
        _dbContext.StaffClassrooms.Returns(staffClassroomsSet);

        var incidentsSet = MockDbSetHelper.CreateMockDbSet<Incident>([]);
        _dbContext.Incidents.Returns(incidentsSet);

        var useCase = new GetStaffDashboardUseCase(_dbContext, _currentUser, _dateTimeProvider);

        // Act
        var result = await useCase.ExecuteAsync(new GetStaffDashboardRequest());

        // Assert
        result.Date.ShouldBe(_today);
        result.Classrooms.Count.ShouldBe(1);
        var cr = result.Classrooms[0];
        cr.ClassroomId.ShouldBe(_classroomId);
        cr.TotalEnrolled.ShouldBe(2);
        cr.PresentCount.ShouldBe(1);
        cr.CheckedOutCount.ShouldBe(0);
        cr.AbsentCount.ShouldBe(0);
        cr.NotArrivedCount.ShouldBe(1);
        cr.Ratio.PresentCount.ShouldBe(1);
        cr.Ratio.StaffCount.ShouldBe(1);
        cr.Ratio.IsOverLimit.ShouldBeFalse();
    }
}

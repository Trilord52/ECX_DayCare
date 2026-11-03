using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Features.Billing.Invoices.GenerateInvoices;
using Daycare.Application.Tests.Helpers;
using Daycare.Domain.Common;
using Daycare.Domain.Entities;
using Daycare.Domain.Enums;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Daycare.Application.Tests.Billing;

public class InvoiceGenerationUseCaseTests
{
    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly DaycareOptions _daycareOptions = new()
    {
        LateFeeBlockMinutes = 15,
        LateFeePerBlock = 50m,
        InvoiceDueDays = 10,
        Currency = "ETB"
    };

    private readonly DateOnly _today = new(2026, 4, 1);
    private readonly DateTime _nowUtc = new(2026, 4, 1, 8, 0, 0, DateTimeKind.Utc);
    private readonly Guid _classroomId = Guid.NewGuid();
    private readonly FeePlan _feePlan;

    public InvoiceGenerationUseCaseTests()
    {
        _feePlan = new FeePlan("Full-time monthly", "Full time", 4500m);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(_feePlan, Guid.NewGuid());

        _dateTimeProvider.LocalToday.Returns(_today);
        _dateTimeProvider.UtcNow.Returns(_nowUtc);
        _dbContext.GetNextInvoiceSequenceAsync(Arg.Any<CancellationToken>()).Returns(1L, 2L, 3L, 4L, 5L);
    }

    [Fact]
    public async Task GenerateInvoices_WhenChildHasNoFeePlan_ShouldSkipWithNoFeePlan()
    {
        // Arrange
        var child = new Child("Abel", "Abebe", new DateOnly(2024, 1, 1), Gender.Male, new DateOnly(2026, 1, 1), _classroomId, feePlanId: null);
        var guardian = new Guardian("Parent", "One", "+251911111111", "parent@daycare.local");
        var link = new ChildGuardian(child.Id, guardian.Id, Relationship.Father, isPrimaryContact: true, canPickUp: true, isPayer: true, isEmergencyContact: true);
        typeof(Child).GetProperty(nameof(Child.Guardians))!.SetValue(child, new List<ChildGuardian> { link });

        var childSet = MockDbSetHelper.CreateMockDbSet([child]);
        _dbContext.Children.Returns(childSet);

        var invoiceSet = MockDbSetHelper.CreateMockDbSet<Invoice>([]);
        _dbContext.Invoices.Returns(invoiceSet);

        var attendanceSet = MockDbSetHelper.CreateMockDbSet<AttendanceRecord>([]);
        _dbContext.AttendanceRecords.Returns(attendanceSet);

        var validator = new GenerateInvoicesValidator();
        var useCase = new GenerateInvoicesUseCase(
            _dbContext, _dateTimeProvider, Options.Create(_daycareOptions), validator);

        // Act
        var result = await useCase.ExecuteAsync(new GenerateInvoicesRequest(2026, 4), CancellationToken.None);

        // Assert
        result.InvoiceIds.ShouldBeEmpty();
        result.Skipped.Count.ShouldBe(1);
        result.Skipped[0].Code.ShouldBe("NO_FEE_PLAN");
    }

    [Fact]
    public async Task GenerateInvoices_WhenChildHasNoPayerGuardian_ShouldSkipWithNoPayerGuardian()
    {
        // Arrange
        var child = new Child("Abel", "Abebe", new DateOnly(2024, 1, 1), Gender.Male, new DateOnly(2026, 1, 1), _classroomId, feePlanId: _feePlan.Id);
        typeof(Child).GetProperty(nameof(Child.FeePlan))!.SetValue(child, _feePlan);

        var guardian = new Guardian("Parent", "One", "+251911111111", "parent@daycare.local");
        var link = new ChildGuardian(child.Id, guardian.Id, Relationship.Father, isPrimaryContact: true, canPickUp: true, isPayer: false, isEmergencyContact: true);
        typeof(Child).GetProperty(nameof(Child.Guardians))!.SetValue(child, new List<ChildGuardian> { link });

        var childSet = MockDbSetHelper.CreateMockDbSet([child]);
        _dbContext.Children.Returns(childSet);

        var invoiceSet = MockDbSetHelper.CreateMockDbSet<Invoice>([]);
        _dbContext.Invoices.Returns(invoiceSet);

        var attendanceSet = MockDbSetHelper.CreateMockDbSet<AttendanceRecord>([]);
        _dbContext.AttendanceRecords.Returns(attendanceSet);

        var validator = new GenerateInvoicesValidator();
        var useCase = new GenerateInvoicesUseCase(
            _dbContext, _dateTimeProvider, Options.Create(_daycareOptions), validator);

        // Act
        var result = await useCase.ExecuteAsync(new GenerateInvoicesRequest(2026, 4), CancellationToken.None);

        // Assert
        result.InvoiceIds.ShouldBeEmpty();
        result.Skipped.Count.ShouldBe(1);
        result.Skipped[0].Code.ShouldBe("NO_PAYER_GUARDIAN");
    }

    [Fact]
    public async Task GenerateInvoices_WhenInvoiceAlreadyExists_ShouldSkipWithInvoiceAlreadyExists()
    {
        // Arrange
        var child = new Child("Abel", "Abebe", new DateOnly(2024, 1, 1), Gender.Male, new DateOnly(2026, 1, 1), _classroomId, feePlanId: _feePlan.Id);
        typeof(Child).GetProperty(nameof(Child.FeePlan))!.SetValue(child, _feePlan);

        var guardian = new Guardian("Parent", "One", "+251911111111", "parent@daycare.local");
        var link = new ChildGuardian(child.Id, guardian.Id, Relationship.Father, isPrimaryContact: true, canPickUp: true, isPayer: true, isEmergencyContact: true);
        typeof(Child).GetProperty(nameof(Child.Guardians))!.SetValue(child, new List<ChildGuardian> { link });

        var childSet = MockDbSetHelper.CreateMockDbSet([child]);
        _dbContext.Children.Returns(childSet);

        var periodStart = new DateOnly(2026, 4, 1);
        var existingInvoice = new Invoice("INV-202604-00001", child.Id, guardian.Id, periodStart, periodStart.AddMonths(1).AddDays(-1), _today, _today.AddDays(10));
        var invoiceSet = MockDbSetHelper.CreateMockDbSet([existingInvoice]);
        _dbContext.Invoices.Returns(invoiceSet);

        var attendanceSet = MockDbSetHelper.CreateMockDbSet<AttendanceRecord>([]);
        _dbContext.AttendanceRecords.Returns(attendanceSet);

        var validator = new GenerateInvoicesValidator();
        var useCase = new GenerateInvoicesUseCase(
            _dbContext, _dateTimeProvider, Options.Create(_daycareOptions), validator);

        // Act
        var result = await useCase.ExecuteAsync(new GenerateInvoicesRequest(2026, 4), CancellationToken.None);

        // Assert
        result.InvoiceIds.ShouldBeEmpty();
        result.Skipped.Count.ShouldBe(1);
        result.Skipped[0].Code.ShouldBe("INVOICE_ALREADY_EXISTS");
    }

    [Fact]
    public async Task GenerateInvoices_WithLateMinutes_ShouldCreateTuitionAndLateFeeLines()
    {
        // Arrange
        var child = new Child("Abel", "Abebe", new DateOnly(2024, 1, 1), Gender.Male, new DateOnly(2026, 1, 1), _classroomId, feePlanId: _feePlan.Id);
        typeof(Child).GetProperty(nameof(Child.FeePlan))!.SetValue(child, _feePlan);

        var guardian = new Guardian("Parent", "One", "+251911111111", "parent@daycare.local");
        var link = new ChildGuardian(child.Id, guardian.Id, Relationship.Father, isPrimaryContact: true, canPickUp: true, isPayer: true, isEmergencyContact: true);
        typeof(Child).GetProperty(nameof(Child.Guardians))!.SetValue(child, new List<ChildGuardian> { link });

        var childSet = MockDbSetHelper.CreateMockDbSet([child]);
        _dbContext.Children.Returns(childSet);

        var createdInvoices = new List<Invoice>();
        var invoiceSet = MockDbSetHelper.CreateMockDbSet(createdInvoices);
        _dbContext.Invoices.Returns(invoiceSet);

        // Attendance records with late minutes:
        // Record 1: 10 late minutes -> ceil(10/15) = 1 block
        // Record 2: 35 late minutes -> ceil(35/15) = 3 blocks
        // Total blocks = 4 blocks -> 4 * 50 = 200 ETB
        var record1 = AttendanceRecord.CreateCheckIn(child.Id, _classroomId, new DateOnly(2026, 4, 5), _nowUtc, guardian.Id, Guid.NewGuid(), "Staff");
        record1.CheckOut(_nowUtc.AddHours(8), guardian.Id, Guid.NewGuid(), "Staff", true, 10);

        var record2 = AttendanceRecord.CreateCheckIn(child.Id, _classroomId, new DateOnly(2026, 4, 12), _nowUtc, guardian.Id, Guid.NewGuid(), "Staff");
        record2.CheckOut(_nowUtc.AddHours(8), guardian.Id, Guid.NewGuid(), "Staff", true, 35);

        var attendanceSet = MockDbSetHelper.CreateMockDbSet([record1, record2]);
        _dbContext.AttendanceRecords.Returns(attendanceSet);

        var validator = new GenerateInvoicesValidator();
        var useCase = new GenerateInvoicesUseCase(
            _dbContext, _dateTimeProvider, Options.Create(_daycareOptions), validator);

        // Act
        var result = await useCase.ExecuteAsync(new GenerateInvoicesRequest(2026, 4), CancellationToken.None);

        // Assert
        result.InvoiceIds.Count.ShouldBe(1);
        result.Skipped.ShouldBeEmpty();

        createdInvoices.Count.ShouldBe(1);
        var invoice = createdInvoices[0];
        invoice.InvoiceNumber.ShouldBe("INV-202604-00001");
        invoice.Lines.Count.ShouldBe(2);

        var tuitionLine = invoice.Lines.First(l => l.LineType == InvoiceLineType.Tuition);
        tuitionLine.LineTotal.ShouldBe(4500m);

        var lateFeeLine = invoice.Lines.First(l => l.LineType == InvoiceLineType.LateFee);
        lateFeeLine.Quantity.ShouldBe(4);
        lateFeeLine.UnitPrice.ShouldBe(50m);
        lateFeeLine.LineTotal.ShouldBe(200m);

        invoice.TotalAmount.ShouldBe(4700m);
        invoice.AmountPaid.ShouldBe(0m);
        invoice.Balance.ShouldBe(4700m);
    }

    [Fact]
    public async Task GenerateInvoices_WithNoLateMinutes_ShouldCreateOnlyTuitionLine()
    {
        // Arrange
        var child = new Child("Abel", "Abebe", new DateOnly(2024, 1, 1), Gender.Male, new DateOnly(2026, 1, 1), _classroomId, feePlanId: _feePlan.Id);
        typeof(Child).GetProperty(nameof(Child.FeePlan))!.SetValue(child, _feePlan);

        var guardian = new Guardian("Parent", "One", "+251911111111", "parent@daycare.local");
        var link = new ChildGuardian(child.Id, guardian.Id, Relationship.Father, isPrimaryContact: true, canPickUp: true, isPayer: true, isEmergencyContact: true);
        typeof(Child).GetProperty(nameof(Child.Guardians))!.SetValue(child, new List<ChildGuardian> { link });

        var childSet = MockDbSetHelper.CreateMockDbSet([child]);
        _dbContext.Children.Returns(childSet);

        var createdInvoices = new List<Invoice>();
        var invoiceSet = MockDbSetHelper.CreateMockDbSet(createdInvoices);
        _dbContext.Invoices.Returns(invoiceSet);

        // Attendance records with 0 late minutes:
        var record1 = AttendanceRecord.CreateCheckIn(child.Id, _classroomId, new DateOnly(2026, 4, 5), _nowUtc, guardian.Id, Guid.NewGuid(), "Staff");
        record1.CheckOut(_nowUtc.AddHours(8), guardian.Id, Guid.NewGuid(), "Staff", true, 0);

        var attendanceSet = MockDbSetHelper.CreateMockDbSet([record1]);
        _dbContext.AttendanceRecords.Returns(attendanceSet);

        var validator = new GenerateInvoicesValidator();
        var useCase = new GenerateInvoicesUseCase(
            _dbContext, _dateTimeProvider, Options.Create(_daycareOptions), validator);

        // Act
        var result = await useCase.ExecuteAsync(new GenerateInvoicesRequest(2026, 4), CancellationToken.None);

        // Assert
        result.InvoiceIds.Count.ShouldBe(1);
        createdInvoices.Count.ShouldBe(1);
        var invoice = createdInvoices[0];
        invoice.Lines.Count.ShouldBe(1);
        invoice.Lines.ShouldContain(l => l.LineType == InvoiceLineType.Tuition);
        invoice.Lines.ShouldNotContain(l => l.LineType == InvoiceLineType.LateFee);
        invoice.TotalAmount.ShouldBe(4500m);
        invoice.Balance.ShouldBe(4500m);
    }
}


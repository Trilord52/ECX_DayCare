using Daycare.Application.Abstractions;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Billing.Invoices.AddInvoiceAdjustment;
using Daycare.Application.Features.Billing.Invoices.VoidInvoice;
using Daycare.Application.Tests.Helpers;
using Daycare.Domain.Common;
using Daycare.Domain.Entities;
using Daycare.Domain.Enums;
using Daycare.Domain.Exceptions;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Daycare.Application.Tests.Billing;

public class InvoiceLifecycleUseCaseTests
{
    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _childId = Guid.NewGuid();
    private readonly Guid _guardianId = Guid.NewGuid();
    private readonly Child _child;
    private readonly Guardian _guardian;

    public InvoiceLifecycleUseCaseTests()
    {
        _dateTimeProvider.LocalToday.Returns(new DateOnly(2026, 4, 1));
        _dateTimeProvider.UtcNow.Returns(new DateTime(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc));
        _currentUser.UserId.Returns(_userId);
        _currentUser.Email.Returns("admin@daycare.local");
        _currentUser.Role.Returns("Admin");

        _child = new Child("Abel", "Abebe", new DateOnly(2024, 1, 1), Gender.Male, new DateOnly(2026, 1, 1), Guid.NewGuid());
        _guardian = new Guardian("Parent", "One", "+251911111111", "parent@daycare.local");
    }

    private Invoice CreateTestInvoice(decimal tuitionAmount = 4500m)
    {
        var invoice = new Invoice(
            "INV-202604-00001",
            _childId,
            _guardianId,
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 4, 30),
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 4, 10),
            "ETB");

        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(invoice, Guid.NewGuid());
        typeof(Invoice).GetProperty(nameof(Invoice.Child))!.SetValue(invoice, _child);
        typeof(Invoice).GetProperty(nameof(Invoice.Guardian))!.SetValue(invoice, _guardian);
        invoice.AddLine(InvoiceLineType.Tuition, "Tuition", 1, tuitionAmount, 0);
        return invoice;
    }

    [Fact]
    public async Task AddAdjustment_WhenInvoiceIsVoid_ShouldThrowInvoiceVoid()
    {
        // Arrange
        var invoice = CreateTestInvoice();
        invoice.Void("Cancelled enrollment", _dateTimeProvider.UtcNow);

        var invoiceSet = MockDbSetHelper.CreateMockDbSet([invoice]);
        _dbContext.Invoices.Returns(invoiceSet);

        var validator = new AddInvoiceAdjustmentValidator();
        var useCase = new AddInvoiceAdjustmentUseCase(_dbContext, _dateTimeProvider, validator);
        var request = new AddInvoiceAdjustmentRequest(invoice.Id, "Late pickup surcharge", 200m);

        // Act & Assert
        var ex = await Should.ThrowAsync<DomainRuleException>(() => useCase.ExecuteAsync(request));
        ex.Code.ShouldBe("INVOICE_VOID");
    }

    [Fact]
    public async Task AddAdjustment_WhenNegativeAdjustmentExceedsRemainingBalance_ShouldThrowOverpayment()
    {
        // Arrange
        var invoice = CreateTestInvoice(4500m);
        // Paid 3000, balance is 1500
        invoice.ApplyPayment(3000m);

        var invoiceSet = MockDbSetHelper.CreateMockDbSet([invoice]);
        _dbContext.Invoices.Returns(invoiceSet);

        var validator = new AddInvoiceAdjustmentValidator();
        var useCase = new AddInvoiceAdjustmentUseCase(_dbContext, _dateTimeProvider, validator);

        // Attempt discount of 2000 (total would become 2500, but 3000 was already paid!)
        var request = new AddInvoiceAdjustmentRequest(invoice.Id, "Sibling discount", -2000m);

        // Act & Assert
        var ex = await Should.ThrowAsync<DomainRuleException>(() => useCase.ExecuteAsync(request));
        ex.Code.ShouldBe("OVERPAYMENT");
    }

    [Fact]
    public async Task AddAdjustment_WithValidDiscount_ShouldReduceTotalAndBalance()
    {
        // Arrange
        var invoice = CreateTestInvoice(4500m);
        var invoiceSet = MockDbSetHelper.CreateMockDbSet([invoice]);
        _dbContext.Invoices.Returns(invoiceSet);

        var validator = new AddInvoiceAdjustmentValidator();
        var useCase = new AddInvoiceAdjustmentUseCase(_dbContext, _dateTimeProvider, validator);
        var request = new AddInvoiceAdjustmentRequest(invoice.Id, "Sibling discount", -500m);

        // Act
        var result = await useCase.ExecuteAsync(request);

        // Assert
        result.TotalAmount.ShouldBe(4000m);
        result.Balance.ShouldBe(4000m);
        result.Status.ShouldBe(InvoiceStatus.Issued);
        invoice.Lines.Count.ShouldBe(2);
        var adjLine = invoice.Lines.First(l => l.LineType == InvoiceLineType.Adjustment);
        adjLine.LineTotal.ShouldBe(-500m);
        adjLine.Description.ShouldBe("Sibling discount");
    }

    [Fact]
    public async Task VoidInvoice_WhenInvoiceHasActivePayments_ShouldThrowInvoiceHasPayments()
    {
        // Arrange
        var invoice = CreateTestInvoice(4500m);
        invoice.ApplyPayment(2000m);

        var payment = new Payment(
            invoice.Id,
            2000m,
            PaymentMethod.BankTransfer,
            _dateTimeProvider.UtcNow,
            _userId,
            "Admin",
            "TX-123");
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(payment, Guid.NewGuid());
        invoice.Payments.Add(payment);

        var invoiceSet = MockDbSetHelper.CreateMockDbSet([invoice]);
        var paymentSet = MockDbSetHelper.CreateMockDbSet([payment]);
        _dbContext.Invoices.Returns(invoiceSet);
        _dbContext.Payments.Returns(paymentSet);

        var validator = new VoidInvoiceValidator();
        var useCase = new VoidInvoiceUseCase(_dbContext, _dateTimeProvider, validator);
        var request = new VoidInvoiceRequest(invoice.Id, "Child withdrew from program");

        // Act & Assert
        var ex = await Should.ThrowAsync<DomainRuleException>(() => useCase.ExecuteAsync(request));
        ex.Code.ShouldBe("INVOICE_HAS_PAYMENTS");
    }

    [Fact]
    public async Task VoidInvoice_WithoutReason_ShouldThrowValidationFailed()
    {
        // Arrange
        var validator = new VoidInvoiceValidator();
        var useCase = new VoidInvoiceUseCase(_dbContext, _dateTimeProvider, validator);
        var request = new VoidInvoiceRequest(Guid.NewGuid(), "");

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationFailedException>(() => useCase.ExecuteAsync(request));
        ex.Errors.ShouldContainKey("reason");
    }

    [Fact]
    public async Task VoidInvoice_WithNoActivePayments_ShouldSucceed()
    {
        // Arrange
        var invoice = CreateTestInvoice(4500m);
        var invoiceSet = MockDbSetHelper.CreateMockDbSet([invoice]);
        var paymentSet = MockDbSetHelper.CreateMockDbSet<Payment>([]);
        _dbContext.Invoices.Returns(invoiceSet);
        _dbContext.Payments.Returns(paymentSet);

        var validator = new VoidInvoiceValidator();
        var useCase = new VoidInvoiceUseCase(_dbContext, _dateTimeProvider, validator);
        var request = new VoidInvoiceRequest(invoice.Id, "Incorrect fee plan applied");

        // Act
        var result = await useCase.ExecuteAsync(request);

        // Assert
        result.Status.ShouldBe(InvoiceStatus.Void);
        invoice.Status.ShouldBe(InvoiceStatus.Void);
        invoice.VoidReason.ShouldBe("Incorrect fee plan applied");
    }
}


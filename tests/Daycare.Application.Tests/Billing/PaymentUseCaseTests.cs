using Daycare.Application.Abstractions;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Billing.Payments.ProcessOnlinePayment;
using Daycare.Application.Features.Billing.Payments.RecordPayment;
using Daycare.Application.Features.Billing.Payments.VoidPayment;
using Daycare.Application.Tests.Helpers;
using Daycare.Domain.Common;
using Daycare.Domain.Entities;
using Daycare.Domain.Enums;
using Daycare.Domain.Exceptions;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Daycare.Application.Tests.Billing;

public class PaymentUseCaseTests
{
    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly IPaymentGateway _paymentGateway = Substitute.For<IPaymentGateway>();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _childId = Guid.NewGuid();
    private readonly Guid _guardianId = Guid.NewGuid();
    private readonly Child _child;
    private readonly Guardian _guardian;

    public PaymentUseCaseTests()
    {
        _currentUser.UserId.Returns(_userId);
        _currentUser.Email.Returns("admin@daycare.local");
        _currentUser.Role.Returns("Admin");
        _currentUser.FullName.Returns("System Admin");

        _dateTimeProvider.LocalToday.Returns(new DateOnly(2026, 4, 15));
        _dateTimeProvider.UtcNow.Returns(new DateTime(2026, 4, 15, 12, 0, 0, DateTimeKind.Utc));

        _child = new Child("Abel", "Abebe", new DateOnly(2023, 1, 1), Gender.Male, new DateOnly(2026, 1, 1), Guid.NewGuid());
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(_child, _childId);

        _guardian = new Guardian("Parent", "One", "+251911111111", "parent@daycare.local");
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(_guardian, _guardianId);
    }

    private Invoice CreateTestInvoice(decimal tuitionAmount = 5000m)
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
    public async Task RecordPayment_WhenInvoiceIsVoid_ShouldThrowInvoiceVoid()
    {
        // Arrange
        var invoice = CreateTestInvoice();
        invoice.Void("Test void", _dateTimeProvider.UtcNow);

        var invoiceSet = MockDbSetHelper.CreateMockDbSet([invoice]);
        _dbContext.Invoices.Returns(invoiceSet);

        var validator = new RecordPaymentValidator();
        var useCase = new RecordPaymentUseCase(_dbContext, _currentUser, _dateTimeProvider, validator);
        var request = new RecordPaymentRequest(invoice.Id, 1000m, PaymentMethod.Cash);

        // Act & Assert
        var ex = await Should.ThrowAsync<DomainRuleException>(() => useCase.ExecuteAsync(request));
        ex.Code.ShouldBe("INVOICE_VOID");
    }

    [Fact]
    public async Task RecordPayment_WhenAmountExceedsBalance_ShouldThrowOverpayment()
    {
        // Arrange
        var invoice = CreateTestInvoice(5000m);
        var invoiceSet = MockDbSetHelper.CreateMockDbSet([invoice]);
        _dbContext.Invoices.Returns(invoiceSet);

        var validator = new RecordPaymentValidator();
        var useCase = new RecordPaymentUseCase(_dbContext, _currentUser, _dateTimeProvider, validator);
        var request = new RecordPaymentRequest(invoice.Id, 5500m, PaymentMethod.BankTransfer);

        // Act & Assert
        var ex = await Should.ThrowAsync<DomainRuleException>(() => useCase.ExecuteAsync(request));
        ex.Code.ShouldBe("OVERPAYMENT");
    }

    [Fact]
    public async Task RecordPayment_WhenDuplicateActiveReferenceExists_ShouldThrowDuplicateReference()
    {
        // Arrange
        var invoice = CreateTestInvoice(5000m);
        var existingPayment = new Payment(
            invoice.Id,
            2000m,
            PaymentMethod.BankTransfer,
            _dateTimeProvider.UtcNow,
            _userId,
            "Admin",
            "REF-999");
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(existingPayment, Guid.NewGuid());

        var invoiceSet = MockDbSetHelper.CreateMockDbSet([invoice]);
        var paymentSet = MockDbSetHelper.CreateMockDbSet([existingPayment]);
        _dbContext.Invoices.Returns(invoiceSet);
        _dbContext.Payments.Returns(paymentSet);

        var validator = new RecordPaymentValidator();
        var useCase = new RecordPaymentUseCase(_dbContext, _currentUser, _dateTimeProvider, validator);
        var request = new RecordPaymentRequest(invoice.Id, 1500m, PaymentMethod.BankTransfer, "REF-999");

        // Act & Assert
        var ex = await Should.ThrowAsync<DomainRuleException>(() => useCase.ExecuteAsync(request));
        ex.Code.ShouldBe("DUPLICATE_REFERENCE");
    }

    [Fact]
    public async Task RecordPayment_WithPartialAmount_ShouldUpdateStatusToPartiallyPaid()
    {
        // Arrange
        var invoice = CreateTestInvoice(5000m);
        var invoiceSet = MockDbSetHelper.CreateMockDbSet([invoice]);
        var paymentSet = MockDbSetHelper.CreateMockDbSet<Payment>([]);
        _dbContext.Invoices.Returns(invoiceSet);
        _dbContext.Payments.Returns(paymentSet);

        var validator = new RecordPaymentValidator();
        var useCase = new RecordPaymentUseCase(_dbContext, _currentUser, _dateTimeProvider, validator);
        var request = new RecordPaymentRequest(invoice.Id, 2000m, PaymentMethod.Cash, "REC-001");

        // Act
        var result = await useCase.ExecuteAsync(request);

        // Assert
        result.AmountPaid.ShouldBe(2000m);
        result.Balance.ShouldBe(3000m);
        result.Status.ShouldBe(InvoiceStatus.PartiallyPaid);
        invoice.Status.ShouldBe(InvoiceStatus.PartiallyPaid);
    }

    [Fact]
    public async Task RecordPayment_WithFullAmount_ShouldUpdateStatusToPaid()
    {
        // Arrange
        var invoice = CreateTestInvoice(5000m);
        var invoiceSet = MockDbSetHelper.CreateMockDbSet([invoice]);
        var paymentSet = MockDbSetHelper.CreateMockDbSet<Payment>([]);
        _dbContext.Invoices.Returns(invoiceSet);
        _dbContext.Payments.Returns(paymentSet);

        var validator = new RecordPaymentValidator();
        var useCase = new RecordPaymentUseCase(_dbContext, _currentUser, _dateTimeProvider, validator);
        var request = new RecordPaymentRequest(invoice.Id, 5000m, PaymentMethod.BankTransfer, "REF-FULL");

        // Act
        var result = await useCase.ExecuteAsync(request);

        // Assert
        result.AmountPaid.ShouldBe(5000m);
        result.Balance.ShouldBe(0m);
        result.Status.ShouldBe(InvoiceStatus.Paid);
        invoice.Status.ShouldBe(InvoiceStatus.Paid);
    }

    [Fact]
    public async Task VoidPayment_WhenPaymentAlreadyVoided_ShouldThrowPaymentAlreadyVoided()
    {
        // Arrange
        var invoice = CreateTestInvoice(5000m);
        var payment = new Payment(
            invoice.Id,
            2000m,
            PaymentMethod.Cash,
            _dateTimeProvider.UtcNow,
            _userId,
            "Admin",
            "REC-001");
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(payment, Guid.NewGuid());
        payment.Void("Original void reason", _userId, "Admin", _dateTimeProvider.UtcNow);

        var paymentSet = MockDbSetHelper.CreateMockDbSet([payment]);
        _dbContext.Payments.Returns(paymentSet);

        var validator = new VoidPaymentValidator();
        var useCase = new VoidPaymentUseCase(_dbContext, _currentUser, _dateTimeProvider, validator);
        var request = new VoidPaymentRequest(payment.Id, "Attempting second void");

        // Act & Assert
        var ex = await Should.ThrowAsync<DomainRuleException>(() => useCase.ExecuteAsync(request));
        ex.Code.ShouldBe("PAYMENT_ALREADY_VOIDED");
    }

    [Fact]
    public async Task VoidPayment_WhenValid_ShouldRestoreInvoiceBalance()
    {
        // Arrange
        var invoice = CreateTestInvoice(5000m);
        invoice.ApplyPayment(5000m);
        invoice.Status.ShouldBe(InvoiceStatus.Paid);

        var payment = new Payment(
            invoice.Id,
            5000m,
            PaymentMethod.BankTransfer,
            _dateTimeProvider.UtcNow,
            _userId,
            "Admin",
            "REF-VOID-TEST");
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(payment, Guid.NewGuid());
        typeof(Payment).GetProperty(nameof(Payment.Invoice))!.SetValue(payment, invoice);

        var paymentSet = MockDbSetHelper.CreateMockDbSet([payment]);
        _dbContext.Payments.Returns(paymentSet);

        var validator = new VoidPaymentValidator();
        var useCase = new VoidPaymentUseCase(_dbContext, _currentUser, _dateTimeProvider, validator);
        var request = new VoidPaymentRequest(payment.Id, "Bounced check / chargeback");

        // Act
        var result = await useCase.ExecuteAsync(request);

        // Assert
        result.VoidedAt.ShouldNotBeNull();
        result.VoidReason.ShouldBe("Bounced check / chargeback");
        payment.VoidedAt.ShouldNotBeNull();

        invoice.AmountPaid.ShouldBe(0m);
        invoice.Balance.ShouldBe(5000m);
        invoice.Status.ShouldBe(InvoiceStatus.Issued);
    }

    [Fact]
    public async Task ProcessOnlinePayment_WhenGatewaySucceeds_ShouldRecordPaymentAndMarkPaid()
    {
        // Arrange
        var invoice = CreateTestInvoice(4000m);
        _currentUser.GuardianId.Returns(_guardianId);
        _currentUser.Role.Returns("Parent");
        _currentUser.FullName.Returns("Parent One");

        var invoiceSet = MockDbSetHelper.CreateMockDbSet([invoice]);
        var paymentSet = MockDbSetHelper.CreateMockDbSet<Payment>([]);
        _dbContext.Invoices.Returns(invoiceSet);
        _dbContext.Payments.Returns(paymentSet);

        _paymentGateway.ProcessPaymentAsync(4000m, "ETB", invoice.InvoiceNumber, Arg.Any<CancellationToken>())
            .Returns(new PaymentGatewayResult(true, "FAKE-GW-12345", null));

        var validator = new ProcessOnlinePaymentValidator();
        var useCase = new ProcessOnlinePaymentUseCase(
            _dbContext, _currentUser, _dateTimeProvider, _paymentGateway, validator);

        // Act
        var result = await useCase.ExecuteAsync(new ProcessOnlinePaymentRequest(invoice.Id));

        // Assert
        result.Status.ShouldBe(InvoiceStatus.Paid);
        result.Balance.ShouldBe(0m);
        result.AmountPaid.ShouldBe(4000m);
        invoice.Status.ShouldBe(InvoiceStatus.Paid);
    }

    [Fact]
    public async Task ProcessOnlinePayment_WhenGatewayFails_ShouldThrowDomainRuleException()
    {
        // Arrange
        var invoice = CreateTestInvoice(4000m);
        _currentUser.GuardianId.Returns(_guardianId);
        _currentUser.Role.Returns("Parent");

        var invoiceSet = MockDbSetHelper.CreateMockDbSet([invoice]);
        _dbContext.Invoices.Returns(invoiceSet);

        _paymentGateway.ProcessPaymentAsync(4000m, "ETB", invoice.InvoiceNumber, Arg.Any<CancellationToken>())
            .Returns(new PaymentGatewayResult(false, null, "Card declined: insufficient funds"));

        var validator = new ProcessOnlinePaymentValidator();
        var useCase = new ProcessOnlinePaymentUseCase(
            _dbContext, _currentUser, _dateTimeProvider, _paymentGateway, validator);

        // Act & Assert
        var ex = await Should.ThrowAsync<DomainRuleException>(() =>
            useCase.ExecuteAsync(new ProcessOnlinePaymentRequest(invoice.Id)));
        ex.Message.ShouldContain("Card declined");
        invoice.AmountPaid.ShouldBe(0m);
        invoice.Balance.ShouldBe(4000m);
    }

    [Fact]
    public async Task ProcessOnlinePayment_WhenParentTriesToPayAnotherParentsInvoice_ShouldThrowForbidden()
    {
        // Arrange
        var invoice = CreateTestInvoice(4000m);
        var otherGuardianId = Guid.NewGuid();
        _currentUser.GuardianId.Returns(otherGuardianId);
        _currentUser.Role.Returns("Parent");

        var invoiceSet = MockDbSetHelper.CreateMockDbSet([invoice]);
        _dbContext.Invoices.Returns(invoiceSet);

        var validator = new ProcessOnlinePaymentValidator();
        var useCase = new ProcessOnlinePaymentUseCase(
            _dbContext, _currentUser, _dateTimeProvider, _paymentGateway, validator);

        // Act & Assert
        var ex = await Should.ThrowAsync<ForbiddenException>(() =>
            useCase.ExecuteAsync(new ProcessOnlinePaymentRequest(invoice.Id)));
        ex.Message.ShouldContain("Parents may only pay their own invoices");
        invoice.AmountPaid.ShouldBe(0m);
    }
}

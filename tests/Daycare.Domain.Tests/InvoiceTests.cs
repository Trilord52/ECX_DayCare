using Daycare.Domain.Entities;
using Daycare.Domain.Enums;
using Daycare.Domain.Exceptions;
using Shouldly;
using Xunit;

namespace Daycare.Domain.Tests;

public class InvoiceTests
{
    private readonly Guid _childId = Guid.NewGuid();
    private readonly Guid _guardianId = Guid.NewGuid();
    private readonly DateOnly _periodStart = new(2026, 10, 1);
    private readonly DateOnly _periodEnd = new(2026, 10, 31);
    private readonly DateOnly _issueDate = new(2026, 10, 1);
    private readonly DateOnly _dueDate = new(2026, 10, 11);

    private Invoice CreateTestInvoice()
    {
        return new Invoice(
            "INV-202610-00001",
            _childId,
            _guardianId,
            _periodStart,
            _periodEnd,
            _issueDate,
            _dueDate,
            "ETB");
    }

    [Fact]
    public void AddLine_ShouldRecalculateTotalAmount()
    {
        var invoice = CreateTestInvoice();

        invoice.AddLine(InvoiceLineType.Tuition, "Full-time Tuition", 1, 4500m);
        invoice.AddLine(InvoiceLineType.LateFee, "Late pickup fee", 2, 50m);

        invoice.TotalAmount.ShouldBe(4600m);
        invoice.Balance.ShouldBe(4600m);
        invoice.Status.ShouldBe(InvoiceStatus.Issued);
    }

    [Fact]
    public void AddLine_NegativePriceForNonAdjustment_ShouldThrowValidationFailed()
    {
        var invoice = CreateTestInvoice();

        var ex = Should.Throw<DomainRuleException>(() =>
            invoice.AddLine(InvoiceLineType.Tuition, "Invalid negative tuition", 1, -500m));

        ex.Code.ShouldBe("VALIDATION_FAILED");
    }

    [Fact]
    public void AddLine_NegativePriceForAdjustment_ShouldBeAllowed()
    {
        var invoice = CreateTestInvoice();
        invoice.AddLine(InvoiceLineType.Tuition, "Tuition", 1, 4500m);
        invoice.AddLine(InvoiceLineType.Adjustment, "Sibling discount adjustment", 1, -500m);

        invoice.TotalAmount.ShouldBe(4000m);
        invoice.Balance.ShouldBe(4000m);
    }

    [Fact]
    public void ApplyPayment_Partial_ShouldSetPartiallyPaid()
    {
        var invoice = CreateTestInvoice();
        invoice.AddLine(InvoiceLineType.Tuition, "Tuition", 1, 4000m);

        invoice.ApplyPayment(1500m);

        invoice.AmountPaid.ShouldBe(1500m);
        invoice.Balance.ShouldBe(2500m);
        invoice.Status.ShouldBe(InvoiceStatus.PartiallyPaid);
    }

    [Fact]
    public void ApplyPayment_Full_ShouldSetPaid()
    {
        var invoice = CreateTestInvoice();
        invoice.AddLine(InvoiceLineType.Tuition, "Tuition", 1, 4000m);

        invoice.ApplyPayment(4000m);

        invoice.AmountPaid.ShouldBe(4000m);
        invoice.Balance.ShouldBe(0m);
        invoice.Status.ShouldBe(InvoiceStatus.Paid);
    }

    [Fact]
    public void ApplyPayment_Overpayment_ShouldThrowOverpaymentException()
    {
        var invoice = CreateTestInvoice();
        invoice.AddLine(InvoiceLineType.Tuition, "Tuition", 1, 4000m);

        var ex = Should.Throw<DomainRuleException>(() =>
            invoice.ApplyPayment(4500m));

        ex.Code.ShouldBe("OVERPAYMENT");
    }

    [Fact]
    public void VoidPayment_ShouldReduceAmountPaidAndAdjustStatus()
    {
        var invoice = CreateTestInvoice();
        invoice.AddLine(InvoiceLineType.Tuition, "Tuition", 1, 4000m);
        invoice.ApplyPayment(4000m);
        invoice.Status.ShouldBe(InvoiceStatus.Paid);

        invoice.VoidPayment(1000m);

        invoice.AmountPaid.ShouldBe(3000m);
        invoice.Balance.ShouldBe(1000m);
        invoice.Status.ShouldBe(InvoiceStatus.PartiallyPaid);

        invoice.VoidPayment(3000m);

        invoice.AmountPaid.ShouldBe(0m);
        invoice.Balance.ShouldBe(4000m);
        invoice.Status.ShouldBe(InvoiceStatus.Issued);
    }

    [Fact]
    public void Void_WhenNoPayments_ShouldSucceed()
    {
        var invoice = CreateTestInvoice();
        invoice.AddLine(InvoiceLineType.Tuition, "Tuition", 1, 4000m);
        var voidedAt = DateTime.UtcNow;

        invoice.Void("Child withdrawn", voidedAt);

        invoice.Status.ShouldBe(InvoiceStatus.Void);
        invoice.VoidReason.ShouldBe("Child withdrawn");
        invoice.VoidedAt.ShouldBe(voidedAt);
    }

    [Fact]
    public void Void_WhenHasPayments_ShouldThrowInvoiceHasPayments()
    {
        var invoice = CreateTestInvoice();
        invoice.AddLine(InvoiceLineType.Tuition, "Tuition", 1, 4000m);
        invoice.ApplyPayment(500m);

        var ex = Should.Throw<DomainRuleException>(() =>
            invoice.Void("Cannot void", DateTime.UtcNow));

        ex.Code.ShouldBe("INVOICE_HAS_PAYMENTS");
    }

    [Fact]
    public void IsOverdue_ShouldComputeAccurately()
    {
        var invoice = CreateTestInvoice();
        invoice.AddLine(InvoiceLineType.Tuition, "Tuition", 1, 4000m);

        // Due date is 2026-10-11
        invoice.IsOverdue(new DateOnly(2026, 10, 10)).ShouldBeFalse();
        invoice.IsOverdue(new DateOnly(2026, 10, 11)).ShouldBeFalse();
        invoice.IsOverdue(new DateOnly(2026, 10, 12)).ShouldBeTrue();

        invoice.ApplyPayment(4000m);
        invoice.IsOverdue(new DateOnly(2026, 10, 12)).ShouldBeFalse();
    }
}

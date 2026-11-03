using Daycare.Domain.Common;
using Daycare.Domain.Enums;
using Daycare.Domain.Exceptions;

namespace Daycare.Domain.Entities;

public class Invoice : BaseEntity
{
    public string InvoiceNumber { get; private set; } = null!;
    public Guid ChildId { get; private set; }
    public Guid GuardianId { get; private set; }
    public DateOnly PeriodStart { get; private set; }
    public DateOnly PeriodEnd { get; private set; }
    public DateOnly IssueDate { get; private set; }
    public DateOnly DueDate { get; private set; }
    public InvoiceStatus Status { get; private set; } = InvoiceStatus.Issued;
    public string Currency { get; private set; } = "ETB";
    public decimal TotalAmount { get; private set; }
    public decimal AmountPaid { get; private set; }
    public string? VoidReason { get; private set; }
    public DateTime? VoidedAt { get; private set; }

    // Navigations
    public Child Child { get; private set; } = null!;
    public Guardian Guardian { get; private set; } = null!;
    public ICollection<InvoiceLine> Lines { get; private set; } = new List<InvoiceLine>();
    public ICollection<Payment> Payments { get; private set; } = new List<Payment>();

    // Computed
    public decimal Balance => TotalAmount - AmountPaid;

    public bool IsOverdue(DateOnly today) =>
        DueDate < today && (Status == InvoiceStatus.Issued || Status == InvoiceStatus.PartiallyPaid);

    private Invoice() { }

    public Invoice(
        string invoiceNumber,
        Guid childId,
        Guid guardianId,
        DateOnly periodStart,
        DateOnly periodEnd,
        DateOnly issueDate,
        DateOnly dueDate,
        string currency = "ETB")
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber))
            throw new DomainRuleException("VALIDATION_FAILED", "Invoice number is required.");

        InvoiceNumber = invoiceNumber.Trim();
        ChildId = childId;
        GuardianId = guardianId;
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
        IssueDate = issueDate;
        DueDate = dueDate;
        Currency = string.IsNullOrWhiteSpace(currency) ? "ETB" : currency.Trim().ToUpperInvariant();
        Status = InvoiceStatus.Issued;
        TotalAmount = 0m;
        AmountPaid = 0m;
    }

    public InvoiceLine AddLine(InvoiceLineType lineType, string description, int quantity, decimal unitPrice, int? sortOrder = null)
    {
        if (Status == InvoiceStatus.Void)
            throw new DomainRuleException("INVOICE_VOID", "Cannot add lines to a void invoice.");

        int order = sortOrder ?? Lines.Count;
        var line = new InvoiceLine(Id, lineType, description, quantity, unitPrice, order);
        Lines.Add(line);

        RecalculateTotal();
        return line;
    }

    public void RecalculateTotal()
    {
        TotalAmount = decimal.Round(Lines.Sum(l => l.LineTotal), 2, MidpointRounding.AwayFromZero);
        if (TotalAmount < 0)
            throw new DomainRuleException("VALIDATION_FAILED", "Invoice total amount cannot be negative.");

        UpdateStatusAfterBalanceChange();
    }

    public void ApplyPayment(decimal paymentAmount)
    {
        if (Status == InvoiceStatus.Void)
            throw new DomainRuleException("INVOICE_VOID", "Cannot apply payment to a void invoice.");
        if (paymentAmount <= 0)
            throw new DomainRuleException("VALIDATION_FAILED", "Payment amount must be greater than zero.");

        decimal roundedPayment = decimal.Round(paymentAmount, 2, MidpointRounding.AwayFromZero);
        if (AmountPaid + roundedPayment > TotalAmount)
            throw new DomainRuleException("OVERPAYMENT", $"Payment of {roundedPayment} exceeds remaining balance of {Balance}.");

        AmountPaid += roundedPayment;
        UpdateStatusAfterBalanceChange();
    }

    public void VoidPayment(decimal paymentAmount)
    {
        if (Status == InvoiceStatus.Void)
            throw new DomainRuleException("INVOICE_VOID", "Cannot void payment for a void invoice.");

        decimal roundedPayment = decimal.Round(paymentAmount, 2, MidpointRounding.AwayFromZero);
        AmountPaid = Math.Max(0m, AmountPaid - roundedPayment);
        UpdateStatusAfterBalanceChange();
    }

    public void Void(string reason, DateTime voidedAt)
    {
        if (Status == InvoiceStatus.Void)
            throw new DomainRuleException("INVOICE_VOID", "Invoice is already void.");
        if (AmountPaid > 0)
            throw new DomainRuleException("INVOICE_HAS_PAYMENTS", "Cannot void an invoice with recorded payments.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainRuleException("VALIDATION_FAILED", "Void reason is required.");

        Status = InvoiceStatus.Void;
        VoidReason = reason.Trim();
        VoidedAt = voidedAt;
    }

    private void UpdateStatusAfterBalanceChange()
    {
        if (Status == InvoiceStatus.Void)
            return;

        if (TotalAmount > 0 && AmountPaid >= TotalAmount)
        {
            Status = InvoiceStatus.Paid;
        }
        else if (AmountPaid > 0)
        {
            Status = InvoiceStatus.PartiallyPaid;
        }
        else
        {
            Status = InvoiceStatus.Issued;
        }
    }
}

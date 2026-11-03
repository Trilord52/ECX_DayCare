using Daycare.Domain.Common;
using Daycare.Domain.Enums;
using Daycare.Domain.Exceptions;

namespace Daycare.Domain.Entities;

public class Payment : BaseEntity
{
    public Guid InvoiceId { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentMethod Method { get; private set; }
    public string? Reference { get; private set; }
    public DateTime PaidAt { get; private set; }
    public string? Notes { get; private set; }
    public Guid RecordedByUserId { get; private set; }
    public string RecordedByName { get; private set; } = null!;
    public DateTime? VoidedAt { get; private set; }
    public Guid? VoidedByUserId { get; private set; }
    public string? VoidReason { get; private set; }
    public string? VoidedByName { get; private set; }

    // Navigations
    public Invoice Invoice { get; private set; } = null!;

    private Payment() { }

    public Payment(
        Guid invoiceId,
        decimal amount,
        PaymentMethod method,
        DateTime paidAt,
        Guid recordedByUserId,
        string recordedByName,
        string? reference = null,
        string? notes = null)
    {
        if (amount <= 0)
            throw new DomainRuleException("VALIDATION_FAILED", "Payment amount must be greater than zero.");
        if (paidAt.Kind != DateTimeKind.Utc)
            throw new ArgumentException("PaidAt timestamp must be UTC.", nameof(paidAt));
        if (string.IsNullOrWhiteSpace(recordedByName))
            throw new DomainRuleException("VALIDATION_FAILED", "Recorded by name is required.");

        InvoiceId = invoiceId;
        Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        Method = method;
        Reference = reference?.Trim();
        PaidAt = paidAt;
        RecordedByUserId = recordedByUserId;
        RecordedByName = recordedByName.Trim();
        Notes = notes?.Trim();
    }

    public void Void(string reason, Guid voidedByUserId, string voidedByName, DateTime voidedAt)
    {
        if (VoidedAt.HasValue)
            throw new DomainRuleException("PAYMENT_ALREADY_VOIDED", "Payment is already voided.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainRuleException("VALIDATION_FAILED", "Void reason is required.");
        if (string.IsNullOrWhiteSpace(voidedByName))
            throw new DomainRuleException("VALIDATION_FAILED", "Voided by name is required.");

        VoidReason = reason.Trim();
        VoidedByUserId = voidedByUserId;
        VoidedByName = voidedByName.Trim();
        VoidedAt = voidedAt;
    }
}

using Daycare.Domain.Common;
using Daycare.Domain.Enums;
using Daycare.Domain.Exceptions;

namespace Daycare.Domain.Entities;

public class InvoiceLine : BaseEntity
{
    public Guid InvoiceId { get; private set; }
    public InvoiceLineType LineType { get; private set; }
    public string Description { get; private set; } = null!;
    public int Quantity { get; private set; } = 1;
    public decimal UnitPrice { get; private set; }
    public decimal LineTotal { get; private set; }
    public int SortOrder { get; private set; }

    // Navigations
    public Invoice Invoice { get; private set; } = null!;

    private InvoiceLine() { }

    public InvoiceLine(
        Guid invoiceId,
        InvoiceLineType lineType,
        string description,
        int quantity,
        decimal unitPrice,
        int sortOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new DomainRuleException("VALIDATION_FAILED", "Invoice line description is required.");
        if (quantity <= 0)
            throw new DomainRuleException("VALIDATION_FAILED", "Quantity must be greater than zero.");
        if (lineType != InvoiceLineType.Adjustment && unitPrice < 0)
            throw new DomainRuleException("VALIDATION_FAILED", "Unit price can only be negative for adjustment lines.");

        InvoiceId = invoiceId;
        LineType = lineType;
        Description = description.Trim();
        Quantity = quantity;
        UnitPrice = decimal.Round(unitPrice, 2, MidpointRounding.AwayFromZero);
        LineTotal = decimal.Round(quantity * UnitPrice, 2, MidpointRounding.AwayFromZero);
        SortOrder = sortOrder;
    }
}

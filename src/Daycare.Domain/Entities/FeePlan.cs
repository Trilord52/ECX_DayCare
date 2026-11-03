using Daycare.Domain.Common;
using Daycare.Domain.Exceptions;

namespace Daycare.Domain.Entities;

public class FeePlan : BaseEntity
{
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public decimal MonthlyAmount { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Navigations
    public ICollection<Child> Children { get; private set; } = new List<Child>();

    private FeePlan() { }

    public FeePlan(string name, string? description, decimal monthlyAmount, bool isActive = true)
    {
        Update(name, description, monthlyAmount, isActive);
    }

    public void Update(string name, string? description, decimal monthlyAmount, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainRuleException("VALIDATION_FAILED", "Fee plan name is required.");
        if (monthlyAmount < 0)
            throw new DomainRuleException("VALIDATION_FAILED", "Monthly amount cannot be negative.");

        Name = name.Trim();
        Description = description?.Trim();
        MonthlyAmount = decimal.Round(monthlyAmount, 2, MidpointRounding.AwayFromZero);
        IsActive = isActive;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }
}

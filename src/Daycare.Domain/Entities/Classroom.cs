using Daycare.Domain.Common;
using Daycare.Domain.Exceptions;

namespace Daycare.Domain.Entities;

public class Classroom : BaseEntity
{
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public int MinAgeMonths { get; private set; }
    public int MaxAgeMonths { get; private set; }
    public int Capacity { get; private set; }
    public int MaxChildrenPerStaff { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Navigation properties
    public ICollection<Child> Children { get; private set; } = new List<Child>();
    public ICollection<StaffClassroom> StaffClassrooms { get; private set; } = new List<StaffClassroom>();

    private Classroom() { }

    public Classroom(
        string name,
        string? description,
        int minAgeMonths,
        int maxAgeMonths,
        int capacity,
        int maxChildrenPerStaff,
        bool isActive = true)
    {
        Update(name, description, minAgeMonths, maxAgeMonths, capacity, maxChildrenPerStaff, isActive);
    }

    public void Update(
        string name,
        string? description,
        int minAgeMonths,
        int maxAgeMonths,
        int capacity,
        int maxChildrenPerStaff,
        bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainRuleException("VALIDATION_FAILED", "Classroom name is required.");
        if (minAgeMonths < 0)
            throw new DomainRuleException("VALIDATION_FAILED", "Minimum age in months cannot be negative.");
        if (maxAgeMonths < minAgeMonths)
            throw new DomainRuleException("VALIDATION_FAILED", "Maximum age cannot be less than minimum age.");
        if (capacity <= 0)
            throw new DomainRuleException("VALIDATION_FAILED", "Capacity must be greater than zero.");
        if (maxChildrenPerStaff <= 0)
            throw new DomainRuleException("VALIDATION_FAILED", "Max children per staff must be greater than zero.");

        Name = name.Trim();
        Description = description?.Trim();
        MinAgeMonths = minAgeMonths;
        MaxAgeMonths = maxAgeMonths;
        Capacity = capacity;
        MaxChildrenPerStaff = maxChildrenPerStaff;
        IsActive = isActive;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }

    public bool IsOverRatio(int presentCount, int staffCount)
    {
        if (staffCount <= 0)
            return presentCount > 0;

        return presentCount > staffCount * MaxChildrenPerStaff;
    }
}

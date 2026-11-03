using Daycare.Domain.Common;
using Daycare.Domain.Enums;

namespace Daycare.Domain.Entities;

public class ChildGuardian : BaseEntity
{
    public Guid ChildId { get; private set; }
    public Guid GuardianId { get; private set; }
    public Relationship Relationship { get; private set; }
    public bool IsPrimaryContact { get; private set; }
    public bool CanPickUp { get; private set; }
    public bool IsPayer { get; private set; }
    public bool IsEmergencyContact { get; private set; }

    // Navigations
    public Child Child { get; private set; } = null!;
    public Guardian Guardian { get; private set; } = null!;

    private ChildGuardian() { }

    public ChildGuardian(
        Guid childId,
        Guid guardianId,
        Relationship relationship,
        bool isPrimaryContact,
        bool canPickUp,
        bool isPayer,
        bool isEmergencyContact)
    {
        ChildId = childId;
        GuardianId = guardianId;
        Relationship = relationship;
        IsPrimaryContact = isPrimaryContact;
        CanPickUp = canPickUp;
        IsPayer = isPayer;
        IsEmergencyContact = isEmergencyContact;
    }

    public void Update(
        Relationship relationship,
        bool isPrimaryContact,
        bool canPickUp,
        bool isPayer,
        bool isEmergencyContact)
    {
        Relationship = relationship;
        IsPrimaryContact = isPrimaryContact;
        CanPickUp = canPickUp;
        IsPayer = isPayer;
        IsEmergencyContact = isEmergencyContact;
    }
}

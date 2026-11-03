using Daycare.Domain.Common;
using Daycare.Domain.Exceptions;
using Daycare.Domain.ValueObjects;

namespace Daycare.Domain.Entities;

public class Guardian : BaseEntity, ISoftDeletable
{
    public Guid? UserId { get; private set; }
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string Phone { get; private set; } = null!;
    public string? Email { get; private set; }
    public string? Address { get; private set; }
    public ProfilePhoto? Photo { get; private set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navigations
    public ICollection<ChildGuardian> Children { get; private set; } = new List<ChildGuardian>();

    private Guardian() { }

    public Guardian(
        string firstName,
        string lastName,
        string phone,
        string? email = null,
        string? address = null,
        Guid? userId = null)
    {
        Update(firstName, lastName, phone, email, address);
        UserId = userId;
    }

    public void Update(
        string firstName,
        string lastName,
        string phone,
        string? email = null,
        string? address = null)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new DomainRuleException("VALIDATION_FAILED", "First name is required.");
        if (string.IsNullOrWhiteSpace(lastName))
            throw new DomainRuleException("VALIDATION_FAILED", "Last name is required.");
        if (string.IsNullOrWhiteSpace(phone))
            throw new DomainRuleException("VALIDATION_FAILED", "Phone is required.");

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Phone = phone.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
        Address = address?.Trim();
    }

    public void LinkUser(Guid userId)
    {
        UserId = userId;
    }

    public void UnlinkUser()
    {
        UserId = null;
    }

    public string? SetPhoto(ProfilePhoto photo)
    {
        ArgumentNullException.ThrowIfNull(photo);
        var previousKey = Photo?.Key;
        Photo = photo;
        return previousKey;
    }

    public string? RemovePhoto()
    {
        if (Photo is null)
            return null;

        var previousKey = Photo.Key;
        Photo = null;
        return previousKey;
    }

    public void SoftDelete(DateTime utcNow)
    {
        IsDeleted = true;
        DeletedAt = utcNow;
    }
}

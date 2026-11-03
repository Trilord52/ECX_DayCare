using Daycare.Domain.Common;
using Daycare.Domain.Exceptions;
using Daycare.Domain.ValueObjects;

namespace Daycare.Domain.Entities;

public class Staff : BaseEntity, ISoftDeletable
{
    public Guid? UserId { get; private set; }
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string Phone { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string JobTitle { get; private set; } = null!;
    public DateOnly HireDate { get; private set; }
    public bool IsActive { get; private set; } = true;
    public ProfilePhoto? Photo { get; private set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navigations
    public ICollection<StaffClassroom> StaffClassrooms { get; private set; } = new List<StaffClassroom>();

    private Staff() { }

    public Staff(
        string firstName,
        string lastName,
        string phone,
        string email,
        string jobTitle,
        DateOnly hireDate,
        Guid? userId = null,
        bool isActive = true)
    {
        Update(firstName, lastName, phone, email, jobTitle, hireDate, isActive);
        UserId = userId;
    }

    public void Update(
        string firstName,
        string lastName,
        string phone,
        string email,
        string jobTitle,
        DateOnly hireDate,
        bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new DomainRuleException("VALIDATION_FAILED", "First name is required.");
        if (string.IsNullOrWhiteSpace(lastName))
            throw new DomainRuleException("VALIDATION_FAILED", "Last name is required.");
        if (string.IsNullOrWhiteSpace(phone))
            throw new DomainRuleException("VALIDATION_FAILED", "Phone is required.");
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainRuleException("VALIDATION_FAILED", "Email is required.");
        if (string.IsNullOrWhiteSpace(jobTitle))
            throw new DomainRuleException("VALIDATION_FAILED", "Job title is required.");

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Phone = phone.Trim();
        Email = email.Trim().ToLowerInvariant();
        JobTitle = jobTitle.Trim();
        HireDate = hireDate;
        IsActive = isActive;
    }

    public void LinkUser(Guid userId)
    {
        UserId = userId;
    }

    public void UnlinkUser()
    {
        UserId = null;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Reactivate()
    {
        IsActive = true;
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
        IsActive = false;
    }
}

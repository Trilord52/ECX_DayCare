using Daycare.Domain.Common;
using Daycare.Domain.Enums;
using Daycare.Domain.Exceptions;
using Daycare.Domain.ValueObjects;

namespace Daycare.Domain.Entities;

public class Child : BaseEntity, ISoftDeletable
{
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public DateOnly DateOfBirth { get; private set; }
    public Gender Gender { get; private set; }
    public Guid? ClassroomId { get; private set; }
    public Guid? FeePlanId { get; private set; }
    public ChildStatus Status { get; private set; } = ChildStatus.Active;
    public DateOnly EnrollmentDate { get; private set; }
    public string? Allergies { get; private set; }
    public string? MedicalNotes { get; private set; }
    public string? DietaryNotes { get; private set; }
    public ProfilePhoto? Photo { get; private set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navigations
    public Classroom? Classroom { get; private set; }
    public FeePlan? FeePlan { get; private set; }
    public ICollection<ChildGuardian> Guardians { get; private set; } = new List<ChildGuardian>();
    public ICollection<AttendanceRecord> AttendanceRecords { get; private set; } = new List<AttendanceRecord>();
    public ICollection<DailyReport> DailyReports { get; private set; } = new List<DailyReport>();
    public ICollection<Incident> Incidents { get; private set; } = new List<Incident>();
    public ICollection<Invoice> Invoices { get; private set; } = new List<Invoice>();

    private Child() { }

    public Child(
        string firstName,
        string lastName,
        DateOnly dateOfBirth,
        Gender gender,
        DateOnly enrollmentDate,
        Guid? classroomId = null,
        Guid? feePlanId = null,
        string? allergies = null,
        string? medicalNotes = null,
        string? dietaryNotes = null)
    {
        Update(firstName, lastName, dateOfBirth, gender, enrollmentDate, allergies, medicalNotes, dietaryNotes);
        ClassroomId = classroomId;
        FeePlanId = feePlanId;
        Status = ChildStatus.Active;
    }

    public void Update(
        string firstName,
        string lastName,
        DateOnly dateOfBirth,
        Gender gender,
        DateOnly enrollmentDate,
        string? allergies = null,
        string? medicalNotes = null,
        string? dietaryNotes = null)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new DomainRuleException("VALIDATION_FAILED", "First name is required.");
        if (string.IsNullOrWhiteSpace(lastName))
            throw new DomainRuleException("VALIDATION_FAILED", "Last name is required.");
        if (dateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow))
            throw new DomainRuleException("VALIDATION_FAILED", "Date of birth cannot be in the future.");

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        DateOfBirth = dateOfBirth;
        Gender = gender;
        EnrollmentDate = enrollmentDate;
        Allergies = allergies?.Trim();
        MedicalNotes = medicalNotes?.Trim();
        DietaryNotes = dietaryNotes?.Trim();
    }

    public void AssignClassroom(Guid? classroomId)
    {
        ClassroomId = classroomId;
    }

    public void AssignFeePlan(Guid? feePlanId)
    {
        FeePlanId = feePlanId;
    }

    public void Withdraw()
    {
        Status = ChildStatus.Withdrawn;
    }

    public void Reactivate()
    {
        Status = ChildStatus.Active;
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

    public int CalculateAgeInMonths(DateOnly asOfDate)
    {
        int months = (asOfDate.Year - DateOfBirth.Year) * 12 + asOfDate.Month - DateOfBirth.Month;
        if (asOfDate.Day < DateOfBirth.Day)
        {
            months--;
        }
        return Math.Max(0, months);
    }
}

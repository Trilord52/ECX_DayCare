namespace Daycare.Domain.Enums;

public enum Gender
{
    Male,
    Female
}

public enum ChildStatus
{
    Active,
    Withdrawn
}

public enum Relationship
{
    Mother,
    Father,
    Grandparent,
    Sibling,
    Relative,
    Other
}

public enum AttendanceStatus
{
    Present,
    Absent
}

public enum Mood
{
    Happy,
    Calm,
    Tired,
    Fussy,
    Upset
}

public enum EntryType
{
    Meal,
    Nap,
    Diaper,
    Activity,
    Medication,
    Note
}

public enum MealType
{
    Breakfast,
    Lunch,
    Snack,
    Bottle
}

public enum AmountEaten
{
    None,
    Some,
    Most,
    All
}

public enum DiaperType
{
    Dry,
    Wet,
    Soiled,
    Both
}

public enum IncidentType
{
    Injury,
    Illness,
    Behavioral,
    AllergicReaction,
    Other
}

public enum IncidentSeverity
{
    Minor,
    Moderate,
    Serious
}

public enum AnnouncementAudience
{
    Everyone,
    Parents,
    Staff,
    Classroom
}

public enum InvoiceStatus
{
    Issued,
    PartiallyPaid,
    Paid,
    Void
}

public enum InvoiceLineType
{
    Tuition,
    LateFee,
    Adjustment,
    Other
}

public enum PaymentMethod
{
    Cash,
    BankTransfer,
    Telebirr,
    Online,
    Other
}

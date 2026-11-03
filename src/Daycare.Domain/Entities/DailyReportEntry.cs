using Daycare.Domain.Common;
using Daycare.Domain.Enums;
using Daycare.Domain.Exceptions;

namespace Daycare.Domain.Entities;

public class DailyReportEntry : BaseEntity
{
    public Guid DailyReportId { get; private set; }
    public EntryType EntryType { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public DateTime? EndedAt { get; private set; }
    public MealType? MealType { get; private set; }
    public AmountEaten? AmountEaten { get; private set; }
    public DiaperType? DiaperType { get; private set; }
    public string? Description { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public string CreatedByName { get; private set; } = null!;

    // Navigations
    public DailyReport DailyReport { get; private set; } = null!;

    private DailyReportEntry() { }

    public DailyReportEntry(
        Guid dailyReportId,
        EntryType entryType,
        DateTime occurredAt,
        Guid createdByUserId,
        string createdByName,
        DateTime? endedAt = null,
        MealType? mealType = null,
        AmountEaten? amountEaten = null,
        DiaperType? diaperType = null,
        string? description = null)
    {
        if (occurredAt.Kind != DateTimeKind.Utc)
            throw new ArgumentException("OccurredAt timestamp must be UTC.", nameof(occurredAt));
        if (string.IsNullOrWhiteSpace(createdByName))
            throw new DomainRuleException("VALIDATION_FAILED", "Creator name is required.");

        DailyReportId = dailyReportId;
        EntryType = entryType;
        OccurredAt = occurredAt;
        CreatedByUserId = createdByUserId;
        CreatedByName = createdByName.Trim();
        EndedAt = endedAt;
        MealType = mealType;
        AmountEaten = amountEaten;
        DiaperType = diaperType;
        Description = description?.Trim();
    }

    public void Update(
        DateTime occurredAt,
        DateTime? endedAt,
        MealType? mealType,
        AmountEaten? amountEaten,
        DiaperType? diaperType,
        string? description)
    {
        OccurredAt = occurredAt;
        EndedAt = endedAt;
        MealType = mealType;
        AmountEaten = amountEaten;
        DiaperType = diaperType;
        Description = description?.Trim();
    }

    public void EndNap(DateTime endedAt)
    {
        if (EntryType != EntryType.Nap)
            throw new DomainRuleException("VALIDATION_FAILED", "Only nap entries have an end time.");
        if (endedAt < OccurredAt)
            throw new DomainRuleException("VALIDATION_FAILED", "Nap end time cannot be before start time.");

        EndedAt = endedAt;
    }
}

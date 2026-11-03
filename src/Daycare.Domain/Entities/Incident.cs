using Daycare.Domain.Common;
using Daycare.Domain.Enums;
using Daycare.Domain.Exceptions;

namespace Daycare.Domain.Entities;

public class Incident : BaseEntity
{
    public Guid ChildId { get; private set; }
    public Guid ClassroomId { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public IncidentType Type { get; private set; }
    public IncidentSeverity Severity { get; private set; }
    public string Description { get; private set; } = null!;
    public string? ActionTaken { get; private set; }
    public Guid ReportedByUserId { get; private set; }
    public string ReportedByName { get; private set; } = null!;
    public DateTime? ParentNotifiedAt { get; private set; }
    public string? ParentNotifiedByName { get; private set; }
    public DateTime? AcknowledgedAt { get; private set; }
    public Guid? AcknowledgedByGuardianId { get; private set; }

    // Navigations
    public Child Child { get; private set; } = null!;
    public Classroom Classroom { get; private set; } = null!;
    public Guardian? AcknowledgedByGuardian { get; private set; }

    private Incident() { }

    public Incident(
        Guid childId,
        Guid classroomId,
        DateTime occurredAt,
        IncidentType type,
        IncidentSeverity severity,
        string description,
        Guid reportedByUserId,
        string reportedByName,
        string? actionTaken = null,
        DateTime? parentNotifiedAt = null,
        string? parentNotifiedByName = null)
    {
        if (occurredAt.Kind != DateTimeKind.Utc)
            throw new ArgumentException("OccurredAt timestamp must be UTC.", nameof(occurredAt));
        if (string.IsNullOrWhiteSpace(description))
            throw new DomainRuleException("VALIDATION_FAILED", "Description is required.");
        if (string.IsNullOrWhiteSpace(reportedByName))
            throw new DomainRuleException("VALIDATION_FAILED", "Reporter name is required.");

        ChildId = childId;
        ClassroomId = classroomId;
        OccurredAt = occurredAt;
        Type = type;
        Severity = severity;
        Description = description.Trim();
        ReportedByUserId = reportedByUserId;
        ReportedByName = reportedByName.Trim();
        ActionTaken = actionTaken?.Trim();
        ParentNotifiedAt = parentNotifiedAt;
        ParentNotifiedByName = parentNotifiedByName?.Trim();
    }

    public void Update(
        DateTime occurredAt,
        IncidentType type,
        IncidentSeverity severity,
        string description,
        string? actionTaken,
        DateTime? parentNotifiedAt,
        string? parentNotifiedByName)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new DomainRuleException("VALIDATION_FAILED", "Description is required.");

        OccurredAt = occurredAt;
        Type = type;
        Severity = severity;
        Description = description.Trim();
        ActionTaken = actionTaken?.Trim();
        ParentNotifiedAt = parentNotifiedAt;
        ParentNotifiedByName = parentNotifiedByName?.Trim();
    }

    public void NotifyParent(DateTime notifiedAt, string notifiedByName)
    {
        ParentNotifiedAt = notifiedAt;
        ParentNotifiedByName = notifiedByName.Trim();
    }

    public void Acknowledge(Guid guardianId, DateTime acknowledgedAt)
    {
        AcknowledgedAt = acknowledgedAt;
        AcknowledgedByGuardianId = guardianId;
    }
}

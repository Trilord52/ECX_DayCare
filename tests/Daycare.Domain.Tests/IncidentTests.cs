using Daycare.Domain.Entities;
using Daycare.Domain.Enums;
using Daycare.Domain.Exceptions;
using Shouldly;
using Xunit;

namespace Daycare.Domain.Tests;

public class IncidentTests
{
    private readonly Guid _childId = Guid.NewGuid();
    private readonly Guid _classroomId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly DateTime _occurredAt = new(2026, 4, 1, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Constructor_WhenOccurredAtNotUtc_ShouldThrowArgumentException()
    {
        var localTime = new DateTime(2026, 4, 1, 9, 0, 0, DateTimeKind.Local);

        Should.Throw<ArgumentException>(() =>
            new Incident(_childId, _classroomId, localTime, IncidentType.Injury, IncidentSeverity.Minor, "Scrape", _userId, "Staff"));
    }

    [Fact]
    public void Constructor_WhenDescriptionBlank_ShouldThrowDomainRuleException()
    {
        Should.Throw<DomainRuleException>(() =>
            new Incident(_childId, _classroomId, _occurredAt, IncidentType.Injury, IncidentSeverity.Minor, "  ", _userId, "Staff"));
    }

    [Fact]
    public void NotifyParent_ShouldSetTimestampAndName()
    {
        var incident = new Incident(_childId, _classroomId, _occurredAt, IncidentType.Injury, IncidentSeverity.Minor, "Scrape", _userId, "Staff");
        var now = DateTime.UtcNow;

        incident.NotifyParent(now, "Staff Person");

        incident.ParentNotifiedAt.ShouldBe(now);
        incident.ParentNotifiedByName.ShouldBe("Staff Person");
    }

    [Fact]
    public void Acknowledge_ShouldSetGuardianIdAndTimestamp()
    {
        var incident = new Incident(_childId, _classroomId, _occurredAt, IncidentType.Injury, IncidentSeverity.Minor, "Scrape", _userId, "Staff");
        var guardianId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        incident.Acknowledge(guardianId, now);

        incident.AcknowledgedByGuardianId.ShouldBe(guardianId);
        incident.AcknowledgedAt.ShouldBe(now);
    }
}


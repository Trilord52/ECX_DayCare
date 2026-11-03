using Daycare.Domain.Entities;
using Daycare.Domain.Enums;
using Daycare.Domain.Exceptions;
using Shouldly;
using Xunit;

namespace Daycare.Domain.Tests;

public class DailyReportEntryTests
{
    private readonly Guid _reportId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly DateTime _occurredAt = new(2026, 4, 1, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Constructor_WhenOccurredAtNotUtc_ShouldThrowArgumentException()
    {
        var localTime = new DateTime(2026, 4, 1, 9, 0, 0, DateTimeKind.Local);

        Should.Throw<ArgumentException>(() =>
            new DailyReportEntry(_reportId, EntryType.Note, localTime, _userId, "Staff", description: "Hello"));
    }

    [Fact]
    public void Constructor_WhenCreatedByNameBlank_ShouldThrowDomainRuleException()
    {
        Should.Throw<DomainRuleException>(() =>
            new DailyReportEntry(_reportId, EntryType.Note, _occurredAt, _userId, "   ", description: "Hello"));
    }

    [Fact]
    public void EndNap_WhenNotNapEntry_ShouldThrowDomainRuleException()
    {
        var entry = new DailyReportEntry(_reportId, EntryType.Meal, _occurredAt, _userId, "Staff", mealType: MealType.Lunch);

        Should.Throw<DomainRuleException>(() =>
            entry.EndNap(_occurredAt.AddHours(1)));
    }

    [Fact]
    public void EndNap_WhenEndedAtBeforeOccurredAt_ShouldThrowDomainRuleException()
    {
        var entry = new DailyReportEntry(_reportId, EntryType.Nap, _occurredAt, _userId, "Staff");

        Should.Throw<DomainRuleException>(() =>
            entry.EndNap(_occurredAt.AddHours(-1)));
    }

    [Fact]
    public void EndNap_WhenValid_ShouldSetEndedAt()
    {
        var entry = new DailyReportEntry(_reportId, EntryType.Nap, _occurredAt, _userId, "Staff");
        var endedAt = _occurredAt.AddHours(1);

        entry.EndNap(endedAt);

        entry.EndedAt.ShouldBe(endedAt);
    }
}


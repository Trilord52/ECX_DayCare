using Daycare.Domain.Entities;
using Daycare.Domain.Enums;
using Daycare.Domain.Exceptions;
using Shouldly;
using Xunit;

namespace Daycare.Domain.Tests;

public class AnnouncementTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly DateTime _publishedAt = new(2026, 4, 1, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Constructor_WhenPublishedAtNotUtc_ShouldThrowArgumentException()
    {
        var localTime = new DateTime(2026, 4, 1, 9, 0, 0, DateTimeKind.Local);

        Should.Throw<ArgumentException>(() =>
            new Announcement("Title", "Body", AnnouncementAudience.Everyone, _userId, "Admin", localTime));
    }

    [Fact]
    public void Constructor_WhenClassroomAudienceWithoutClassroomId_ShouldThrowDomainRuleException()
    {
        Should.Throw<DomainRuleException>(() =>
            new Announcement("Title", "Body", AnnouncementAudience.Classroom, _userId, "Admin", _publishedAt, classroomId: null));
    }

    [Fact]
    public void Update_WhenClassroomAudienceWithoutClassroomId_ShouldThrowDomainRuleException()
    {
        var announcement = new Announcement("Title", "Body", AnnouncementAudience.Everyone, _userId, "Admin", _publishedAt);

        Should.Throw<DomainRuleException>(() =>
            announcement.Update("Title", "Body", AnnouncementAudience.Classroom, classroomId: null, expiresAt: null));
    }
}


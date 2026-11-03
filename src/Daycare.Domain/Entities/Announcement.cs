using Daycare.Domain.Common;
using Daycare.Domain.Enums;
using Daycare.Domain.Exceptions;

namespace Daycare.Domain.Entities;

public class Announcement : BaseEntity
{
    public string Title { get; private set; } = null!;
    public string Body { get; private set; } = null!;
    public AnnouncementAudience Audience { get; private set; }
    public Guid? ClassroomId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public string CreatedByName { get; private set; } = null!;
    public DateTime PublishedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }

    // Navigations
    public Classroom? Classroom { get; private set; }

    private Announcement() { }

    public Announcement(
        string title,
        string body,
        AnnouncementAudience audience,
        Guid createdByUserId,
        string createdByName,
        DateTime publishedAt,
        Guid? classroomId = null,
        DateTime? expiresAt = null)
    {
        if (publishedAt.Kind != DateTimeKind.Utc)
            throw new ArgumentException("PublishedAt timestamp must be UTC.", nameof(publishedAt));
        if (audience == AnnouncementAudience.Classroom && !classroomId.HasValue)
            throw new DomainRuleException("VALIDATION_FAILED", "Classroom is required when audience is Classroom.");

        Title = title.Trim();
        Body = body.Trim();
        Audience = audience;
        ClassroomId = classroomId;
        CreatedByUserId = createdByUserId;
        CreatedByName = createdByName.Trim();
        PublishedAt = publishedAt;
        ExpiresAt = expiresAt;
    }

    public void Update(
        string title,
        string body,
        AnnouncementAudience audience,
        Guid? classroomId,
        DateTime? expiresAt)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainRuleException("VALIDATION_FAILED", "Title is required.");
        if (string.IsNullOrWhiteSpace(body))
            throw new DomainRuleException("VALIDATION_FAILED", "Body is required.");
        if (audience == AnnouncementAudience.Classroom && !classroomId.HasValue)
            throw new DomainRuleException("VALIDATION_FAILED", "Classroom is required when audience is Classroom.");

        Title = title.Trim();
        Body = body.Trim();
        Audience = audience;
        ClassroomId = classroomId;
        ExpiresAt = expiresAt;
    }
}

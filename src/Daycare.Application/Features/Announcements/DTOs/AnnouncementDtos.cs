using Daycare.Domain.Enums;

namespace Daycare.Application.Features.Announcements.DTOs;

public record AnnouncementDto(
    Guid Id,
    string Title,
    string Body,
    AnnouncementAudience Audience,
    Guid? ClassroomId,
    string? ClassroomName,
    Guid CreatedByUserId,
    string CreatedByName,
    DateTime PublishedAt,
    DateTime? ExpiresAt
);


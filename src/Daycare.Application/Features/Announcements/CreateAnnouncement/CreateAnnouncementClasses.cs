using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Announcements.DTOs;
using Daycare.Domain.Entities;
using Daycare.Domain.Enums;
using Daycare.Domain.Exceptions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Announcements.CreateAnnouncement;

public record CreateAnnouncementRequest(
    string Title,
    string Body,
    AnnouncementAudience Audience,
    Guid? ClassroomId = null,
    DateTime? ExpiresAt = null
);

public class CreateAnnouncementValidator : AbstractValidator<CreateAnnouncementRequest>
{
    public CreateAnnouncementValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200).WithMessage("Title is required and must not exceed 200 characters.");
        RuleFor(x => x.Body).NotEmpty().MaximumLength(4000).WithMessage("Body is required and must not exceed 4000 characters.");

        When(x => x.Audience == AnnouncementAudience.Classroom, () =>
        {
            RuleFor(x => x.ClassroomId).NotEmpty().WithMessage("Classroom is required when audience is Classroom.");
        });

        When(x => x.Audience != AnnouncementAudience.Classroom, () =>
        {
            RuleFor(x => x.ClassroomId).Null().WithMessage("Classroom must not be set unless audience is Classroom.");
        });
    }
}

public class CreateAnnouncementUseCase : UseCase<CreateAnnouncementRequest, AnnouncementDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateAnnouncementUseCase(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        IValidator<CreateAnnouncementRequest> validator)
        : base(validator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    protected override async Task<AnnouncementDto> HandleAsync(
        CreateAnnouncementRequest request,
        CancellationToken ct)
    {
        if (request.Audience == AnnouncementAudience.Classroom)
        {
            if (!request.ClassroomId.HasValue)
            {
                throw new DomainRuleException("VALIDATION_FAILED", "Classroom is required when audience is Classroom.");
            }

            var classroomExists = await _dbContext.Classrooms
                .AnyAsync(c => c.Id == request.ClassroomId.Value, ct);

            if (!classroomExists)
            {
                throw new NotFoundException("Classroom", request.ClassroomId.Value);
            }
        }

        var userId = _currentUser.UserId ?? throw new UnauthorizedException("Authenticated user ID is required.");
        var userName = _currentUser.FullName ?? _currentUser.Email ?? "Admin";

        var publishedAtUtc = _dateTimeProvider.UtcNow;
        var expiresAtUtc = request.ExpiresAt.HasValue
            ? (request.ExpiresAt.Value.Kind == DateTimeKind.Utc
                ? request.ExpiresAt.Value
                : DateTime.SpecifyKind(request.ExpiresAt.Value, DateTimeKind.Utc))
            : (DateTime?)null;

        var classroomId = request.Audience == AnnouncementAudience.Classroom ? request.ClassroomId : null;

        var announcement = new Announcement(
            request.Title,
            request.Body,
            request.Audience,
            userId,
            userName,
            publishedAtUtc,
            classroomId,
            expiresAtUtc);

        _dbContext.Announcements.Add(announcement);
        await _dbContext.SaveChangesAsync(ct);

        string? classroomName = null;
        if (announcement.ClassroomId.HasValue)
        {
            classroomName = await _dbContext.Classrooms
                .Where(c => c.Id == announcement.ClassroomId.Value)
                .Select(c => c.Name)
                .FirstOrDefaultAsync(ct);
        }

        return new AnnouncementDto(
            announcement.Id,
            announcement.Title,
            announcement.Body,
            announcement.Audience,
            announcement.ClassroomId,
            classroomName,
            announcement.CreatedByUserId,
            announcement.CreatedByName,
            announcement.PublishedAt,
            announcement.ExpiresAt);
    }
}


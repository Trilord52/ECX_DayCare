using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Announcements.DTOs;
using Daycare.Domain.Enums;
using Daycare.Domain.Exceptions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Announcements.UpdateAnnouncement;

public record UpdateAnnouncementRequest(
    Guid AnnouncementId,
    string Title,
    string Body,
    AnnouncementAudience Audience,
    Guid? ClassroomId = null,
    DateTime? ExpiresAt = null
);

public class UpdateAnnouncementValidator : AbstractValidator<UpdateAnnouncementRequest>
{
    public UpdateAnnouncementValidator()
    {
        RuleFor(x => x.AnnouncementId).NotEmpty().WithMessage("Announcement ID is required.");
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

public class UpdateAnnouncementUseCase : UseCase<UpdateAnnouncementRequest, AnnouncementDto>
{
    private readonly IApplicationDbContext _dbContext;

    public UpdateAnnouncementUseCase(
        IApplicationDbContext dbContext,
        IValidator<UpdateAnnouncementRequest> validator)
        : base(validator)
    {
        _dbContext = dbContext;
    }

    protected override async Task<AnnouncementDto> HandleAsync(
        UpdateAnnouncementRequest request,
        CancellationToken ct)
    {
        var announcement = await _dbContext.Announcements
            .Include(a => a.Classroom)
            .FirstOrDefaultAsync(a => a.Id == request.AnnouncementId, ct);

        if (announcement == null)
        {
            throw new NotFoundException("Announcement", request.AnnouncementId);
        }

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

        var expiresAtUtc = request.ExpiresAt.HasValue
            ? (request.ExpiresAt.Value.Kind == DateTimeKind.Utc
                ? request.ExpiresAt.Value
                : DateTime.SpecifyKind(request.ExpiresAt.Value, DateTimeKind.Utc))
            : (DateTime?)null;

        var classroomId = request.Audience == AnnouncementAudience.Classroom ? request.ClassroomId : null;

        announcement.Update(
            request.Title,
            request.Body,
            request.Audience,
            classroomId,
            expiresAtUtc);

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


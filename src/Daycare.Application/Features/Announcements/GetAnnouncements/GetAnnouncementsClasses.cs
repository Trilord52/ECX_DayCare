using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Models;
using Daycare.Application.Features.Announcements.DTOs;
using Daycare.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Announcements.GetAnnouncements;

public record GetAnnouncementsRequest(
    int Page = 1,
    int PageSize = 20
);

public class GetAnnouncementsValidator : AbstractValidator<GetAnnouncementsRequest>
{
    public GetAnnouncementsValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("Page must be at least 1.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("PageSize must be between 1 and 100.");
    }
}

public class GetAnnouncementsUseCase : UseCase<GetAnnouncementsRequest, PagedResult<AnnouncementDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetAnnouncementsUseCase(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        IValidator<GetAnnouncementsRequest> validator)
        : base(validator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    protected override async Task<PagedResult<AnnouncementDto>> HandleAsync(
        GetAnnouncementsRequest request,
        CancellationToken ct)
    {
        var query = _dbContext.Announcements
            .AsNoTracking()
            .Include(a => a.Classroom)
            .AsQueryable();

        var nowUtc = _dateTimeProvider.UtcNow;

        // BR-17: Expired announcements are hidden from non-admins
        if (_currentUser.Role != "Admin")
        {
            query = query.Where(a => a.ExpiresAt == null || a.ExpiresAt > nowUtc);
        }

        // BR-17: Visibility scoping by role
        if (_currentUser.Role == "Staff")
        {
            var staffId = _currentUser.StaffId;
            if (!staffId.HasValue && _currentUser.UserId.HasValue)
            {
                staffId = await _dbContext.Staff
                    .AsNoTracking()
                    .Where(s => s.UserId == _currentUser.UserId.Value)
                    .Select(s => (Guid?)s.Id)
                    .FirstOrDefaultAsync(ct);
            }

            var assignedClassroomIds = staffId.HasValue
                ? await _dbContext.StaffClassrooms
                    .AsNoTracking()
                    .Where(sc => sc.StaffId == staffId.Value)
                    .Select(sc => sc.ClassroomId)
                    .ToListAsync(ct)
                : [];

            query = query.Where(a => a.Audience == AnnouncementAudience.Everyone ||
                                     a.Audience == AnnouncementAudience.Staff ||
                                     (a.Audience == AnnouncementAudience.Classroom &&
                                      a.ClassroomId.HasValue &&
                                      assignedClassroomIds.Contains(a.ClassroomId.Value)));
        }
        else if (_currentUser.Role == "Parent")
        {
            var guardianId = _currentUser.GuardianId;
            if (!guardianId.HasValue && _currentUser.UserId.HasValue)
            {
                guardianId = await _dbContext.Guardians
                    .AsNoTracking()
                    .Where(g => g.UserId == _currentUser.UserId.Value)
                    .Select(g => (Guid?)g.Id)
                    .FirstOrDefaultAsync(ct);
            }

            var enrolledClassroomIds = guardianId.HasValue
                ? await _dbContext.ChildGuardians
                    .AsNoTracking()
                    .Where(cg => cg.GuardianId == guardianId.Value && cg.Child.ClassroomId.HasValue)
                    .Select(cg => cg.Child.ClassroomId!.Value)
                    .Distinct()
                    .ToListAsync(ct)
                : [];

            query = query.Where(a => a.Audience == AnnouncementAudience.Everyone ||
                                     a.Audience == AnnouncementAudience.Parents ||
                                     (a.Audience == AnnouncementAudience.Classroom &&
                                      a.ClassroomId.HasValue &&
                                      enrolledClassroomIds.Contains(a.ClassroomId.Value)));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(a => a.PublishedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new AnnouncementDto(
                a.Id,
                a.Title,
                a.Body,
                a.Audience,
                a.ClassroomId,
                a.Classroom != null ? a.Classroom.Name : null,
                a.CreatedByUserId,
                a.CreatedByName,
                a.PublishedAt,
                a.ExpiresAt))
            .ToListAsync(ct);

        return new PagedResult<AnnouncementDto>(items, totalCount, request.Page, request.PageSize);
    }
}


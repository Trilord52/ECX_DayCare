using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Models;
using Daycare.Application.Features.Incidents.DTOs;
using Daycare.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Incidents.GetIncidents;

public record GetIncidentsRequest(
    Guid? ChildId = null,
    Guid? ClassroomId = null,
    IncidentSeverity? Severity = null,
    DateTime? From = null,
    DateTime? To = null,
    bool? Acknowledged = null,
    int Page = 1,
    int PageSize = 20
);

public class GetIncidentsValidator : AbstractValidator<GetIncidentsRequest>
{
    public GetIncidentsValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("Page must be at least 1.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("PageSize must be between 1 and 100.");
        RuleFor(x => x).Must(x => !x.From.HasValue || !x.To.HasValue || x.From <= x.To)
            .WithMessage("From date must be on or before To date.");
    }
}

public class GetIncidentsUseCase : UseCase<GetIncidentsRequest, PagedResult<IncidentListItemDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetIncidentsUseCase(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<GetIncidentsRequest> validator)
        : base(validator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    protected override async Task<PagedResult<IncidentListItemDto>> HandleAsync(
        GetIncidentsRequest request,
        CancellationToken ct)
    {
        var query = _dbContext.Incidents
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(i => i.Child)
            .Include(i => i.Classroom)
            .Include(i => i.AcknowledgedByGuardian)
            .AsQueryable();

        // Scope by caller role
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

            if (!staffId.HasValue)
            {
                return new PagedResult<IncidentListItemDto>([], 0, request.Page, request.PageSize);
            }

            var assignedClassroomIds = await _dbContext.StaffClassrooms
                .AsNoTracking()
                .Where(sc => sc.StaffId == staffId.Value)
                .Select(sc => sc.ClassroomId)
                .ToListAsync(ct);

            query = query.Where(i => assignedClassroomIds.Contains(i.ClassroomId));
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

            if (!guardianId.HasValue)
            {
                return new PagedResult<IncidentListItemDto>([], 0, request.Page, request.PageSize);
            }

            var linkedChildIds = await _dbContext.ChildGuardians
                .AsNoTracking()
                .Where(cg => cg.GuardianId == guardianId.Value)
                .Select(cg => cg.ChildId)
                .ToListAsync(ct);

            query = query.Where(i => linkedChildIds.Contains(i.ChildId));
        }

        // Apply filters
        if (request.ChildId.HasValue)
        {
            query = query.Where(i => i.ChildId == request.ChildId.Value);
        }

        if (request.ClassroomId.HasValue)
        {
            query = query.Where(i => i.ClassroomId == request.ClassroomId.Value);
        }

        if (request.Severity.HasValue)
        {
            query = query.Where(i => i.Severity == request.Severity.Value);
        }

        if (request.From.HasValue)
        {
            var fromUtc = request.From.Value.Kind == DateTimeKind.Utc ? request.From.Value : DateTime.SpecifyKind(request.From.Value, DateTimeKind.Utc);
            query = query.Where(i => i.OccurredAt >= fromUtc);
        }

        if (request.To.HasValue)
        {
            var toUtc = request.To.Value.Kind == DateTimeKind.Utc ? request.To.Value : DateTime.SpecifyKind(request.To.Value, DateTimeKind.Utc);
            query = query.Where(i => i.OccurredAt <= toUtc);
        }

        if (request.Acknowledged.HasValue)
        {
            query = request.Acknowledged.Value
                ? query.Where(i => i.AcknowledgedAt != null)
                : query.Where(i => i.AcknowledgedAt == null);
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(i => i.OccurredAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(i => new IncidentListItemDto(
                i.Id,
                i.ChildId,
                i.Child.FirstName + " " + i.Child.LastName,
                i.ClassroomId,
                i.Classroom.Name,
                i.OccurredAt,
                i.Type,
                i.Severity,
                i.Description,
                i.ActionTaken,
                i.ReportedByUserId,
                i.ReportedByName,
                i.ParentNotifiedAt,
                i.ParentNotifiedByName,
                i.AcknowledgedAt,
                i.AcknowledgedByGuardianId,
                i.AcknowledgedByGuardian != null
                    ? i.AcknowledgedByGuardian.FirstName + " " + i.AcknowledgedByGuardian.LastName
                    : null,
                i.Severity == IncidentSeverity.Serious))
            .ToListAsync(ct);

        return new PagedResult<IncidentListItemDto>(items, totalCount, request.Page, request.PageSize);
    }
}


using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Common.Models;
using Daycare.Application.Features.Children.DTOs;
using Daycare.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Children.GetChildren;

public record GetChildrenRequest(
    string? Search = null,
    Guid? ClassroomId = null,
    ChildStatus? Status = null,
    int Page = 1,
    int PageSize = 20
);

public class GetChildrenUseCase : UseCase<GetChildrenRequest, PagedResult<ChildListItemDto>>
{
    private readonly IAccessGuard _accessGuard;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetChildrenUseCase(IAccessGuard accessGuard, IDateTimeProvider dateTimeProvider)
    {
        _accessGuard = accessGuard;
        _dateTimeProvider = dateTimeProvider;
    }

    protected override async Task<PagedResult<ChildListItemDto>> HandleAsync(
        GetChildrenRequest request,
        CancellationToken ct)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize switch
        {
            < 1 => 20,
            > 100 => 100,
            _ => request.PageSize
        };

        // Scoped by AccessGuard per BR-03
        var query = _accessGuard.VisibleChildren();

        if (request.ClassroomId.HasValue)
        {
            query = query.Where(c => c.ClassroomId == request.ClassroomId.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(c => c.Status == request.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(c =>
                c.FirstName.ToLower().Contains(search) ||
                c.LastName.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(ct);

        var children = await query
            .Include(c => c.Classroom)
            .OrderBy(c => c.LastName)
            .ThenBy(c => c.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var today = _dateTimeProvider.LocalToday;

        var items = children.Select(c => new ChildListItemDto(
            c.Id,
            c.FirstName,
            c.LastName,
            c.FirstName + " " + c.LastName,
            c.DateOfBirth,
            c.CalculateAgeInMonths(today),
            c.Gender,
            c.ClassroomId,
            c.Classroom?.Name,
            c.Status,
            c.EnrollmentDate,
            c.Allergies,
            c.Photo != null,
            c.Photo?.UpdatedAt
        )).ToList();

        return new PagedResult<ChildListItemDto>(items, totalCount, page, pageSize);
    }
}

public record GetChildByIdRequest(Guid Id);

public class GetChildByIdUseCase : UseCase<GetChildByIdRequest, ChildDetailDto>
{
    private readonly IAccessGuard _accessGuard;
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetChildByIdUseCase(
        IAccessGuard accessGuard,
        IApplicationDbContext dbContext,
        IDateTimeProvider dateTimeProvider)
    {
        _accessGuard = accessGuard;
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    protected override async Task<ChildDetailDto> HandleAsync(GetChildByIdRequest request, CancellationToken ct)
    {
        // Enforce access control per BR-03
        await _accessGuard.EnsureCanViewChildAsync(request.Id, ct);

        var child = await _dbContext.Children
            .AsNoTracking()
            .Include(c => c.Classroom)
            .Include(c => c.FeePlan)
            .Include(c => c.Guardians)
                .ThenInclude(cg => cg.Guardian)
            .FirstOrDefaultAsync(c => c.Id == request.Id, ct);

        if (child == null)
        {
            throw new NotFoundException("Child", request.Id);
        }

        var today = _dateTimeProvider.LocalToday;

        var guardians = child.Guardians.Select(cg => new ChildGuardianDto(
            cg.GuardianId,
            cg.Guardian.FirstName + " " + cg.Guardian.LastName,
            cg.Relationship,
            cg.Guardian.Phone,
            cg.Guardian.Email,
            cg.IsPrimaryContact,
            cg.CanPickUp,
            cg.IsPayer,
            cg.IsEmergencyContact,
            cg.Guardian.Photo != null,
            cg.Guardian.Photo?.UpdatedAt
        )).ToList();

        return new ChildDetailDto(
            child.Id,
            child.FirstName,
            child.LastName,
            child.FirstName + " " + child.LastName,
            child.DateOfBirth,
            child.CalculateAgeInMonths(today),
            child.Gender,
            child.ClassroomId,
            child.Classroom?.Name,
            child.FeePlanId,
            child.FeePlan?.Name,
            child.Status,
            child.EnrollmentDate,
            child.Allergies,
            child.MedicalNotes,
            child.DietaryNotes,
            guardians,
            child.Photo != null,
            child.Photo?.UpdatedAt);
    }
}

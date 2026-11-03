using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Common.Models;
using Daycare.Application.Features.Guardians.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Guardians.GetGuardians;

public record GetGuardiansRequest(
    string? Search = null,
    int Page = 1,
    int PageSize = 20
);

public class GetGuardiansUseCase : UseCase<GetGuardiansRequest, PagedResult<GuardianDto>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetGuardiansUseCase(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    protected override async Task<PagedResult<GuardianDto>> HandleAsync(
        GetGuardiansRequest request,
        CancellationToken ct)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize switch
        {
            < 1 => 20,
            > 100 => 100,
            _ => request.PageSize
        };

        var query = _dbContext.Guardians.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(g =>
                g.FirstName.ToLower().Contains(search) ||
                g.LastName.ToLower().Contains(search) ||
                (g.Email != null && g.Email.ToLower().Contains(search)) ||
                g.Phone.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(ct);

        var guardians = await query
            .Include(g => g.Children)
                .ThenInclude(cg => cg.Child)
            .OrderBy(g => g.LastName)
            .ThenBy(g => g.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = guardians.Select(g => new GuardianDto(
            g.Id,
            g.UserId,
            g.FirstName,
            g.LastName,
            g.FirstName + " " + g.LastName,
            g.Phone,
            g.Email,
            g.Address,
            g.Children.Select(cg => new ChildBriefDto(
                cg.Child.Id,
                cg.Child.FirstName + " " + cg.Child.LastName,
                cg.Child.Status.ToString()
            )).ToList(),
            g.Photo != null,
            g.Photo?.UpdatedAt
        )).ToList();

        return new PagedResult<GuardianDto>(items, totalCount, page, pageSize);
    }
}

public record GetGuardianByIdRequest(Guid Id);

public class GetGuardianByIdUseCase : UseCase<GetGuardianByIdRequest, GuardianDto>
{
    private readonly IApplicationDbContext _dbContext;

    public GetGuardianByIdUseCase(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    protected override async Task<GuardianDto> HandleAsync(GetGuardianByIdRequest request, CancellationToken ct)
    {
        var guardian = await _dbContext.Guardians
            .AsNoTracking()
            .Include(g => g.Children)
                .ThenInclude(cg => cg.Child)
            .FirstOrDefaultAsync(g => g.Id == request.Id, ct);

        if (guardian == null)
        {
            throw new NotFoundException("Guardian", request.Id);
        }

        return new GuardianDto(
            guardian.Id,
            guardian.UserId,
            guardian.FirstName,
            guardian.LastName,
            guardian.FirstName + " " + guardian.LastName,
            guardian.Phone,
            guardian.Email,
            guardian.Address,
            guardian.Children.Select(cg => new ChildBriefDto(
                cg.Child.Id,
                cg.Child.FirstName + " " + cg.Child.LastName,
                cg.Child.Status.ToString()
            )).ToList(),
            guardian.Photo != null,
            guardian.Photo?.UpdatedAt);
    }
}

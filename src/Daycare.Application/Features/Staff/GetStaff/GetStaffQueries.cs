using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Staff.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Staff.GetStaff;

public record GetStaffListRequest();

public class GetStaffListUseCase : UseCase<GetStaffListRequest, List<StaffDto>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetStaffListUseCase(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    protected override async Task<List<StaffDto>> HandleAsync(GetStaffListRequest request, CancellationToken ct)
    {
        var staffList = await _dbContext.Staff
            .AsNoTracking()
            .Include(s => s.StaffClassrooms)
                .ThenInclude(sc => sc.Classroom)
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .ToListAsync(ct);

        return staffList.Select(s => new StaffDto(
            s.Id,
            s.UserId,
            s.FirstName,
            s.LastName,
            s.FirstName + " " + s.LastName,
            s.Email,
            s.Phone,
            s.JobTitle,
            s.HireDate,
            s.IsActive,
            s.StaffClassrooms.Select(sc => new ClassroomBriefDto(sc.ClassroomId, sc.Classroom.Name)).ToList(),
            s.Photo != null,
            s.Photo?.UpdatedAt
        )).ToList();
    }
}

public record GetStaffByIdRequest(Guid Id);

public class GetStaffByIdUseCase : UseCase<GetStaffByIdRequest, StaffDto>
{
    private readonly IApplicationDbContext _dbContext;

    public GetStaffByIdUseCase(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    protected override async Task<StaffDto> HandleAsync(GetStaffByIdRequest request, CancellationToken ct)
    {
        var staff = await _dbContext.Staff
            .AsNoTracking()
            .Include(s => s.StaffClassrooms)
                .ThenInclude(sc => sc.Classroom)
            .FirstOrDefaultAsync(s => s.Id == request.Id, ct);

        if (staff == null)
        {
            throw new NotFoundException("Staff member", request.Id);
        }

        return new StaffDto(
            staff.Id,
            staff.UserId,
            staff.FirstName,
            staff.LastName,
            staff.FirstName + " " + staff.LastName,
            staff.Email,
            staff.Phone,
            staff.JobTitle,
            staff.HireDate,
            staff.IsActive,
            staff.StaffClassrooms.Select(sc => new ClassroomBriefDto(sc.ClassroomId, sc.Classroom.Name)).ToList(),
            staff.Photo != null,
            staff.Photo?.UpdatedAt);
    }
}

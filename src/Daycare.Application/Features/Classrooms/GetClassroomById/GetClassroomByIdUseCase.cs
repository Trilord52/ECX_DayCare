using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Classrooms.DTOs;
using Daycare.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Classrooms.GetClassroomById;

public record GetClassroomByIdRequest(Guid Id);

public class GetClassroomByIdUseCase : UseCase<GetClassroomByIdRequest, ClassroomDetailDto>
{
    private readonly IApplicationDbContext _dbContext;

    public GetClassroomByIdUseCase(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    protected override async Task<ClassroomDetailDto> HandleAsync(GetClassroomByIdRequest request, CancellationToken ct)
    {
        var classroom = await _dbContext.Classrooms
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.Id, ct);

        if (classroom == null)
        {
            throw new NotFoundException("Classroom", request.Id);
        }

        var assignedStaff = await _dbContext.StaffClassrooms
            .AsNoTracking()
            .Where(sc => sc.ClassroomId == request.Id)
            .Include(sc => sc.Staff)
            .Select(sc => new StaffBriefDto(
                sc.Staff.Id,
                sc.Staff.FirstName + " " + sc.Staff.LastName,
                sc.Staff.JobTitle))
            .ToListAsync(ct);

        var enrolledCount = await _dbContext.Children
            .CountAsync(c => c.ClassroomId == request.Id && c.Status == ChildStatus.Active, ct);

        return new ClassroomDetailDto(
            classroom.Id,
            classroom.Name,
            classroom.Description,
            classroom.MinAgeMonths,
            classroom.MaxAgeMonths,
            classroom.Capacity,
            classroom.MaxChildrenPerStaff,
            classroom.IsActive,
            enrolledCount,
            assignedStaff.Count,
            assignedStaff);
    }
}

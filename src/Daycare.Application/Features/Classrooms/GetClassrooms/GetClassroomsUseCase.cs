using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Features.Classrooms.DTOs;
using Daycare.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Classrooms.GetClassrooms;

public record GetClassroomsRequest();

public class GetClassroomsUseCase : UseCase<GetClassroomsRequest, List<ClassroomDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetClassroomsUseCase(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    protected override async Task<List<ClassroomDto>> HandleAsync(GetClassroomsRequest request, CancellationToken ct)
    {
        var classroomsQuery = _dbContext.Classrooms.AsNoTracking();

        if (_currentUser.Role == "Staff")
        {
            var staffId = _currentUser.StaffId;
            if (!staffId.HasValue && _currentUser.UserId.HasValue)
            {
                staffId = await _dbContext.Staff
                    .Where(s => s.UserId == _currentUser.UserId.Value)
                    .Select(s => (Guid?)s.Id)
                    .FirstOrDefaultAsync(ct);
            }

            if (!staffId.HasValue)
            {
                return [];
            }

            var assignedClassroomIds = await _dbContext.StaffClassrooms
                .Where(sc => sc.StaffId == staffId.Value)
                .Select(sc => sc.ClassroomId)
                .ToListAsync(ct);

            classroomsQuery = classroomsQuery.Where(c => assignedClassroomIds.Contains(c.Id));
        }

        var classrooms = await classroomsQuery
            .OrderBy(c => c.Name)
            .Select(c => new ClassroomDto(
                c.Id,
                c.Name,
                c.Description,
                c.MinAgeMonths,
                c.MaxAgeMonths,
                c.Capacity,
                c.MaxChildrenPerStaff,
                c.IsActive,
                _dbContext.Children.Count(ch => ch.ClassroomId == c.Id && ch.Status == ChildStatus.Active),
                _dbContext.StaffClassrooms.Count(sc => sc.ClassroomId == c.Id)
            ))
            .ToListAsync(ct);

        return classrooms;
    }
}

using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Attendance.DTOs;
using Daycare.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Attendance.GetRatios;

public record GetRatiosRequest;

public class GetRatiosUseCase : UseCase<GetRatiosRequest, List<RatioItemDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetRatiosUseCase(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    protected override async Task<List<RatioItemDto>> HandleAsync(GetRatiosRequest request, CancellationToken ct)
    {
        if (_currentUser.Role != "Admin" && _currentUser.Role != "Staff")
        {
            throw new ForbiddenException("Only staff and admins can view classroom ratios.");
        }

        IQueryable<Domain.Entities.Classroom> classroomQuery = _dbContext.Classrooms
            .AsNoTracking()
            .Where(c => c.IsActive);

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
                throw new ForbiddenException("Staff profile not found.");
            }

            var assignedClassroomIds = await _dbContext.StaffClassrooms
                .Where(sc => sc.StaffId == staffId.Value)
                .Select(sc => sc.ClassroomId)
                .ToListAsync(ct);

            classroomQuery = classroomQuery.Where(c => assignedClassroomIds.Contains(c.Id));
        }

        var classrooms = await classroomQuery
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

        var today = _dateTimeProvider.LocalToday;

        // Group present children by classroom for today
        var presentCounts = await _dbContext.AttendanceRecords
            .AsNoTracking()
            .Where(a => a.Date == today &&
                        a.Status == AttendanceStatus.Present &&
                        a.CheckOutAt == null)
            .GroupBy(a => a.ClassroomId)
            .Select(g => new { ClassroomId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ClassroomId, x => x.Count, ct);

        // Group active assigned staff by classroom
        var staffCounts = await _dbContext.StaffClassrooms
            .AsNoTracking()
            .Where(sc => sc.Staff.IsActive && !sc.Staff.IsDeleted)
            .GroupBy(sc => sc.ClassroomId)
            .Select(g => new { ClassroomId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ClassroomId, x => x.Count, ct);

        var result = new List<RatioItemDto>();

        foreach (var c in classrooms)
        {
            var presentCount = presentCounts.GetValueOrDefault(c.Id, 0);
            var staffCount = staffCounts.GetValueOrDefault(c.Id, 0);
            var allowedCount = staffCount * c.MaxChildrenPerStaff;
            var isOverLimit = c.IsOverRatio(presentCount, staffCount);

            result.Add(new RatioItemDto(
                c.Id,
                c.Name,
                presentCount,
                staffCount,
                c.MaxChildrenPerStaff,
                allowedCount,
                isOverLimit));
        }

        return result;
    }
}

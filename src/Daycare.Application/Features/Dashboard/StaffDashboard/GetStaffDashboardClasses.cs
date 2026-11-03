using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Attendance.DTOs;
using Daycare.Application.Features.Dashboard.DTOs;
using Daycare.Domain.Entities;
using Daycare.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Dashboard.StaffDashboard;

public record GetStaffDashboardRequest;

public class GetStaffDashboardValidator : AbstractValidator<GetStaffDashboardRequest>
{
}

public class GetStaffDashboardUseCase : UseCase<GetStaffDashboardRequest, StaffDashboardDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetStaffDashboardUseCase(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    protected override async Task<StaffDashboardDto> HandleAsync(
        GetStaffDashboardRequest request,
        CancellationToken ct)
    {
        if (_currentUser.Role != "Staff" && _currentUser.Role != "Admin")
        {
            throw new ForbiddenException("Only staff and admins can access the staff dashboard.");
        }

        var today = _dateTimeProvider.LocalToday;
        List<Classroom> classrooms;

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
                .AsNoTracking()
                .Where(sc => sc.StaffId == staffId.Value)
                .Select(sc => sc.ClassroomId)
                .ToListAsync(ct);

            classrooms = await _dbContext.Classrooms
                .AsNoTracking()
                .Where(c => c.IsActive && assignedClassroomIds.Contains(c.Id))
                .OrderBy(c => c.Name)
                .ToListAsync(ct);
        }
        else
        {
            classrooms = await _dbContext.Classrooms
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync(ct);
        }

        var classroomIds = classrooms.Select(c => c.Id).ToList();

        // 1. Enrolled counts per classroom
        var enrolledCounts = await _dbContext.Children
            .AsNoTracking()
            .Where(c => c.ClassroomId.HasValue &&
                        classroomIds.Contains(c.ClassroomId.Value) &&
                        c.Status == ChildStatus.Active &&
                        !c.IsDeleted)
            .GroupBy(c => c.ClassroomId!.Value)
            .Select(g => new { ClassroomId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ClassroomId, x => x.Count, ct);

        // 2. Attendance records today for these classrooms
        var todayRecords = await _dbContext.AttendanceRecords
            .AsNoTracking()
            .Where(a => a.Date == today && classroomIds.Contains(a.ClassroomId))
            .ToListAsync(ct);

        // 3. Staff count per classroom
        var staffCounts = await _dbContext.StaffClassrooms
            .AsNoTracking()
            .Where(sc => sc.Staff.IsActive && !sc.Staff.IsDeleted && classroomIds.Contains(sc.ClassroomId))
            .GroupBy(sc => sc.ClassroomId)
            .Select(g => new { ClassroomId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ClassroomId, x => x.Count, ct);

        // 4. Incidents per classroom
        var openIncidents = await _dbContext.Incidents
            .AsNoTracking()
            .Where(i => classroomIds.Contains(i.ClassroomId) && i.AcknowledgedAt == null)
            .GroupBy(i => i.ClassroomId)
            .Select(g => new { ClassroomId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ClassroomId, x => x.Count, ct);

        var recentIncidentsList = await _dbContext.Incidents
            .AsNoTracking()
            .Include(i => i.Child)
            .Where(i => classroomIds.Contains(i.ClassroomId))
            .OrderByDescending(i => i.OccurredAt)
            .Take(20)
            .ToListAsync(ct);

        var classroomDtos = new List<StaffDashboardClassroomDto>();

        foreach (var c in classrooms)
        {
            var enrolled = enrolledCounts.GetValueOrDefault(c.Id, 0);
            var records = todayRecords.Where(a => a.ClassroomId == c.Id).ToList();

            var present = records.Count(a => a.Status == AttendanceStatus.Present && a.CheckInAt != null && a.CheckOutAt == null);
            var checkedOut = records.Count(a => a.Status == AttendanceStatus.Present && a.CheckInAt != null && a.CheckOutAt != null);
            var absent = records.Count(a => a.Status == AttendanceStatus.Absent);
            var notArrived = Math.Max(0, enrolled - (present + checkedOut + absent));

            var staffCount = staffCounts.GetValueOrDefault(c.Id, 0);
            var allowedCount = staffCount * c.MaxChildrenPerStaff;
            var isOverLimit = c.IsOverRatio(present, staffCount);

            var ratio = new RatioItemDto(
                c.Id,
                c.Name,
                present,
                staffCount,
                c.MaxChildrenPerStaff,
                allowedCount,
                isOverLimit);

            var openCount = openIncidents.GetValueOrDefault(c.Id, 0);

            var recentIncidentsForClassroom = recentIncidentsList
                .Where(i => i.ClassroomId == c.Id)
                .Take(5)
                .Select(i => new DashboardIncidentDto(
                    i.Id,
                    i.Child.FirstName + " " + i.Child.LastName,
                    i.Type,
                    i.Severity,
                    i.OccurredAt))
                .ToList();

            classroomDtos.Add(new StaffDashboardClassroomDto(
                c.Id,
                c.Name,
                enrolled,
                present,
                checkedOut,
                absent,
                notArrived,
                ratio,
                openCount,
                recentIncidentsForClassroom));
        }

        return new StaffDashboardDto(today, classroomDtos);
    }
}

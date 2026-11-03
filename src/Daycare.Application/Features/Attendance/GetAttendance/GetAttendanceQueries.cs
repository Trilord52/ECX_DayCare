using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Attendance.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Attendance.GetAttendance;

public record GetAttendanceRecordsRequest(
    DateOnly? Date = null,
    Guid? ClassroomId = null
);

public class GetAttendanceRecordsUseCase : UseCase<GetAttendanceRecordsRequest, List<AttendanceRecordDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetAttendanceRecordsUseCase(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    protected override async Task<List<AttendanceRecordDto>> HandleAsync(
        GetAttendanceRecordsRequest request,
        CancellationToken ct)
    {
        var targetDate = request.Date ?? _dateTimeProvider.LocalToday;
        var query = _dbContext.AttendanceRecords
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(a => a.Date == targetDate);

        if (request.ClassroomId.HasValue)
        {
            query = query.Where(a => a.ClassroomId == request.ClassroomId.Value);
        }

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

            query = query.Where(a => assignedClassroomIds.Contains(a.ClassroomId));
        }
        else if (_currentUser.Role != "Admin")
        {
            throw new ForbiddenException("Only staff and admins can query attendance records.");
        }

        var records = await query
            .Include(a => a.Child)
            .Include(a => a.Classroom)
            .Include(a => a.CheckedInByGuardian)
            .Include(a => a.CheckedOutByGuardian)
            .OrderByDescending(a => a.CheckInAt)
            .ThenBy(a => a.Child.LastName)
            .ToListAsync(ct);

        return records.Select(a => new AttendanceRecordDto(
            a.Id,
            a.ChildId,
            $"{a.Child.FirstName} {a.Child.LastName}",
            a.ClassroomId,
            a.Classroom.Name,
            a.Date,
            a.Status,
            a.CheckInAt,
            a.CheckedInByName,
            a.CheckedInByGuardianId,
            a.CheckedInByGuardian != null ? $"{a.CheckedInByGuardian.FirstName} {a.CheckedInByGuardian.LastName}" : null,
            a.CheckOutAt,
            a.CheckedOutByName,
            a.CheckedOutByGuardianId,
            a.CheckedOutByGuardian != null ? $"{a.CheckedOutByGuardian.FirstName} {a.CheckedOutByGuardian.LastName}" : null,
            a.PickupIdVerified,
            a.LateMinutes,
            a.AbsenceReason,
            a.Notes
        )).ToList();
    }
}

public record GetChildAttendanceHistoryRequest(
    Guid ChildId,
    DateOnly? From = null,
    DateOnly? To = null
);

public class GetChildAttendanceHistoryUseCase : UseCase<GetChildAttendanceHistoryRequest, List<AttendanceRecordDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAccessGuard _accessGuard;

    public GetChildAttendanceHistoryUseCase(
        IApplicationDbContext dbContext,
        IAccessGuard accessGuard)
    {
        _dbContext = dbContext;
        _accessGuard = accessGuard;
    }

    protected override async Task<List<AttendanceRecordDto>> HandleAsync(
        GetChildAttendanceHistoryRequest request,
        CancellationToken ct)
    {
        // Enforce access control: Admin, Staff in assigned classroom, Parent of child
        await _accessGuard.EnsureCanViewChildAsync(request.ChildId, ct);

        var query = _dbContext.AttendanceRecords
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(a => a.ChildId == request.ChildId);

        if (request.From.HasValue)
        {
            query = query.Where(a => a.Date >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(a => a.Date <= request.To.Value);
        }

        var records = await query
            .Include(a => a.Child)
            .Include(a => a.Classroom)
            .Include(a => a.CheckedInByGuardian)
            .Include(a => a.CheckedOutByGuardian)
            .OrderByDescending(a => a.Date)
            .ThenByDescending(a => a.CheckInAt)
            .ToListAsync(ct);

        return records.Select(a => new AttendanceRecordDto(
            a.Id,
            a.ChildId,
            $"{a.Child.FirstName} {a.Child.LastName}",
            a.ClassroomId,
            a.Classroom.Name,
            a.Date,
            a.Status,
            a.CheckInAt,
            a.CheckedInByName,
            a.CheckedInByGuardianId,
            a.CheckedInByGuardian != null ? $"{a.CheckedInByGuardian.FirstName} {a.CheckedInByGuardian.LastName}" : null,
            a.CheckOutAt,
            a.CheckedOutByName,
            a.CheckedOutByGuardianId,
            a.CheckedOutByGuardian != null ? $"{a.CheckedOutByGuardian.FirstName} {a.CheckedOutByGuardian.LastName}" : null,
            a.PickupIdVerified,
            a.LateMinutes,
            a.AbsenceReason,
            a.Notes
        )).ToList();
    }
}

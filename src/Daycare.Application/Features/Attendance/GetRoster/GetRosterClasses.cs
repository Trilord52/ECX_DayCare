using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Attendance.DTOs;
using Daycare.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Attendance.GetRoster;

public record GetRosterRequest(Guid ClassroomId, DateOnly? Date = null);

public class GetRosterUseCase : UseCase<GetRosterRequest, List<RosterItemDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetRosterUseCase(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    protected override async Task<List<RosterItemDto>> HandleAsync(GetRosterRequest request, CancellationToken ct)
    {
        var classroom = await _dbContext.Classrooms
            .FirstOrDefaultAsync(c => c.Id == request.ClassroomId, ct);

        if (classroom == null)
        {
            throw new NotFoundException("Classroom", request.ClassroomId);
        }

        // Staff scoping: must be assigned to classroom
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

            var isAssigned = await _dbContext.StaffClassrooms
                .AnyAsync(sc => sc.StaffId == staffId.Value && sc.ClassroomId == request.ClassroomId, ct);

            if (!isAssigned)
            {
                throw new ForbiddenException("Staff may only view the roster for assigned classrooms.");
            }
        }
        else if (_currentUser.Role != "Admin")
        {
            throw new ForbiddenException("Only staff and admins may view classroom rosters.");
        }

        var targetDate = request.Date ?? _dateTimeProvider.LocalToday;

        var children = await _dbContext.Children
            .AsNoTracking()
            .Where(c => c.ClassroomId == request.ClassroomId && c.Status == ChildStatus.Active)
            .Include(c => c.Guardians)
                .ThenInclude(cg => cg.Guardian)
            .OrderBy(c => c.LastName)
            .ThenBy(c => c.FirstName)
            .ToListAsync(ct);

        var childIds = children.Select(c => c.Id).ToList();

        var attendanceRecords = await _dbContext.AttendanceRecords
            .AsNoTracking()
            .Where(a => a.ClassroomId == request.ClassroomId && a.Date == targetDate && childIds.Contains(a.ChildId))
            .ToDictionaryAsync(a => a.ChildId, ct);

        var today = _dateTimeProvider.LocalToday;
        var result = new List<RosterItemDto>();

        foreach (var child in children)
        {
            string status;
            Guid? attendanceId = null;
            DateTime? checkInAt = null;
            DateTime? checkOutAt = null;
            var lateMinutes = 0;

            if (attendanceRecords.TryGetValue(child.Id, out var record))
            {
                attendanceId = record.Id;
                checkInAt = record.CheckInAt;
                checkOutAt = record.CheckOutAt;
                lateMinutes = record.LateMinutes;

                if (record.Status == AttendanceStatus.Absent)
                {
                    status = "Absent";
                }
                else if (record.CheckOutAt != null)
                {
                    status = "CheckedOut";
                }
                else
                {
                    status = "Present";
                }
            }
            else
            {
                status = "NotArrived";
            }

            var guardians = child.Guardians.Select(cg => new RosterGuardianDto(
                cg.GuardianId,
                $"{cg.Guardian.FirstName} {cg.Guardian.LastName}",
                cg.Relationship,
                cg.Guardian.Phone,
                cg.CanPickUp,
                cg.Guardian.Photo != null,
                cg.Guardian.Photo?.UpdatedAt
            )).ToList();

            result.Add(new RosterItemDto(
                child.Id,
                $"{child.FirstName} {child.LastName}",
                child.CalculateAgeInMonths(today),
                child.Allergies,
                child.MedicalNotes,
                status,
                attendanceId,
                checkInAt,
                checkOutAt,
                lateMinutes,
                guardians,
                child.Photo != null,
                child.Photo?.UpdatedAt));
        }

        return result;
    }
}

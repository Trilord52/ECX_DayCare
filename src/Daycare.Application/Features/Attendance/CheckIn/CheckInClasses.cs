using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Attendance.DTOs;
using Daycare.Domain.Entities;
using Daycare.Domain.Enums;
using Daycare.Domain.Exceptions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Attendance.CheckIn;

public record CheckInRequest(
    Guid ChildId,
    Guid? DroppedOffByGuardianId = null,
    string? Notes = null
);

public class CheckInValidator : AbstractValidator<CheckInRequest>
{
    public CheckInValidator()
    {
        RuleFor(x => x.ChildId).NotEmpty().WithMessage("Child ID is required.");
        RuleFor(x => x.Notes).MaximumLength(1000).WithMessage("Notes must not exceed 1000 characters.");
    }
}

public class CheckInUseCase : UseCase<CheckInRequest, CheckInResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAccessGuard _accessGuard;

    public CheckInUseCase(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        IAccessGuard accessGuard,
        IValidator<CheckInRequest> validator)
        : base(validator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _accessGuard = accessGuard;
    }

    protected override async Task<CheckInResponse> HandleAsync(CheckInRequest request, CancellationToken ct)
    {
        // Permission check
        await _accessGuard.EnsureCanWriteChildDataAsync(request.ChildId, ct);

        var child = await _dbContext.Children
            .Include(c => c.Classroom)
            .Include(c => c.Guardians)
                .ThenInclude(cg => cg.Guardian)
            .FirstOrDefaultAsync(c => c.Id == request.ChildId, ct);

        if (child == null)
        {
            throw new NotFoundException("Child", request.ChildId);
        }

        // BR-09: Child is Active
        if (child.Status != ChildStatus.Active)
        {
            throw new DomainRuleException("CHILD_INACTIVE", "Child is not currently active.");
        }

        // BR-09: Child has a classroom
        if (!child.ClassroomId.HasValue || child.Classroom == null)
        {
            throw new DomainRuleException("NO_CLASSROOM", "Child is not enrolled in a classroom.");
        }

        // BR-09: At least one can_pick_up guardian
        var hasPickupGuardian = child.Guardians.Any(cg => cg.CanPickUp);
        if (!hasPickupGuardian)
        {
            throw new DomainRuleException("NO_PICKUP_GUARDIAN", "Child has no authorized pickup contact.");
        }

        // BR-09: At least one emergency contact
        var hasEmergencyContact = child.Guardians.Any(cg => cg.IsEmergencyContact);
        if (!hasEmergencyContact)
        {
            throw new DomainRuleException("NO_EMERGENCY_CONTACT", "Child has no emergency contact.");
        }

        var today = _dateTimeProvider.LocalToday;

        // BR-08, BR-09: One record per child per local day
        var alreadyExists = await _dbContext.AttendanceRecords
            .AnyAsync(a => a.ChildId == request.ChildId && a.Date == today, ct);

        if (alreadyExists)
        {
            throw new DomainRuleException("ALREADY_CHECKED_IN", "Child already has an attendance record for today.");
        }

        string? droppedOffByName = null;
        if (request.DroppedOffByGuardianId.HasValue)
        {
            var linkedGuardian = child.Guardians
                .FirstOrDefault(cg => cg.GuardianId == request.DroppedOffByGuardianId.Value);

            if (linkedGuardian == null)
            {
                throw new DomainRuleException("DROPOFF_NOT_AUTHORIZED", "Guardian is not linked to child.");
            }

            droppedOffByName = $"{linkedGuardian.Guardian.FirstName} {linkedGuardian.Guardian.LastName}";
        }

        var nowUtc = _dateTimeProvider.UtcNow;
        var userId = _currentUser.UserId ?? throw new UnauthorizedException("Authenticated user ID is required.");
        var userName = _currentUser.FullName ?? _currentUser.Email ?? "Staff";

        var record = AttendanceRecord.CreateCheckIn(
            child.Id,
            child.ClassroomId.Value,
            today,
            nowUtc,
            request.DroppedOffByGuardianId,
            userId,
            userName,
            request.Notes);

        _dbContext.AttendanceRecords.Add(record);
        await _dbContext.SaveChangesAsync(ct);

        // BR-13: Calculate classroom ratio
        var classroom = child.Classroom;
        var presentCount = await _dbContext.AttendanceRecords
            .CountAsync(a => a.ClassroomId == classroom.Id &&
                             a.Date == today &&
                             a.Status == AttendanceStatus.Present &&
                             a.CheckOutAt == null, ct);

        var staffCount = await _dbContext.StaffClassrooms
            .CountAsync(sc => sc.ClassroomId == classroom.Id &&
                             sc.Staff.IsActive &&
                             !sc.Staff.IsDeleted, ct);

        var allowedCount = staffCount * classroom.MaxChildrenPerStaff;
        var isOverLimit = classroom.IsOverRatio(presentCount, staffCount);

        var ratioDto = new RatioItemDto(
            classroom.Id,
            classroom.Name,
            presentCount,
            staffCount,
            classroom.MaxChildrenPerStaff,
            allowedCount,
            isOverLimit);

        var recordDto = new AttendanceRecordDto(
            record.Id,
            record.ChildId,
            $"{child.FirstName} {child.LastName}",
            classroom.Id,
            classroom.Name,
            record.Date,
            record.Status,
            record.CheckInAt,
            record.CheckedInByName,
            record.CheckedInByGuardianId,
            droppedOffByName,
            record.CheckOutAt,
            record.CheckedOutByName,
            record.CheckedOutByGuardianId,
            null,
            record.PickupIdVerified,
            record.LateMinutes,
            record.AbsenceReason,
            record.Notes);

        return new CheckInResponse(recordDto, isOverLimit, ratioDto);
    }
}

using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Attendance.DTOs;
using Daycare.Domain.Enums;
using Daycare.Domain.Exceptions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Daycare.Application.Features.Attendance.CheckOut;

public record CheckOutRequest(
    Guid AttendanceId,
    Guid PickedUpByGuardianId,
    bool IdVerified
);

public class CheckOutValidator : AbstractValidator<CheckOutRequest>
{
    public CheckOutValidator()
    {
        RuleFor(x => x.AttendanceId).NotEmpty().WithMessage("Attendance ID is required.");
        RuleFor(x => x.PickedUpByGuardianId).NotEmpty().WithMessage("Pickup guardian ID is required.");
    }
}

public class CheckOutUseCase : UseCase<CheckOutRequest, AttendanceRecordDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAccessGuard _accessGuard;
    private readonly DaycareOptions _daycareOptions;

    public CheckOutUseCase(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        IAccessGuard accessGuard,
        IOptions<DaycareOptions> daycareOptions,
        IValidator<CheckOutRequest> validator)
        : base(validator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _accessGuard = accessGuard;
        _daycareOptions = daycareOptions.Value;
    }

    protected override async Task<AttendanceRecordDto> HandleAsync(CheckOutRequest request, CancellationToken ct)
    {
        var record = await _dbContext.AttendanceRecords
            .Include(a => a.Child)
                .ThenInclude(c => c.Guardians)
                    .ThenInclude(cg => cg.Guardian)
            .Include(a => a.Classroom)
            .Include(a => a.CheckedInByGuardian)
            .FirstOrDefaultAsync(a => a.Id == request.AttendanceId, ct);

        if (record == null)
        {
            throw new NotFoundException("Attendance record", request.AttendanceId);
        }

        // Permission check
        await _accessGuard.EnsureCanWriteChildDataAsync(record.ChildId, ct);

        // BR-10: Check-out requires an open Present record
        if (record.Status != AttendanceStatus.Present || record.CheckInAt == null)
        {
            throw new DomainRuleException("NOT_CHECKED_IN", "Child is not currently checked in.");
        }

        if (record.CheckOutAt != null)
        {
            throw new DomainRuleException("ALREADY_CHECKED_OUT", "Child has already been checked out.");
        }

        // BR-10: idVerified must be true
        if (!request.IdVerified)
        {
            throw new DomainRuleException("ID_NOT_VERIFIED", "Photo ID must be verified before check-out.");
        }

        // BR-10: pickedUpByGuardianId must be linked to the child with can_pick_up = true
        var guardianLink = record.Child.Guardians
            .FirstOrDefault(cg => cg.GuardianId == request.PickedUpByGuardianId);

        if (guardianLink == null || !guardianLink.CanPickUp)
        {
            throw new DomainRuleException("PICKUP_NOT_AUTHORIZED", "Guardian is not authorized to pick up this child.");
        }

        // BR-11: Late minutes computation
        var nowUtc = _dateTimeProvider.UtcNow;
        var localNow = _dateTimeProvider.ToLocal(nowUtc);
        var localTime = TimeOnly.FromDateTime(localNow);
        var closingTime = TimeOnly.Parse(_daycareOptions.ClosingTime);

        var lateMinutes = 0;
        if (localTime > closingTime)
        {
            lateMinutes = (int)(localTime - closingTime).TotalMinutes;
        }

        var userId = _currentUser.UserId ?? throw new UnauthorizedException("Authenticated user ID is required.");
        var userName = _currentUser.FullName ?? _currentUser.Email ?? "Staff";

        record.CheckOut(
            nowUtc,
            request.PickedUpByGuardianId,
            userId,
            userName,
            request.IdVerified,
            lateMinutes);

        await _dbContext.SaveChangesAsync(ct);

        var pickedUpByName = $"{guardianLink.Guardian.FirstName} {guardianLink.Guardian.LastName}";
        var droppedOffByName = record.CheckedInByGuardian != null
            ? $"{record.CheckedInByGuardian.FirstName} {record.CheckedInByGuardian.LastName}"
            : null;

        return new AttendanceRecordDto(
            record.Id,
            record.ChildId,
            $"{record.Child.FirstName} {record.Child.LastName}",
            record.ClassroomId,
            record.Classroom.Name,
            record.Date,
            record.Status,
            record.CheckInAt,
            record.CheckedInByName,
            record.CheckedInByGuardianId,
            droppedOffByName,
            record.CheckOutAt,
            record.CheckedOutByName,
            record.CheckedOutByGuardianId,
            pickedUpByName,
            record.PickupIdVerified,
            record.LateMinutes,
            record.AbsenceReason,
            record.Notes);
    }
}

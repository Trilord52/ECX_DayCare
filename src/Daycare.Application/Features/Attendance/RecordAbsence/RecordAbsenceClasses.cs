using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Attendance.DTOs;
using Daycare.Domain.Entities;
using Daycare.Domain.Enums;
using Daycare.Domain.Exceptions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Attendance.RecordAbsence;

public record RecordAbsenceRequest(
    Guid ChildId,
    DateOnly Date,
    string Reason
);

public class RecordAbsenceValidator : AbstractValidator<RecordAbsenceRequest>
{
    public RecordAbsenceValidator(IDateTimeProvider dateTimeProvider)
    {
        RuleFor(x => x.ChildId).NotEmpty().WithMessage("Child ID is required.");
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Absence reason is required.")
            .MaximumLength(500).WithMessage("Reason must not exceed 500 characters.");

        RuleFor(x => x.Date)
            .LessThanOrEqualTo(_ => dateTimeProvider.LocalToday)
            .WithMessage("Absence date cannot be in the future.")
            .GreaterThanOrEqualTo(_ => dateTimeProvider.LocalToday.AddDays(-30))
            .WithMessage("Absence date cannot be more than 30 days in the past.");
    }
}

public class RecordAbsenceUseCase : UseCase<RecordAbsenceRequest, AttendanceRecordDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IAccessGuard _accessGuard;

    public RecordAbsenceUseCase(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IAccessGuard accessGuard,
        IValidator<RecordAbsenceRequest> validator)
        : base(validator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _accessGuard = accessGuard;
    }

    protected override async Task<AttendanceRecordDto> HandleAsync(RecordAbsenceRequest request, CancellationToken ct)
    {
        await _accessGuard.EnsureCanWriteChildDataAsync(request.ChildId, ct);

        var child = await _dbContext.Children
            .Include(c => c.Classroom)
            .FirstOrDefaultAsync(c => c.Id == request.ChildId, ct);

        if (child == null)
        {
            throw new NotFoundException("Child", request.ChildId);
        }

        if (child.Status != ChildStatus.Active)
        {
            throw new DomainRuleException("CHILD_INACTIVE", "Child is not currently active.");
        }

        if (!child.ClassroomId.HasValue || child.Classroom == null)
        {
            throw new DomainRuleException("NO_CLASSROOM", "Child is not enrolled in a classroom.");
        }

        // BR-12: Fails with ALREADY_CHECKED_IN if a record exists for that day
        var alreadyExists = await _dbContext.AttendanceRecords
            .AnyAsync(a => a.ChildId == request.ChildId && a.Date == request.Date, ct);

        if (alreadyExists)
        {
            throw new DomainRuleException("ALREADY_CHECKED_IN", "Child already has an attendance record for this day.");
        }

        var userId = _currentUser.UserId;
        var userName = _currentUser.FullName ?? _currentUser.Email ?? "Staff";

        var record = AttendanceRecord.CreateAbsent(
            child.Id,
            child.ClassroomId.Value,
            request.Date,
            request.Reason,
            userId,
            userName);

        _dbContext.AttendanceRecords.Add(record);
        await _dbContext.SaveChangesAsync(ct);

        return new AttendanceRecordDto(
            record.Id,
            record.ChildId,
            $"{child.FirstName} {child.LastName}",
            child.Classroom.Id,
            child.Classroom.Name,
            record.Date,
            record.Status,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            0,
            record.AbsenceReason,
            record.Notes);
    }
}

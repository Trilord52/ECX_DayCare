using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Reports.DTOs;
using Daycare.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Reports.AttendanceReport;

public record GetAttendanceReportRequest(
    DateOnly? From = null,
    DateOnly? To = null,
    Guid? ClassroomId = null
);

public class GetAttendanceReportValidator : AbstractValidator<GetAttendanceReportRequest>
{
    public GetAttendanceReportValidator()
    {
        RuleFor(x => x)
            .Must(x => !x.From.HasValue || !x.To.HasValue || x.From.Value <= x.To.Value)
            .WithMessage("'From' date must be less than or equal to 'To' date.");
    }
}

public class GetAttendanceReportUseCase : UseCase<GetAttendanceReportRequest, AttendanceReportDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetAttendanceReportUseCase(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        IValidator<GetAttendanceReportRequest> validator)
        : base(validator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    protected override async Task<AttendanceReportDto> HandleAsync(
        GetAttendanceReportRequest request,
        CancellationToken ct)
    {
        if (_currentUser.Role != "Admin")
        {
            throw new ForbiddenException("Only admins can access reports.");
        }

        var to = request.To ?? _dateTimeProvider.LocalToday;
        var from = request.From ?? to.AddDays(-30);

        var query = _dbContext.AttendanceRecords
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(a => a.Child)
            .Include(a => a.Classroom)
            .Where(a => a.Date >= from && a.Date <= to);

        if (request.ClassroomId.HasValue)
        {
            query = query.Where(a => a.ClassroomId == request.ClassroomId.Value);
        }

        var records = await query
            .OrderByDescending(a => a.Date)
            .ThenBy(a => a.Child.LastName)
            .ThenBy(a => a.Child.FirstName)
            .ToListAsync(ct);

        var items = records.Select(a => new AttendanceReportItemDto(
            a.Date,
            a.ChildId,
            a.Child.FirstName + " " + a.Child.LastName,
            a.ClassroomId,
            a.Classroom.Name,
            a.Status.ToString(),
            a.CheckInAt,
            a.CheckOutAt,
            a.LateMinutes,
            a.AbsenceReason)).ToList();

        var totalRecords = items.Count;
        var totalPresent = items.Count(i => i.Status == AttendanceStatus.Present.ToString());
        var totalAbsences = items.Count(i => i.Status == AttendanceStatus.Absent.ToString());
        var totalLatePickups = items.Count(i => i.LateMinutes > 0);
        var totalLateMinutes = items.Sum(i => i.LateMinutes);

        return new AttendanceReportDto(
            from,
            to,
            totalRecords,
            totalPresent,
            totalAbsences,
            totalLatePickups,
            totalLateMinutes,
            items);
    }
}

using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Attendance.DTOs;
using Daycare.Application.Features.Dashboard.DTOs;
using Daycare.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Dashboard.AdminDashboard;

public record GetAdminDashboardRequest;

public class GetAdminDashboardValidator : AbstractValidator<GetAdminDashboardRequest>
{
}

public class GetAdminDashboardUseCase : UseCase<GetAdminDashboardRequest, AdminDashboardDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetAdminDashboardUseCase(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    protected override async Task<AdminDashboardDto> HandleAsync(
        GetAdminDashboardRequest request,
        CancellationToken ct)
    {
        if (_currentUser.Role != "Admin")
        {
            throw new ForbiddenException("Only admins can access the admin dashboard.");
        }

        var today = _dateTimeProvider.LocalToday;

        // 1. Enrolled children count
        var enrolledChildren = await _dbContext.Children
            .AsNoTracking()
            .CountAsync(c => c.Status == ChildStatus.Active && !c.IsDeleted, ct);

        // 2. Attendance stats for today
        var todayRecords = await _dbContext.AttendanceRecords
            .AsNoTracking()
            .Where(a => a.Date == today)
            .ToListAsync(ct);

        var presentToday = todayRecords.Count(a => a.Status == AttendanceStatus.Present && a.CheckInAt != null);
        var inBuildingNow = todayRecords.Count(a => a.Status == AttendanceStatus.Present && a.CheckOutAt == null);
        var absentToday = todayRecords.Count(a => a.Status == AttendanceStatus.Absent);
        var notArrivedToday = Math.Max(0, enrolledChildren - (presentToday + absentToday));

        // 3. Ratios per active classroom
        var classrooms = await _dbContext.Classrooms
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

        var presentCountsByClassroom = todayRecords
            .Where(a => a.Status == AttendanceStatus.Present && a.CheckOutAt == null)
            .GroupBy(a => a.ClassroomId)
            .ToDictionary(g => g.Key, g => g.Count());

        var staffCounts = await _dbContext.StaffClassrooms
            .AsNoTracking()
            .Where(sc => sc.Staff.IsActive && !sc.Staff.IsDeleted)
            .GroupBy(sc => sc.ClassroomId)
            .Select(g => new { ClassroomId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ClassroomId, x => x.Count, ct);

        var ratios = classrooms.Select(c =>
        {
            var presentCount = presentCountsByClassroom.GetValueOrDefault(c.Id, 0);
            var staffCount = staffCounts.GetValueOrDefault(c.Id, 0);
            var allowedCount = staffCount * c.MaxChildrenPerStaff;
            var isOverLimit = c.IsOverRatio(presentCount, staffCount);

            return new RatioItemDto(
                c.Id,
                c.Name,
                presentCount,
                staffCount,
                c.MaxChildrenPerStaff,
                allowedCount,
                isOverLimit);
        }).ToList();

        // 4. Incidents
        var unacknowledgedIncidents = await _dbContext.Incidents
            .AsNoTracking()
            .CountAsync(i => i.AcknowledgedAt == null, ct);

        var recentIncidents = await _dbContext.Incidents
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(i => i.Child)
            .OrderByDescending(i => i.OccurredAt)
            .Take(5)
            .Select(i => new DashboardIncidentDto(
                i.Id,
                i.Child.FirstName + " " + i.Child.LastName,
                i.Type,
                i.Severity,
                i.OccurredAt))
            .ToListAsync(ct);

        // 5. Billing summary
        var monthStr = today.ToString("yyyy-MM");
        var activeInvoices = await _dbContext.Invoices
            .AsNoTracking()
            .Where(i => i.Status != InvoiceStatus.Void)
            .ToListAsync(ct);

        // invoiced = non-void invoices with period in the current month
        var invoiced = activeInvoices
            .Where(i => i.PeriodStart.Year == today.Year && i.PeriodStart.Month == today.Month)
            .Sum(i => i.TotalAmount);

        // collected = non-voided payments received this month
        var startOfMonth = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var startOfNextMonth = startOfMonth.AddMonths(1);
        var collected = await _dbContext.Payments
            .AsNoTracking()
            .Where(p => p.VoidedAt == null && p.PaidAt >= startOfMonth && p.PaidAt < startOfNextMonth)
            .SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;

        // outstanding = unpaid balance of all non-void invoices, any month
        var outstanding = activeInvoices.Sum(i => i.Balance);

        // overdueCount = non-void invoices past due and not fully paid, any month
        var overdueCount = activeInvoices.Count(i => i.DueDate < today && i.Balance > 0);

        var billing = new AdminDashboardBillingDto(
            monthStr,
            invoiced,
            collected,
            outstanding,
            overdueCount,
            "ETB");

        // 6. Attendance trend (last 14 days up to today)
        var startDate = today.AddDays(-13);
        var pastRecords = await _dbContext.AttendanceRecords
            .AsNoTracking()
            .Where(a => a.Date >= startDate && a.Date <= today && a.CheckInAt != null && a.Status == AttendanceStatus.Present)
            .GroupBy(a => a.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Date, x => x.Count, ct);

        var attendanceTrend = new List<AttendanceTrendItemDto>();
        for (var d = startDate; d <= today; d = d.AddDays(1))
        {
            attendanceTrend.Add(new AttendanceTrendItemDto(d, pastRecords.GetValueOrDefault(d, 0)));
        }

        return new AdminDashboardDto(
            today,
            enrolledChildren,
            presentToday,
            inBuildingNow,
            absentToday,
            notArrivedToday,
            ratios,
            unacknowledgedIncidents,
            recentIncidents,
            billing,
            attendanceTrend);
    }
}

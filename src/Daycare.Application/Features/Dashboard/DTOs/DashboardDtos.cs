using Daycare.Application.Features.Attendance.DTOs;
using Daycare.Domain.Enums;

namespace Daycare.Application.Features.Dashboard.DTOs;

public record DashboardIncidentDto(
    Guid Id,
    string ChildName,
    IncidentType Type,
    IncidentSeverity Severity,
    DateTime OccurredAt
);

/// <summary>
/// Billing summary for the admin dashboard.
/// </summary>
/// <param name="Month">Current month formatted as YYYY-MM.</param>
/// <param name="Invoiced">Total amount of non-void invoices with period in the current month.</param>
/// <param name="Collected">Total non-voided payments received in the current calendar month.</param>
/// <param name="Outstanding">Total unpaid balance of all non-void invoices, any month.</param>
/// <param name="OverdueCount">Count of non-void invoices past due and not fully paid, any month.</param>
/// <param name="Currency">Billing currency code (e.g., ETB).</param>
public record AdminDashboardBillingDto(
    string Month,
    decimal Invoiced,
    decimal Collected,
    decimal Outstanding,
    int OverdueCount,
    string Currency
);

public record AttendanceTrendItemDto(
    DateOnly Date,
    int Present
);

public record AdminDashboardDto(
    DateOnly Date,
    int EnrolledChildren,
    int PresentToday,
    int InBuildingNow,
    int AbsentToday,
    int NotArrivedToday,
    List<RatioItemDto> Ratios,
    int UnacknowledgedIncidents,
    List<DashboardIncidentDto> RecentIncidents,
    AdminDashboardBillingDto Billing,
    List<AttendanceTrendItemDto> AttendanceTrend
);

public record StaffDashboardClassroomDto(
    Guid ClassroomId,
    string ClassroomName,
    int TotalEnrolled,
    int PresentCount,
    int CheckedOutCount,
    int AbsentCount,
    int NotArrivedCount,
    RatioItemDto Ratio,
    int OpenIncidentsCount,
    List<DashboardIncidentDto> RecentIncidents
);

public record StaffDashboardDto(
    DateOnly Date,
    List<StaffDashboardClassroomDto> Classrooms
);

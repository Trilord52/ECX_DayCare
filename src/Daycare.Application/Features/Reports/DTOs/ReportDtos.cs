using Daycare.Domain.Enums;

namespace Daycare.Application.Features.Reports.DTOs;

public record AttendanceReportItemDto(
    DateOnly Date,
    Guid ChildId,
    string ChildName,
    Guid ClassroomId,
    string ClassroomName,
    string Status,
    DateTime? CheckInAt,
    DateTime? CheckOutAt,
    int LateMinutes,
    string? AbsenceReason
);

public record AttendanceReportDto(
    DateOnly From,
    DateOnly To,
    int TotalRecords,
    int TotalPresent,
    int TotalAbsences,
    int TotalLatePickups,
    int TotalLateMinutes,
    List<AttendanceReportItemDto> Items
);

public record BillingReportItemDto(
    Guid Id,
    string InvoiceNumber,
    Guid ChildId,
    string ChildName,
    Guid GuardianId,
    string GuardianName,
    DateOnly IssueDate,
    DateOnly DueDate,
    InvoiceStatus Status,
    decimal TotalAmount,
    decimal AmountPaid,
    decimal Balance,
    bool IsOverdue
);

public record BillingReportDto(
    DateOnly From,
    DateOnly To,
    int TotalInvoices,
    decimal TotalBilled,
    decimal TotalCollected,
    decimal TotalOutstanding,
    int OverdueInvoicesCount,
    string Currency,
    List<BillingReportItemDto> Items
);

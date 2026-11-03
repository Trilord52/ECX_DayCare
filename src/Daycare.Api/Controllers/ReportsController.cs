using System.Text;
using Daycare.Application.Features.Reports.AttendanceReport;
using Daycare.Application.Features.Reports.BillingReport;
using Daycare.Application.Features.Reports.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Daycare.Api.Controllers;

[Authorize(Roles = "Admin")]
public class ReportsController : BaseApiController
{
    private readonly GetAttendanceReportUseCase _getAttendanceReportUseCase;
    private readonly GetBillingReportUseCase _getBillingReportUseCase;

    public ReportsController(
        GetAttendanceReportUseCase getAttendanceReportUseCase,
        GetBillingReportUseCase getBillingReportUseCase)
    {
        _getAttendanceReportUseCase = getAttendanceReportUseCase;
        _getBillingReportUseCase = getBillingReportUseCase;
    }

    [HttpGet("attendance")]
    public async Task<IActionResult> GetAttendanceReport(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] Guid? classroomId,
        [FromQuery] string? format,
        CancellationToken ct)
    {
        var request = new GetAttendanceReportRequest(from, to, classroomId);
        var report = await _getAttendanceReportUseCase.ExecuteAsync(request, ct);

        if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
        {
            var csv = GenerateAttendanceCsv(report);
            var bytes = Encoding.UTF8.GetBytes(csv);
            return File(bytes, "text/csv", $"attendance-report-{report.From:yyyyMMdd}-{report.To:yyyyMMdd}.csv");
        }

        return Ok(report);
    }

    [HttpGet("billing")]
    public async Task<IActionResult> GetBillingReport(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? format,
        CancellationToken ct)
    {
        var request = new GetBillingReportRequest(from, to);
        var report = await _getBillingReportUseCase.ExecuteAsync(request, ct);

        if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
        {
            var csv = GenerateBillingCsv(report);
            var bytes = Encoding.UTF8.GetBytes(csv);
            return File(bytes, "text/csv", $"billing-report-{report.From:yyyyMMdd}-{report.To:yyyyMMdd}.csv");
        }

        return Ok(report);
    }

    private static string GenerateAttendanceCsv(AttendanceReportDto report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Date,ChildName,ClassroomName,Status,CheckInAt,CheckOutAt,LateMinutes,AbsenceReason");

        foreach (var item in report.Items)
        {
            sb.AppendLine(string.Join(",",
                EscapeCsv(item.Date.ToString("yyyy-MM-dd")),
                EscapeCsv(item.ChildName),
                EscapeCsv(item.ClassroomName),
                EscapeCsv(item.Status),
                EscapeCsv(item.CheckInAt?.ToString("yyyy-MM-ddTHH:mm:ssZ") ?? ""),
                EscapeCsv(item.CheckOutAt?.ToString("yyyy-MM-ddTHH:mm:ssZ") ?? ""),
                item.LateMinutes.ToString(),
                EscapeCsv(item.AbsenceReason ?? "")));
        }

        return sb.ToString();
    }

    private static string GenerateBillingCsv(BillingReportDto report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("InvoiceNumber,ChildName,GuardianName,IssueDate,DueDate,Status,TotalAmount,AmountPaid,Balance,IsOverdue");

        foreach (var item in report.Items)
        {
            sb.AppendLine(string.Join(",",
                EscapeCsv(item.InvoiceNumber),
                EscapeCsv(item.ChildName),
                EscapeCsv(item.GuardianName),
                EscapeCsv(item.IssueDate.ToString("yyyy-MM-dd")),
                EscapeCsv(item.DueDate.ToString("yyyy-MM-dd")),
                EscapeCsv(item.Status.ToString()),
                item.TotalAmount.ToString("F2"),
                item.AmountPaid.ToString("F2"),
                item.Balance.ToString("F2"),
                item.IsOverdue ? "true" : "false"));
        }

        return sb.ToString();
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }
}

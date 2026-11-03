using Daycare.Application.Features.Attendance.CheckIn;
using Daycare.Application.Features.Attendance.CheckOut;
using Daycare.Application.Features.Attendance.DTOs;
using Daycare.Application.Features.Attendance.GetAttendance;
using Daycare.Application.Features.Attendance.GetRatios;
using Daycare.Application.Features.Attendance.RecordAbsence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Daycare.Api.Controllers;

[Authorize(Roles = "Admin,Staff")]
public class AttendanceController : BaseApiController
{
    private readonly GetAttendanceRecordsUseCase _getAttendanceRecordsUseCase;
    private readonly CheckInUseCase _checkInUseCase;
    private readonly CheckOutUseCase _checkOutUseCase;
    private readonly RecordAbsenceUseCase _recordAbsenceUseCase;
    private readonly GetRatiosUseCase _getRatiosUseCase;

    public AttendanceController(
        GetAttendanceRecordsUseCase getAttendanceRecordsUseCase,
        CheckInUseCase checkInUseCase,
        CheckOutUseCase checkOutUseCase,
        RecordAbsenceUseCase recordAbsenceUseCase,
        GetRatiosUseCase getRatiosUseCase)
    {
        _getAttendanceRecordsUseCase = getAttendanceRecordsUseCase;
        _checkInUseCase = checkInUseCase;
        _checkOutUseCase = checkOutUseCase;
        _recordAbsenceUseCase = recordAbsenceUseCase;
        _getRatiosUseCase = getRatiosUseCase;
    }

    [HttpGet]
    public async Task<ActionResult<List<AttendanceRecordDto>>> GetAttendance(
        [FromQuery] DateOnly? date,
        [FromQuery] Guid? classroomId,
        CancellationToken ct)
    {
        var response = await _getAttendanceRecordsUseCase.ExecuteAsync(
            new GetAttendanceRecordsRequest(date, classroomId), ct);
        return Ok(response);
    }

    public record CheckInBody(
        Guid ChildId,
        Guid? DroppedOffByGuardianId = null,
        string? Notes = null
    );

    [HttpPost("check-in")]
    public async Task<ActionResult<CheckInResponse>> CheckIn(
        [FromBody] CheckInBody body,
        CancellationToken ct)
    {
        var request = new CheckInRequest(body.ChildId, body.DroppedOffByGuardianId, body.Notes);
        var response = await _checkInUseCase.ExecuteAsync(request, ct);
        return Ok(response);
    }

    public record CheckOutBody(
        Guid PickedUpByGuardianId,
        bool IdVerified
    );

    [HttpPost("{id:guid}/check-out")]
    public async Task<ActionResult<AttendanceRecordDto>> CheckOut(
        Guid id,
        [FromBody] CheckOutBody body,
        CancellationToken ct)
    {
        var request = new CheckOutRequest(id, body.PickedUpByGuardianId, body.IdVerified);
        var response = await _checkOutUseCase.ExecuteAsync(request, ct);
        return Ok(response);
    }

    public record RecordAbsenceBody(
        Guid ChildId,
        DateOnly Date,
        string Reason
    );

    [HttpPost("absences")]
    public async Task<ActionResult<AttendanceRecordDto>> RecordAbsence(
        [FromBody] RecordAbsenceBody body,
        CancellationToken ct)
    {
        var request = new RecordAbsenceRequest(body.ChildId, body.Date, body.Reason);
        var response = await _recordAbsenceUseCase.ExecuteAsync(request, ct);
        return Ok(response);
    }

    [HttpGet("ratios")]
    public async Task<ActionResult<List<RatioItemDto>>> GetRatios(CancellationToken ct)
    {
        var response = await _getRatiosUseCase.ExecuteAsync(new GetRatiosRequest(), ct);
        return Ok(response);
    }
}

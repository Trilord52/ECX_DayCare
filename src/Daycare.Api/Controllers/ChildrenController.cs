using Daycare.Application.Common.Exceptions;
using Daycare.Application.Common.Models;
using Daycare.Application.Features.Attendance.DTOs;
using Daycare.Application.Features.Attendance.GetAttendance;
using Daycare.Application.Features.Children.AssignChildClassroom;
using Daycare.Application.Features.Children.AssignChildFeePlan;
using Daycare.Application.Features.Children.ChildGuardians;
using Daycare.Application.Features.Children.CreateChild;
using Daycare.Application.Features.Children.DeleteChild;
using Daycare.Application.Features.Children.DTOs;
using Daycare.Application.Features.Children.GetChildren;
using Daycare.Application.Features.Children.Photos;
using Daycare.Application.Features.Children.UpdateChild;
using Daycare.Application.Features.DailyReports.AddDailyReportEntry;
using Daycare.Application.Features.DailyReports.DTOs;
using Daycare.Application.Features.DailyReports.GetDailyReportDetail;
using Daycare.Application.Features.DailyReports.GetDailyReportsSummary;
using Daycare.Application.Features.DailyReports.UpsertDailyReportHeader;
using Daycare.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Daycare.Api.Controllers;

public class ChildrenController : BaseApiController
{
    private readonly GetChildrenUseCase _getChildrenUseCase;
    private readonly GetChildByIdUseCase _getChildByIdUseCase;
    private readonly CreateChildUseCase _createChildUseCase;
    private readonly UpdateChildUseCase _updateChildUseCase;
    private readonly DeleteChildUseCase _deleteChildUseCase;
    private readonly AssignChildClassroomUseCase _assignChildClassroomUseCase;
    private readonly AssignChildFeePlanUseCase _assignChildFeePlanUseCase;
    private readonly GetChildGuardiansUseCase _getChildGuardiansUseCase;
    private readonly LinkGuardianUseCase _linkGuardianUseCase;
    private readonly UpdateChildGuardianUseCase _updateChildGuardianUseCase;
    private readonly UnlinkGuardianUseCase _unlinkGuardianUseCase;
    private readonly GetChildAttendanceHistoryUseCase _getChildAttendanceHistoryUseCase;
    private readonly GetDailyReportsSummaryUseCase _getDailyReportsSummaryUseCase;
    private readonly GetDailyReportDetailUseCase _getDailyReportDetailUseCase;
    private readonly UpsertDailyReportHeaderUseCase _upsertDailyReportHeaderUseCase;
    private readonly AddDailyReportEntryUseCase _addDailyReportEntryUseCase;
    private readonly SetChildPhotoUseCase _setChildPhotoUseCase;
    private readonly RemoveChildPhotoUseCase _removeChildPhotoUseCase;
    private readonly GetChildPhotoUseCase _getChildPhotoUseCase;

    public ChildrenController(
        GetChildrenUseCase getChildrenUseCase,
        GetChildByIdUseCase getChildByIdUseCase,
        CreateChildUseCase createChildUseCase,
        UpdateChildUseCase updateChildUseCase,
        DeleteChildUseCase deleteChildUseCase,
        AssignChildClassroomUseCase assignChildClassroomUseCase,
        AssignChildFeePlanUseCase assignChildFeePlanUseCase,
        GetChildGuardiansUseCase getChildGuardiansUseCase,
        LinkGuardianUseCase linkGuardianUseCase,
        UpdateChildGuardianUseCase updateChildGuardianUseCase,
        UnlinkGuardianUseCase unlinkGuardianUseCase,
        GetChildAttendanceHistoryUseCase getChildAttendanceHistoryUseCase,
        GetDailyReportsSummaryUseCase getDailyReportsSummaryUseCase,
        GetDailyReportDetailUseCase getDailyReportDetailUseCase,
        UpsertDailyReportHeaderUseCase upsertDailyReportHeaderUseCase,
        AddDailyReportEntryUseCase addDailyReportEntryUseCase,
        SetChildPhotoUseCase setChildPhotoUseCase,
        RemoveChildPhotoUseCase removeChildPhotoUseCase,
        GetChildPhotoUseCase getChildPhotoUseCase)
    {
        _getChildrenUseCase = getChildrenUseCase;
        _getChildByIdUseCase = getChildByIdUseCase;
        _createChildUseCase = createChildUseCase;
        _updateChildUseCase = updateChildUseCase;
        _deleteChildUseCase = deleteChildUseCase;
        _assignChildClassroomUseCase = assignChildClassroomUseCase;
        _assignChildFeePlanUseCase = assignChildFeePlanUseCase;
        _getChildGuardiansUseCase = getChildGuardiansUseCase;
        _linkGuardianUseCase = linkGuardianUseCase;
        _updateChildGuardianUseCase = updateChildGuardianUseCase;
        _unlinkGuardianUseCase = unlinkGuardianUseCase;
        _getChildAttendanceHistoryUseCase = getChildAttendanceHistoryUseCase;
        _getDailyReportsSummaryUseCase = getDailyReportsSummaryUseCase;
        _getDailyReportDetailUseCase = getDailyReportDetailUseCase;
        _upsertDailyReportHeaderUseCase = upsertDailyReportHeaderUseCase;
        _addDailyReportEntryUseCase = addDailyReportEntryUseCase;
        _setChildPhotoUseCase = setChildPhotoUseCase;
        _removeChildPhotoUseCase = removeChildPhotoUseCase;
        _getChildPhotoUseCase = getChildPhotoUseCase;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Staff,Parent")]
    public async Task<ActionResult<PagedResult<ChildListItemDto>>> GetChildren(
        [FromQuery] string? search,
        [FromQuery] Guid? classroomId,
        [FromQuery] ChildStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var response = await _getChildrenUseCase.ExecuteAsync(
            new GetChildrenRequest(search, classroomId, status, page, pageSize), ct);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,Staff,Parent")]
    public async Task<ActionResult<ChildDetailDto>> GetChildById(Guid id, CancellationToken ct)
    {
        var response = await _getChildByIdUseCase.ExecuteAsync(new GetChildByIdRequest(id), ct);
        return Ok(response);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ChildListItemDto>> CreateChild(
        [FromBody] CreateChildRequest request,
        CancellationToken ct)
    {
        var response = await _createChildUseCase.ExecuteAsync(request, ct);
        return CreatedAtAction(nameof(GetChildById), new { id = response.Id }, response);
    }

    public record UpdateChildBody(
        string FirstName,
        string LastName,
        DateOnly DateOfBirth,
        Gender Gender,
        DateOnly EnrollmentDate,
        string? Allergies = null,
        string? MedicalNotes = null,
        string? DietaryNotes = null
    );

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ChildListItemDto>> UpdateChild(
        Guid id,
        [FromBody] UpdateChildBody body,
        CancellationToken ct)
    {
        var request = new UpdateChildRequest(
            id, body.FirstName, body.LastName, body.DateOfBirth, body.Gender,
            body.EnrollmentDate, body.Allergies, body.MedicalNotes, body.DietaryNotes);

        var response = await _updateChildUseCase.ExecuteAsync(request, ct);
        return Ok(response);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<DeleteChildResponse>> DeleteChild(Guid id, CancellationToken ct)
    {
        var response = await _deleteChildUseCase.ExecuteAsync(new DeleteChildRequest(id), ct);
        return Ok(response);
    }

    public record AssignClassroomBody(Guid? ClassroomId);

    [HttpPut("{id:guid}/classroom")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AssignChildClassroomResponse>> AssignClassroom(
        Guid id,
        [FromBody] AssignClassroomBody body,
        CancellationToken ct)
    {
        var request = new AssignChildClassroomRequest(id, body.ClassroomId);
        var response = await _assignChildClassroomUseCase.ExecuteAsync(request, ct);
        return Ok(response);
    }

    public record AssignFeePlanBody(Guid? FeePlanId);

    [HttpPut("{id:guid}/fee-plan")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AssignChildFeePlanResponse>> AssignFeePlan(
        Guid id,
        [FromBody] AssignFeePlanBody body,
        CancellationToken ct)
    {
        var request = new AssignChildFeePlanRequest(id, body.FeePlanId);
        var response = await _assignChildFeePlanUseCase.ExecuteAsync(request, ct);
        return Ok(response);
    }

    [HttpGet("{id:guid}/guardians")]
    [Authorize(Roles = "Admin,Staff,Parent")]
    public async Task<ActionResult<List<ChildGuardianDto>>> GetGuardians(Guid id, CancellationToken ct)
    {
        var response = await _getChildGuardiansUseCase.ExecuteAsync(new GetChildGuardiansRequest(id), ct);
        return Ok(response);
    }

    public record LinkGuardianBody(
        Guid GuardianId,
        Relationship Relationship,
        bool IsPrimaryContact = false,
        bool CanPickUp = true,
        bool IsPayer = false,
        bool IsEmergencyContact = false
    );

    [HttpPost("{id:guid}/guardians")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ChildGuardianDto>> LinkGuardian(
        Guid id,
        [FromBody] LinkGuardianBody body,
        CancellationToken ct)
    {
        var request = new LinkGuardianRequest(
            id, body.GuardianId, body.Relationship, body.IsPrimaryContact,
            body.CanPickUp, body.IsPayer, body.IsEmergencyContact);

        var response = await _linkGuardianUseCase.ExecuteAsync(request, ct);
        return Ok(response);
    }

    public record UpdateGuardianLinkBody(
        Relationship Relationship,
        bool IsPrimaryContact,
        bool CanPickUp,
        bool IsPayer,
        bool IsEmergencyContact
    );

    [HttpPut("{id:guid}/guardians/{guardianId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ChildGuardianDto>> UpdateGuardianLink(
        Guid id,
        Guid guardianId,
        [FromBody] UpdateGuardianLinkBody body,
        CancellationToken ct)
    {
        var request = new UpdateChildGuardianRequest(
            id, guardianId, body.Relationship, body.IsPrimaryContact,
            body.CanPickUp, body.IsPayer, body.IsEmergencyContact);

        var response = await _updateChildGuardianUseCase.ExecuteAsync(request, ct);
        return Ok(response);
    }

    [HttpDelete("{id:guid}/guardians/{guardianId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UnlinkGuardianResponse>> UnlinkGuardian(
        Guid id,
        Guid guardianId,
        CancellationToken ct)
    {
        var response = await _unlinkGuardianUseCase.ExecuteAsync(new UnlinkGuardianRequest(id, guardianId), ct);
        return Ok(response);
    }

    [HttpGet("{id:guid}/attendance")]
    [Authorize(Roles = "Admin,Staff,Parent")]
    public async Task<ActionResult<List<AttendanceRecordDto>>> GetChildAttendance(
        Guid id,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken ct)
    {
        var response = await _getChildAttendanceHistoryUseCase.ExecuteAsync(
            new GetChildAttendanceHistoryRequest(id, from, to), ct);
        return Ok(response);
    }

    [HttpGet("{id:guid}/daily-reports")]
    [Authorize(Roles = "Admin,Staff,Parent")]
    public async Task<ActionResult<List<DailyReportSummaryDto>>> GetDailyReports(
        Guid id,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken ct)
    {
        var response = await _getDailyReportsSummaryUseCase.ExecuteAsync(
            new GetDailyReportsSummaryRequest(id, from, to), ct);
        return Ok(response);
    }

    [HttpGet("{id:guid}/daily-reports/{date}")]
    [Authorize(Roles = "Admin,Staff,Parent")]
    public async Task<ActionResult<DailyReportDetailDto>> GetDailyReportDetail(
        Guid id,
        DateOnly date,
        CancellationToken ct)
    {
        var response = await _getDailyReportDetailUseCase.ExecuteAsync(
            new GetDailyReportDetailRequest(id, date), ct);
        return Ok(response);
    }

    public record UpsertDailyReportHeaderBody(Mood? Mood, string? Notes);

    [HttpPut("{id:guid}/daily-reports/{date}")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<ActionResult<DailyReportDetailDto>> UpsertDailyReportHeader(
        Guid id,
        DateOnly date,
        [FromBody] UpsertDailyReportHeaderBody body,
        CancellationToken ct)
    {
        var response = await _upsertDailyReportHeaderUseCase.ExecuteAsync(
            new UpsertDailyReportHeaderRequest(id, date, body.Mood, body.Notes), ct);
        return Ok(response);
    }

    public record AddDailyReportEntryBody(
        EntryType EntryType,
        DateTime OccurredAt,
        DateTime? EndedAt = null,
        MealType? MealType = null,
        AmountEaten? AmountEaten = null,
        DiaperType? DiaperType = null,
        string? Description = null
    );

    [HttpPost("{id:guid}/daily-reports/{date}/entries")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<ActionResult<DailyReportEntryDto>> AddDailyReportEntry(
        Guid id,
        DateOnly date,
        [FromBody] AddDailyReportEntryBody body,
        CancellationToken ct)
    {
        var response = await _addDailyReportEntryUseCase.ExecuteAsync(
            new AddDailyReportEntryRequest(
                id,
                date,
                body.EntryType,
                body.OccurredAt,
                body.EndedAt,
                body.MealType,
                body.AmountEaten,
                body.DiaperType,
                body.Description), ct);
        return Ok(response);
    }

    [HttpPut("{id:guid}/photo")]
    [Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<ActionResult<PhotoUploadResultDto>> SetChildPhoto(
        Guid id,
        [FromForm] IFormFile? file,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
        {
            throw new ValidationFailedException("file", "File is empty.");
        }

        await using var stream = file.OpenReadStream();
        var result = await _setChildPhotoUseCase.ExecuteAsync(
            new SetChildPhotoRequest(id, stream, file.Length), ct);
        return Ok(result);
    }

    [HttpDelete("{id:guid}/photo")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoveChildPhoto(Guid id, CancellationToken ct)
    {
        await _removeChildPhotoUseCase.ExecuteAsync(new RemoveChildPhotoRequest(id), ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/photo")]
    [Authorize(Roles = "Admin,Staff,Parent")]
    public async Task<IActionResult> GetChildPhoto(Guid id, CancellationToken ct)
    {
        var result = await _getChildPhotoUseCase.ExecuteAsync(new GetChildPhotoRequest(id), ct);
        var etag = $"\"{result.UpdatedAt.ToUniversalTime().Ticks}\"";

        if (Request.Headers.TryGetValue("If-None-Match", out var incomingEtag) && incomingEtag == etag)
        {
            return StatusCode(StatusCodes.Status304NotModified);
        }

        Response.Headers.CacheControl = "private, no-cache";
        Response.Headers.ETag = etag;
        Response.Headers["X-Content-Type-Options"] = "nosniff";

        return File(result.Stream, "image/jpeg");
    }
}

using Daycare.Application.Common.Exceptions;
using Daycare.Application.Common.Models;
using Daycare.Application.Features.Staff.CreateStaff;
using Daycare.Application.Features.Staff.DeleteStaff;
using Daycare.Application.Features.Staff.DTOs;
using Daycare.Application.Features.Staff.GetStaff;
using Daycare.Application.Features.Staff.Photos;
using Daycare.Application.Features.Staff.ResetStaffPassword;
using Daycare.Application.Features.Staff.UpdateStaff;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Daycare.Api.Controllers;

[Authorize]
public class StaffController : BaseApiController
{
    private readonly GetStaffListUseCase _getStaffListUseCase;
    private readonly GetStaffByIdUseCase _getStaffByIdUseCase;
    private readonly CreateStaffUseCase _createStaffUseCase;
    private readonly UpdateStaffUseCase _updateStaffUseCase;
    private readonly DeleteStaffUseCase _deleteStaffUseCase;
    private readonly ResetStaffPasswordUseCase _resetStaffPasswordUseCase;
    private readonly SetStaffPhotoUseCase _setStaffPhotoUseCase;
    private readonly RemoveStaffPhotoUseCase _removeStaffPhotoUseCase;
    private readonly GetStaffPhotoUseCase _getStaffPhotoUseCase;

    public StaffController(
        GetStaffListUseCase getStaffListUseCase,
        GetStaffByIdUseCase getStaffByIdUseCase,
        CreateStaffUseCase createStaffUseCase,
        UpdateStaffUseCase updateStaffUseCase,
        DeleteStaffUseCase deleteStaffUseCase,
        ResetStaffPasswordUseCase resetStaffPasswordUseCase,
        SetStaffPhotoUseCase setStaffPhotoUseCase,
        RemoveStaffPhotoUseCase removeStaffPhotoUseCase,
        GetStaffPhotoUseCase getStaffPhotoUseCase)
    {
        _getStaffListUseCase = getStaffListUseCase;
        _getStaffByIdUseCase = getStaffByIdUseCase;
        _createStaffUseCase = createStaffUseCase;
        _updateStaffUseCase = updateStaffUseCase;
        _deleteStaffUseCase = deleteStaffUseCase;
        _resetStaffPasswordUseCase = resetStaffPasswordUseCase;
        _setStaffPhotoUseCase = setStaffPhotoUseCase;
        _removeStaffPhotoUseCase = removeStaffPhotoUseCase;
        _getStaffPhotoUseCase = getStaffPhotoUseCase;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<StaffDto>>> GetStaff(CancellationToken ct)
    {
        var response = await _getStaffListUseCase.ExecuteAsync(new GetStaffListRequest(), ct);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<StaffDto>> GetStaffById(Guid id, CancellationToken ct)
    {
        var response = await _getStaffByIdUseCase.ExecuteAsync(new GetStaffByIdRequest(id), ct);
        return Ok(response);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CreateStaffResponse>> CreateStaff(
        [FromBody] CreateStaffRequest request,
        CancellationToken ct)
    {
        var response = await _createStaffUseCase.ExecuteAsync(request, ct);
        return CreatedAtAction(nameof(GetStaffById), new { id = response.Staff.Id }, response);
    }

    public record UpdateStaffBody(
        string FirstName,
        string LastName,
        string Phone,
        string JobTitle,
        DateOnly HireDate,
        List<Guid>? ClassroomIds = null
    );

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<StaffDto>> UpdateStaff(
        Guid id,
        [FromBody] UpdateStaffBody body,
        CancellationToken ct)
    {
        var request = new UpdateStaffRequest(
            id, body.FirstName, body.LastName, body.Phone, body.JobTitle, body.HireDate, body.ClassroomIds);

        var response = await _updateStaffUseCase.ExecuteAsync(request, ct);
        return Ok(response);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<DeleteStaffResponse>> DeleteStaff(Guid id, CancellationToken ct)
    {
        var response = await _deleteStaffUseCase.ExecuteAsync(new DeleteStaffRequest(id), ct);
        return Ok(response);
    }

    [HttpPost("{id:guid}/reset-password")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ResetStaffPasswordResponse>> ResetPassword(Guid id, CancellationToken ct)
    {
        var response = await _resetStaffPasswordUseCase.ExecuteAsync(new ResetStaffPasswordRequest(id), ct);
        return Ok(response);
    }

    [HttpPut("{id:guid}/photo")]
    [Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<ActionResult<PhotoUploadResultDto>> SetStaffPhoto(
        Guid id,
        [FromForm] IFormFile? file,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
        {
            throw new ValidationFailedException("file", "File is empty.");
        }

        await using var stream = file.OpenReadStream();
        var result = await _setStaffPhotoUseCase.ExecuteAsync(
            new SetStaffPhotoRequest(id, stream, file.Length), ct);
        return Ok(result);
    }

    [HttpDelete("{id:guid}/photo")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoveStaffPhoto(Guid id, CancellationToken ct)
    {
        await _removeStaffPhotoUseCase.ExecuteAsync(new RemoveStaffPhotoRequest(id), ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/photo")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> GetStaffPhoto(Guid id, CancellationToken ct)
    {
        var result = await _getStaffPhotoUseCase.ExecuteAsync(new GetStaffPhotoRequest(id), ct);
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

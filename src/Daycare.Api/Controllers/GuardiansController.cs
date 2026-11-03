using Daycare.Application.Common.Exceptions;
using Daycare.Application.Common.Models;
using Daycare.Application.Features.Guardians.CreateGuardian;
using Daycare.Application.Features.Guardians.CreateGuardianAccount;
using Daycare.Application.Features.Guardians.DeleteGuardian;
using Daycare.Application.Features.Guardians.DTOs;
using Daycare.Application.Features.Guardians.GetGuardians;
using Daycare.Application.Features.Guardians.Photos;
using Daycare.Application.Features.Guardians.ResetGuardianPassword;
using Daycare.Application.Features.Guardians.UpdateGuardian;
using Daycare.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Daycare.Api.Controllers;

[Authorize]
public class GuardiansController : BaseApiController
{
    private readonly GetGuardiansUseCase _getGuardiansUseCase;
    private readonly GetGuardianByIdUseCase _getGuardianByIdUseCase;
    private readonly CreateGuardianUseCase _createGuardianUseCase;
    private readonly UpdateGuardianUseCase _updateGuardianUseCase;
    private readonly DeleteGuardianUseCase _deleteGuardianUseCase;
    private readonly CreateGuardianAccountUseCase _createGuardianAccountUseCase;
    private readonly ResetGuardianPasswordUseCase _resetGuardianPasswordUseCase;
    private readonly SetGuardianPhotoUseCase _setGuardianPhotoUseCase;
    private readonly RemoveGuardianPhotoUseCase _removeGuardianPhotoUseCase;
    private readonly GetGuardianPhotoUseCase _getGuardianPhotoUseCase;

    public GuardiansController(
        GetGuardiansUseCase getGuardiansUseCase,
        GetGuardianByIdUseCase getGuardianByIdUseCase,
        CreateGuardianUseCase createGuardianUseCase,
        UpdateGuardianUseCase updateGuardianUseCase,
        DeleteGuardianUseCase deleteGuardianUseCase,
        CreateGuardianAccountUseCase createGuardianAccountUseCase,
        ResetGuardianPasswordUseCase resetGuardianPasswordUseCase,
        SetGuardianPhotoUseCase setGuardianPhotoUseCase,
        RemoveGuardianPhotoUseCase removeGuardianPhotoUseCase,
        GetGuardianPhotoUseCase getGuardianPhotoUseCase)
    {
        _getGuardiansUseCase = getGuardiansUseCase;
        _getGuardianByIdUseCase = getGuardianByIdUseCase;
        _createGuardianUseCase = createGuardianUseCase;
        _updateGuardianUseCase = updateGuardianUseCase;
        _deleteGuardianUseCase = deleteGuardianUseCase;
        _createGuardianAccountUseCase = createGuardianAccountUseCase;
        _resetGuardianPasswordUseCase = resetGuardianPasswordUseCase;
        _setGuardianPhotoUseCase = setGuardianPhotoUseCase;
        _removeGuardianPhotoUseCase = removeGuardianPhotoUseCase;
        _getGuardianPhotoUseCase = getGuardianPhotoUseCase;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PagedResult<GuardianDto>>> GetGuardians(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var response = await _getGuardiansUseCase.ExecuteAsync(
            new GetGuardiansRequest(search, page, pageSize), ct);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<GuardianDto>> GetGuardianById(Guid id, CancellationToken ct)
    {
        var response = await _getGuardianByIdUseCase.ExecuteAsync(new GetGuardianByIdRequest(id), ct);
        return Ok(response);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CreateGuardianResponse>> CreateGuardian(
        [FromBody] CreateGuardianRequest request,
        CancellationToken ct)
    {
        var response = await _createGuardianUseCase.ExecuteAsync(request, ct);
        return CreatedAtAction(nameof(GetGuardianById), new { id = response.Guardian.Id }, response);
    }

    public record UpdateGuardianBody(
        string FirstName,
        string LastName,
        string Phone,
        string? Email = null,
        string? Address = null
    );

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<GuardianDto>> UpdateGuardian(
        Guid id,
        [FromBody] UpdateGuardianBody body,
        CancellationToken ct)
    {
        var request = new UpdateGuardianRequest(
            id, body.FirstName, body.LastName, body.Phone,
            body.Email, body.Address);

        var response = await _updateGuardianUseCase.ExecuteAsync(request, ct);
        return Ok(response);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<DeleteGuardianResponse>> DeleteGuardian(Guid id, CancellationToken ct)
    {
        var response = await _deleteGuardianUseCase.ExecuteAsync(new DeleteGuardianRequest(id), ct);
        return Ok(response);
    }

    public record CreateAccountBody(string Email);

    [HttpPost("{id:guid}/account")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CreateGuardianAccountResponse>> CreateAccount(
        Guid id,
        [FromBody] CreateAccountBody body,
        CancellationToken ct)
    {
        var request = new CreateGuardianAccountRequest(id, body.Email);
        var response = await _createGuardianAccountUseCase.ExecuteAsync(request, ct);
        return Ok(response);
    }

    [HttpPost("{id:guid}/reset-password")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ResetGuardianPasswordResponse>> ResetPassword(Guid id, CancellationToken ct)
    {
        var response = await _resetGuardianPasswordUseCase.ExecuteAsync(new ResetGuardianPasswordRequest(id), ct);
        return Ok(response);
    }

    [HttpPut("{id:guid}/photo")]
    [Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<ActionResult<PhotoUploadResultDto>> SetGuardianPhoto(
        Guid id,
        [FromForm] IFormFile? file,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
        {
            throw new ValidationFailedException("file", "File is empty.");
        }

        await using var stream = file.OpenReadStream();
        var result = await _setGuardianPhotoUseCase.ExecuteAsync(
            new SetGuardianPhotoRequest(id, stream, file.Length), ct);
        return Ok(result);
    }

    [HttpDelete("{id:guid}/photo")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoveGuardianPhoto(Guid id, CancellationToken ct)
    {
        await _removeGuardianPhotoUseCase.ExecuteAsync(new RemoveGuardianPhotoRequest(id), ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/photo")]
    [Authorize(Roles = "Admin,Staff,Parent")]
    public async Task<IActionResult> GetGuardianPhoto(Guid id, CancellationToken ct)
    {
        var result = await _getGuardianPhotoUseCase.ExecuteAsync(new GetGuardianPhotoRequest(id), ct);
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

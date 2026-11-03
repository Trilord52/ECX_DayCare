using Daycare.Application.Common.Models;
using Daycare.Application.Features.Announcements.CreateAnnouncement;
using Daycare.Application.Features.Announcements.DeleteAnnouncement;
using Daycare.Application.Features.Announcements.DTOs;
using Daycare.Application.Features.Announcements.GetAnnouncements;
using Daycare.Application.Features.Announcements.UpdateAnnouncement;
using Daycare.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Daycare.Api.Controllers;

public class AnnouncementsController : BaseApiController
{
    private readonly GetAnnouncementsUseCase _getAnnouncementsUseCase;
    private readonly CreateAnnouncementUseCase _createAnnouncementUseCase;
    private readonly UpdateAnnouncementUseCase _updateAnnouncementUseCase;
    private readonly DeleteAnnouncementUseCase _deleteAnnouncementUseCase;

    public AnnouncementsController(
        GetAnnouncementsUseCase getAnnouncementsUseCase,
        CreateAnnouncementUseCase createAnnouncementUseCase,
        UpdateAnnouncementUseCase updateAnnouncementUseCase,
        DeleteAnnouncementUseCase deleteAnnouncementUseCase)
    {
        _getAnnouncementsUseCase = getAnnouncementsUseCase;
        _createAnnouncementUseCase = createAnnouncementUseCase;
        _updateAnnouncementUseCase = updateAnnouncementUseCase;
        _deleteAnnouncementUseCase = deleteAnnouncementUseCase;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Staff,Parent")]
    public async Task<ActionResult<PagedResult<AnnouncementDto>>> GetAnnouncements(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var response = await _getAnnouncementsUseCase.ExecuteAsync(
            new GetAnnouncementsRequest(page, pageSize), ct);
        return Ok(response);
    }

    public record CreateAnnouncementBody(
        string Title,
        string Body,
        AnnouncementAudience Audience,
        Guid? ClassroomId = null,
        DateTime? ExpiresAt = null
    );

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AnnouncementDto>> CreateAnnouncement(
        [FromBody] CreateAnnouncementBody body,
        CancellationToken ct)
    {
        var response = await _createAnnouncementUseCase.ExecuteAsync(
            new CreateAnnouncementRequest(
                body.Title,
                body.Body,
                body.Audience,
                body.ClassroomId,
                body.ExpiresAt), ct);

        return CreatedAtAction(nameof(GetAnnouncements), new { id = response.Id }, response);
    }

    public record UpdateAnnouncementBody(
        string Title,
        string Body,
        AnnouncementAudience Audience,
        Guid? ClassroomId = null,
        DateTime? ExpiresAt = null
    );

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AnnouncementDto>> UpdateAnnouncement(
        Guid id,
        [FromBody] UpdateAnnouncementBody body,
        CancellationToken ct)
    {
        var response = await _updateAnnouncementUseCase.ExecuteAsync(
            new UpdateAnnouncementRequest(
                id,
                body.Title,
                body.Body,
                body.Audience,
                body.ClassroomId,
                body.ExpiresAt), ct);

        return Ok(response);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> DeleteAnnouncement(
        Guid id,
        CancellationToken ct)
    {
        await _deleteAnnouncementUseCase.ExecuteAsync(
            new DeleteAnnouncementRequest(id), ct);
        return NoContent();
    }
}


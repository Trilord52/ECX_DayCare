using Daycare.Application.Common.Models;
using Daycare.Application.Features.Incidents.AcknowledgeIncident;
using Daycare.Application.Features.Incidents.CreateIncident;
using Daycare.Application.Features.Incidents.DTOs;
using Daycare.Application.Features.Incidents.GetIncidentById;
using Daycare.Application.Features.Incidents.GetIncidents;
using Daycare.Application.Features.Incidents.NotifyParentIncident;
using Daycare.Application.Features.Incidents.UpdateIncident;
using Daycare.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Daycare.Api.Controllers;

public class IncidentsController : BaseApiController
{
    private readonly GetIncidentsUseCase _getIncidentsUseCase;
    private readonly GetIncidentByIdUseCase _getIncidentByIdUseCase;
    private readonly CreateIncidentUseCase _createIncidentUseCase;
    private readonly UpdateIncidentUseCase _updateIncidentUseCase;
    private readonly NotifyParentIncidentUseCase _notifyParentIncidentUseCase;
    private readonly AcknowledgeIncidentUseCase _acknowledgeIncidentUseCase;

    public IncidentsController(
        GetIncidentsUseCase getIncidentsUseCase,
        GetIncidentByIdUseCase getIncidentByIdUseCase,
        CreateIncidentUseCase createIncidentUseCase,
        UpdateIncidentUseCase updateIncidentUseCase,
        NotifyParentIncidentUseCase notifyParentIncidentUseCase,
        AcknowledgeIncidentUseCase acknowledgeIncidentUseCase)
    {
        _getIncidentsUseCase = getIncidentsUseCase;
        _getIncidentByIdUseCase = getIncidentByIdUseCase;
        _createIncidentUseCase = createIncidentUseCase;
        _updateIncidentUseCase = updateIncidentUseCase;
        _notifyParentIncidentUseCase = notifyParentIncidentUseCase;
        _acknowledgeIncidentUseCase = acknowledgeIncidentUseCase;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Staff,Parent")]
    public async Task<ActionResult<PagedResult<IncidentListItemDto>>> GetIncidents(
        [FromQuery] Guid? childId,
        [FromQuery] Guid? classroomId,
        [FromQuery] IncidentSeverity? severity,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] bool? acknowledged,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var response = await _getIncidentsUseCase.ExecuteAsync(
            new GetIncidentsRequest(
                childId,
                classroomId,
                severity,
                from,
                to,
                acknowledged,
                page,
                pageSize), ct);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,Staff,Parent")]
    public async Task<ActionResult<IncidentDetailDto>> GetIncidentById(
        Guid id,
        CancellationToken ct)
    {
        var response = await _getIncidentByIdUseCase.ExecuteAsync(
            new GetIncidentByIdRequest(id), ct);
        return Ok(response);
    }

    public record CreateIncidentBody(
        Guid ChildId,
        DateTime OccurredAt,
        IncidentType Type,
        IncidentSeverity Severity,
        string Description,
        Guid? ClassroomId = null,
        string? ActionTaken = null,
        DateTime? ParentNotifiedAt = null,
        string? ParentNotifiedByName = null
    );

    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<ActionResult<IncidentDetailDto>> CreateIncident(
        [FromBody] CreateIncidentBody body,
        CancellationToken ct)
    {
        var response = await _createIncidentUseCase.ExecuteAsync(
            new CreateIncidentRequest(
                body.ChildId,
                body.OccurredAt,
                body.Type,
                body.Severity,
                body.Description,
                body.ClassroomId,
                body.ActionTaken,
                body.ParentNotifiedAt,
                body.ParentNotifiedByName), ct);

        return CreatedAtAction(nameof(GetIncidentById), new { id = response.Id }, response);
    }

    public record UpdateIncidentBody(
        DateTime OccurredAt,
        IncidentType Type,
        IncidentSeverity Severity,
        string Description,
        string? ActionTaken = null,
        DateTime? ParentNotifiedAt = null,
        string? ParentNotifiedByName = null
    );

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<ActionResult<IncidentDetailDto>> UpdateIncident(
        Guid id,
        [FromBody] UpdateIncidentBody body,
        CancellationToken ct)
    {
        var response = await _updateIncidentUseCase.ExecuteAsync(
            new UpdateIncidentRequest(
                id,
                body.OccurredAt,
                body.Type,
                body.Severity,
                body.Description,
                body.ActionTaken,
                body.ParentNotifiedAt,
                body.ParentNotifiedByName), ct);

        return Ok(response);
    }

    [HttpPost("{id:guid}/notify-parent")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<ActionResult<IncidentDetailDto>> NotifyParent(
        Guid id,
        CancellationToken ct)
    {
        var response = await _notifyParentIncidentUseCase.ExecuteAsync(
            new NotifyParentIncidentRequest(id), ct);
        return Ok(response);
    }

    [HttpPost("{id:guid}/acknowledge")]
    [Authorize(Roles = "Parent")]
    public async Task<ActionResult<IncidentDetailDto>> Acknowledge(
        Guid id,
        CancellationToken ct)
    {
        var response = await _acknowledgeIncidentUseCase.ExecuteAsync(
            new AcknowledgeIncidentRequest(id), ct);
        return Ok(response);
    }
}


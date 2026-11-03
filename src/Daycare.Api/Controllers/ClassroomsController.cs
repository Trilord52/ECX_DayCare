using Daycare.Application.Features.Attendance.DTOs;
using Daycare.Application.Features.Attendance.GetRoster;
using Daycare.Application.Features.Classrooms.AssignClassroomStaff;
using Daycare.Application.Features.Classrooms.CreateClassroom;
using Daycare.Application.Features.Classrooms.DTOs;
using Daycare.Application.Features.Classrooms.GetClassroomById;
using Daycare.Application.Features.Classrooms.GetClassrooms;
using Daycare.Application.Features.Classrooms.UpdateClassroom;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Daycare.Api.Controllers;

public class ClassroomsController : BaseApiController
{
    private readonly GetClassroomsUseCase _getClassroomsUseCase;
    private readonly GetClassroomByIdUseCase _getClassroomByIdUseCase;
    private readonly CreateClassroomUseCase _createClassroomUseCase;
    private readonly UpdateClassroomUseCase _updateClassroomUseCase;
    private readonly AssignClassroomStaffUseCase _assignClassroomStaffUseCase;
    private readonly GetRosterUseCase _getRosterUseCase;

    public ClassroomsController(
        GetClassroomsUseCase getClassroomsUseCase,
        GetClassroomByIdUseCase getClassroomByIdUseCase,
        CreateClassroomUseCase createClassroomUseCase,
        UpdateClassroomUseCase updateClassroomUseCase,
        AssignClassroomStaffUseCase assignClassroomStaffUseCase,
        GetRosterUseCase getRosterUseCase)
    {
        _getClassroomsUseCase = getClassroomsUseCase;
        _getClassroomByIdUseCase = getClassroomByIdUseCase;
        _createClassroomUseCase = createClassroomUseCase;
        _updateClassroomUseCase = updateClassroomUseCase;
        _assignClassroomStaffUseCase = assignClassroomStaffUseCase;
        _getRosterUseCase = getRosterUseCase;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<ActionResult<List<ClassroomDto>>> GetClassrooms(CancellationToken ct)
    {
        var response = await _getClassroomsUseCase.ExecuteAsync(new GetClassroomsRequest(), ct);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<ActionResult<ClassroomDetailDto>> GetClassroomById(Guid id, CancellationToken ct)
    {
        var response = await _getClassroomByIdUseCase.ExecuteAsync(new GetClassroomByIdRequest(id), ct);
        return Ok(response);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ClassroomDto>> CreateClassroom(
        [FromBody] CreateClassroomRequest request,
        CancellationToken ct)
    {
        var response = await _createClassroomUseCase.ExecuteAsync(request, ct);
        return CreatedAtAction(nameof(GetClassroomById), new { id = response.Id }, response);
    }

    public record UpdateClassroomBody(
        string Name,
        string? Description,
        int MinAgeMonths,
        int MaxAgeMonths,
        int Capacity,
        int MaxChildrenPerStaff,
        bool IsActive
    );

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ClassroomDto>> UpdateClassroom(
        Guid id,
        [FromBody] UpdateClassroomBody body,
        CancellationToken ct)
    {
        var request = new UpdateClassroomRequest(
            id, body.Name, body.Description, body.MinAgeMonths, body.MaxAgeMonths,
            body.Capacity, body.MaxChildrenPerStaff, body.IsActive);

        var response = await _updateClassroomUseCase.ExecuteAsync(request, ct);
        return Ok(response);
    }

    public record AssignStaffBody(List<Guid> StaffIds);

    [HttpPut("{id:guid}/staff")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AssignClassroomStaffResponse>> AssignStaff(
        Guid id,
        [FromBody] AssignStaffBody body,
        CancellationToken ct)
    {
        var request = new AssignClassroomStaffRequest(id, body.StaffIds ?? []);
        var response = await _assignClassroomStaffUseCase.ExecuteAsync(request, ct);
        return Ok(response);
    }

    [HttpGet("{id:guid}/roster")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<ActionResult<List<RosterItemDto>>> GetRoster(
        Guid id,
        [FromQuery] DateOnly? date,
        CancellationToken ct)
    {
        var response = await _getRosterUseCase.ExecuteAsync(new GetRosterRequest(id, date), ct);
        return Ok(response);
    }
}

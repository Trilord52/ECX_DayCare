using Daycare.Application.Features.Dashboard.AdminDashboard;
using Daycare.Application.Features.Dashboard.DTOs;
using Daycare.Application.Features.Dashboard.StaffDashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Daycare.Api.Controllers;

[Route("api/v1/[controller]")]
public class DashboardController : BaseApiController
{
    private readonly GetAdminDashboardUseCase _getAdminDashboardUseCase;
    private readonly GetStaffDashboardUseCase _getStaffDashboardUseCase;

    public DashboardController(
        GetAdminDashboardUseCase getAdminDashboardUseCase,
        GetStaffDashboardUseCase getStaffDashboardUseCase)
    {
        _getAdminDashboardUseCase = getAdminDashboardUseCase;
        _getStaffDashboardUseCase = getStaffDashboardUseCase;
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AdminDashboardDto>> GetAdminDashboard(CancellationToken ct)
    {
        var result = await _getAdminDashboardUseCase.ExecuteAsync(new GetAdminDashboardRequest(), ct);
        return Ok(result);
    }

    [HttpGet("staff")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<ActionResult<StaffDashboardDto>> GetStaffDashboard(CancellationToken ct)
    {
        var result = await _getStaffDashboardUseCase.ExecuteAsync(new GetStaffDashboardRequest(), ct);
        return Ok(result);
    }
}

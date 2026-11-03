using Daycare.Application.Features.Billing.FeePlans.CreateFeePlan;
using Daycare.Application.Features.Billing.FeePlans.DTOs;
using Daycare.Application.Features.Billing.FeePlans.GetFeePlans;
using Daycare.Application.Features.Billing.FeePlans.UpdateFeePlan;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Daycare.Api.Controllers;

[Authorize(Roles = "Admin")]
[Route("api/v1/fee-plans")]
public class FeePlansController : BaseApiController
{
    private readonly GetFeePlansUseCase _getFeePlansUseCase;
    private readonly CreateFeePlanUseCase _createFeePlanUseCase;
    private readonly UpdateFeePlanUseCase _updateFeePlanUseCase;

    public FeePlansController(
        GetFeePlansUseCase getFeePlansUseCase,
        CreateFeePlanUseCase createFeePlanUseCase,
        UpdateFeePlanUseCase updateFeePlanUseCase)
    {
        _getFeePlansUseCase = getFeePlansUseCase;
        _createFeePlanUseCase = createFeePlanUseCase;
        _updateFeePlanUseCase = updateFeePlanUseCase;
    }

    [HttpGet]
    public async Task<ActionResult<List<FeePlanDto>>> GetFeePlans(CancellationToken ct)
    {
        var response = await _getFeePlansUseCase.ExecuteAsync(new GetFeePlansRequest(), ct);
        return Ok(response);
    }

    public record CreateFeePlanBody(string Name, string? Description, decimal MonthlyAmount);

    [HttpPost]
    public async Task<ActionResult<FeePlanDto>> CreateFeePlan(
        [FromBody] CreateFeePlanBody body,
        CancellationToken ct)
    {
        var response = await _createFeePlanUseCase.ExecuteAsync(
            new CreateFeePlanRequest(body.Name, body.Description, body.MonthlyAmount), ct);
        return CreatedAtAction(nameof(GetFeePlans), new { id = response.Id }, response);
    }

    public record UpdateFeePlanBody(string Name, string? Description, decimal MonthlyAmount, bool IsActive);

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<FeePlanDto>> UpdateFeePlan(
        Guid id,
        [FromBody] UpdateFeePlanBody body,
        CancellationToken ct)
    {
        var response = await _updateFeePlanUseCase.ExecuteAsync(
            new UpdateFeePlanRequest(id, body.Name, body.Description, body.MonthlyAmount, body.IsActive), ct);
        return Ok(response);
    }
}


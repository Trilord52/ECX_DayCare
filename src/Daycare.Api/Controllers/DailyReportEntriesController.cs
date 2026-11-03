using Daycare.Application.Features.DailyReports.DTOs;
using Daycare.Application.Features.DailyReports.DeleteDailyReportEntry;
using Daycare.Application.Features.DailyReports.UpdateDailyReportEntry;
using Daycare.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Daycare.Api.Controllers;

[Route("api/v1/daily-report-entries")]
public class DailyReportEntriesController : BaseApiController
{
    private readonly UpdateDailyReportEntryUseCase _updateDailyReportEntryUseCase;
    private readonly DeleteDailyReportEntryUseCase _deleteDailyReportEntryUseCase;

    public DailyReportEntriesController(
        UpdateDailyReportEntryUseCase updateDailyReportEntryUseCase,
        DeleteDailyReportEntryUseCase deleteDailyReportEntryUseCase)
    {
        _updateDailyReportEntryUseCase = updateDailyReportEntryUseCase;
        _deleteDailyReportEntryUseCase = deleteDailyReportEntryUseCase;
    }

    public record UpdateDailyReportEntryBody(
        DateTime OccurredAt,
        DateTime? EndedAt = null,
        MealType? MealType = null,
        AmountEaten? AmountEaten = null,
        DiaperType? DiaperType = null,
        string? Description = null
    );

    [HttpPut("{entryId:guid}")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<ActionResult<DailyReportEntryDto>> UpdateEntry(
        Guid entryId,
        [FromBody] UpdateDailyReportEntryBody body,
        CancellationToken ct)
    {
        var response = await _updateDailyReportEntryUseCase.ExecuteAsync(
            new UpdateDailyReportEntryRequest(
                entryId,
                body.OccurredAt,
                body.EndedAt,
                body.MealType,
                body.AmountEaten,
                body.DiaperType,
                body.Description), ct);
        return Ok(response);
    }

    [HttpDelete("{entryId:guid}")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<ActionResult> DeleteEntry(
        Guid entryId,
        CancellationToken ct)
    {
        await _deleteDailyReportEntryUseCase.ExecuteAsync(
            new DeleteDailyReportEntryRequest(entryId), ct);
        return NoContent();
    }
}


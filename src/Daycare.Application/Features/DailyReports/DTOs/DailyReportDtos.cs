using Daycare.Domain.Enums;

namespace Daycare.Application.Features.DailyReports.DTOs;

public record DailyReportSummaryDto(
    DateOnly Date,
    Mood? Mood,
    int EntryCount
);

public record DailyReportEntryDto(
    Guid Id,
    Guid DailyReportId,
    EntryType EntryType,
    DateTime OccurredAt,
    DateTime? EndedAt,
    MealType? MealType,
    AmountEaten? AmountEaten,
    DiaperType? DiaperType,
    string? Description,
    Guid CreatedByUserId,
    string CreatedByName
);

public record DailyReportDetailDto(
    Guid? Id,
    Guid ChildId,
    Guid? ClassroomId,
    DateOnly Date,
    Mood? Mood,
    string? Notes,
    List<DailyReportEntryDto> Entries
);


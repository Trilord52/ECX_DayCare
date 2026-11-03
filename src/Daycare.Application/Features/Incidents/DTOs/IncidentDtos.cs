using Daycare.Domain.Enums;

namespace Daycare.Application.Features.Incidents.DTOs;

public record IncidentListItemDto(
    Guid Id,
    Guid ChildId,
    string ChildName,
    Guid ClassroomId,
    string ClassroomName,
    DateTime OccurredAt,
    IncidentType Type,
    IncidentSeverity Severity,
    string Description,
    string? ActionTaken,
    Guid ReportedByUserId,
    string ReportedByName,
    DateTime? ParentNotifiedAt,
    string? ParentNotifiedByName,
    DateTime? AcknowledgedAt,
    Guid? AcknowledgedByGuardianId,
    string? AcknowledgedByGuardianName,
    bool IsSerious
);

public record IncidentDetailDto(
    Guid Id,
    Guid ChildId,
    string ChildName,
    Guid ClassroomId,
    string ClassroomName,
    DateTime OccurredAt,
    IncidentType Type,
    IncidentSeverity Severity,
    string Description,
    string? ActionTaken,
    Guid ReportedByUserId,
    string ReportedByName,
    DateTime? ParentNotifiedAt,
    string? ParentNotifiedByName,
    DateTime? AcknowledgedAt,
    Guid? AcknowledgedByGuardianId,
    string? AcknowledgedByGuardianName,
    bool IsSerious
);


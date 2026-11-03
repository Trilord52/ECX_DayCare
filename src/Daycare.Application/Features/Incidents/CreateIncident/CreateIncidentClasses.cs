using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Incidents.DTOs;
using Daycare.Domain.Entities;
using Daycare.Domain.Enums;
using Daycare.Domain.Exceptions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Incidents.CreateIncident;

public record CreateIncidentRequest(
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

public class CreateIncidentValidator : AbstractValidator<CreateIncidentRequest>
{
    public CreateIncidentValidator()
    {
        RuleFor(x => x.ChildId).NotEmpty().WithMessage("Child ID is required.");
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000).WithMessage("Description is required and must not exceed 2000 characters.");
        RuleFor(x => x.ActionTaken).MaximumLength(2000).WithMessage("Action taken must not exceed 2000 characters.");
    }
}

public class CreateIncidentUseCase : UseCase<CreateIncidentRequest, IncidentDetailDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IAccessGuard _accessGuard;

    public CreateIncidentUseCase(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IAccessGuard accessGuard,
        IValidator<CreateIncidentRequest> validator)
        : base(validator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _accessGuard = accessGuard;
    }

    protected override async Task<IncidentDetailDto> HandleAsync(
        CreateIncidentRequest request,
        CancellationToken ct)
    {
        await _accessGuard.EnsureCanWriteChildDataAsync(request.ChildId, ct);

        var child = await _dbContext.Children
            .Include(c => c.Classroom)
            .FirstOrDefaultAsync(c => c.Id == request.ChildId, ct);

        if (child == null)
        {
            throw new NotFoundException("Child", request.ChildId);
        }

        var classroomId = request.ClassroomId ?? child.ClassroomId;
        if (!classroomId.HasValue)
        {
            throw new DomainRuleException("VALIDATION_FAILED", "Classroom is required for an incident.");
        }

        var classroom = await _dbContext.Classrooms
            .FirstOrDefaultAsync(c => c.Id == classroomId.Value, ct);

        if (classroom == null)
        {
            throw new NotFoundException("Classroom", classroomId.Value);
        }

        var userId = _currentUser.UserId ?? throw new UnauthorizedException("Authenticated user ID is required.");
        var userName = _currentUser.FullName ?? _currentUser.Email ?? "Staff";

        var occurredAtUtc = request.OccurredAt.Kind == DateTimeKind.Utc
            ? request.OccurredAt
            : DateTime.SpecifyKind(request.OccurredAt, DateTimeKind.Utc);

        var parentNotifiedAtUtc = request.ParentNotifiedAt.HasValue
            ? (request.ParentNotifiedAt.Value.Kind == DateTimeKind.Utc
                ? request.ParentNotifiedAt.Value
                : DateTime.SpecifyKind(request.ParentNotifiedAt.Value, DateTimeKind.Utc))
            : (DateTime?)null;

        var incident = new Incident(
            request.ChildId,
            classroomId.Value,
            occurredAtUtc,
            request.Type,
            request.Severity,
            request.Description,
            userId,
            userName,
            request.ActionTaken,
            parentNotifiedAtUtc,
            request.ParentNotifiedByName);

        _dbContext.Incidents.Add(incident);
        await _dbContext.SaveChangesAsync(ct);

        return new IncidentDetailDto(
            incident.Id,
            incident.ChildId,
            child.FirstName + " " + child.LastName,
            classroom.Id,
            classroom.Name,
            incident.OccurredAt,
            incident.Type,
            incident.Severity,
            incident.Description,
            incident.ActionTaken,
            incident.ReportedByUserId,
            incident.ReportedByName,
            incident.ParentNotifiedAt,
            incident.ParentNotifiedByName,
            incident.AcknowledgedAt,
            incident.AcknowledgedByGuardianId,
            null,
            incident.Severity == IncidentSeverity.Serious);
    }
}


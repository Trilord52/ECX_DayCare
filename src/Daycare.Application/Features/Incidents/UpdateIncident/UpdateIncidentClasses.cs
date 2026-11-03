using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Incidents.DTOs;
using Daycare.Domain.Enums;
using Daycare.Domain.Exceptions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Incidents.UpdateIncident;

public record UpdateIncidentRequest(
    Guid IncidentId,
    DateTime OccurredAt,
    IncidentType Type,
    IncidentSeverity Severity,
    string Description,
    string? ActionTaken = null,
    DateTime? ParentNotifiedAt = null,
    string? ParentNotifiedByName = null
);

public class UpdateIncidentValidator : AbstractValidator<UpdateIncidentRequest>
{
    public UpdateIncidentValidator()
    {
        RuleFor(x => x.IncidentId).NotEmpty().WithMessage("Incident ID is required.");
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000).WithMessage("Description is required and must not exceed 2000 characters.");
        RuleFor(x => x.ActionTaken).MaximumLength(2000).WithMessage("Action taken must not exceed 2000 characters.");
    }
}

public class UpdateIncidentUseCase : UseCase<UpdateIncidentRequest, IncidentDetailDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAccessGuard _accessGuard;

    public UpdateIncidentUseCase(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        IAccessGuard accessGuard,
        IValidator<UpdateIncidentRequest> validator)
        : base(validator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _accessGuard = accessGuard;
    }

    protected override async Task<IncidentDetailDto> HandleAsync(
        UpdateIncidentRequest request,
        CancellationToken ct)
    {
        var incident = await _dbContext.Incidents
            .Include(i => i.Child)
            .Include(i => i.Classroom)
            .Include(i => i.AcknowledgedByGuardian)
            .FirstOrDefaultAsync(i => i.Id == request.IncidentId, ct);

        if (incident == null)
        {
            throw new NotFoundException("Incident", request.IncidentId);
        }

        await _accessGuard.EnsureCanWriteChildDataAsync(incident.ChildId, ct);

        // BR-16: Staff same local day only; Admin any time
        if (_currentUser.Role == "Staff")
        {
            var incidentLocalDate = DateOnly.FromDateTime(_dateTimeProvider.ToLocal(incident.OccurredAt));
            if (incidentLocalDate != _dateTimeProvider.LocalToday)
            {
                throw new DomainRuleException("ENTRY_LOCKED", "Staff may only edit incidents on the same local day.");
            }
        }

        var occurredAtUtc = request.OccurredAt.Kind == DateTimeKind.Utc
            ? request.OccurredAt
            : DateTime.SpecifyKind(request.OccurredAt, DateTimeKind.Utc);

        var parentNotifiedAtUtc = request.ParentNotifiedAt.HasValue
            ? (request.ParentNotifiedAt.Value.Kind == DateTimeKind.Utc
                ? request.ParentNotifiedAt.Value
                : DateTime.SpecifyKind(request.ParentNotifiedAt.Value, DateTimeKind.Utc))
            : (DateTime?)null;

        incident.Update(
            occurredAtUtc,
            request.Type,
            request.Severity,
            request.Description,
            request.ActionTaken,
            parentNotifiedAtUtc,
            request.ParentNotifiedByName);

        await _dbContext.SaveChangesAsync(ct);

        return new IncidentDetailDto(
            incident.Id,
            incident.ChildId,
            incident.Child.FirstName + " " + incident.Child.LastName,
            incident.ClassroomId,
            incident.Classroom.Name,
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
            incident.AcknowledgedByGuardian != null
                ? incident.AcknowledgedByGuardian.FirstName + " " + incident.AcknowledgedByGuardian.LastName
                : null,
            incident.Severity == IncidentSeverity.Serious);
    }
}


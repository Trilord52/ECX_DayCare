using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Incidents.DTOs;
using Daycare.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Incidents.NotifyParentIncident;

public record NotifyParentIncidentRequest(Guid IncidentId);

public class NotifyParentIncidentValidator : AbstractValidator<NotifyParentIncidentRequest>
{
    public NotifyParentIncidentValidator()
    {
        RuleFor(x => x.IncidentId).NotEmpty().WithMessage("Incident ID is required.");
    }
}

public class NotifyParentIncidentUseCase : UseCase<NotifyParentIncidentRequest, IncidentDetailDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAccessGuard _accessGuard;

    public NotifyParentIncidentUseCase(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        IAccessGuard accessGuard,
        IValidator<NotifyParentIncidentRequest> validator)
        : base(validator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _accessGuard = accessGuard;
    }

    protected override async Task<IncidentDetailDto> HandleAsync(
        NotifyParentIncidentRequest request,
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

        var notifierName = _currentUser.FullName ?? _currentUser.Email ?? "Staff";
        incident.NotifyParent(_dateTimeProvider.UtcNow, notifierName);

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


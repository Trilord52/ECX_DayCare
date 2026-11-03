using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Incidents.DTOs;
using Daycare.Domain.Enums;
using Daycare.Domain.Exceptions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Incidents.AcknowledgeIncident;

public record AcknowledgeIncidentRequest(Guid IncidentId);

public class AcknowledgeIncidentValidator : AbstractValidator<AcknowledgeIncidentRequest>
{
    public AcknowledgeIncidentValidator()
    {
        RuleFor(x => x.IncidentId).NotEmpty().WithMessage("Incident ID is required.");
    }
}

public class AcknowledgeIncidentUseCase : UseCase<AcknowledgeIncidentRequest, IncidentDetailDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AcknowledgeIncidentUseCase(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        IValidator<AcknowledgeIncidentRequest> validator)
        : base(validator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    protected override async Task<IncidentDetailDto> HandleAsync(
        AcknowledgeIncidentRequest request,
        CancellationToken ct)
    {
        var incident = await _dbContext.Incidents
            .Include(i => i.Child)
            .Include(i => i.Classroom)
            .FirstOrDefaultAsync(i => i.Id == request.IncidentId, ct);

        if (incident == null)
        {
            throw new NotFoundException("Incident", request.IncidentId);
        }

        var guardianId = _currentUser.GuardianId;
        if (!guardianId.HasValue && _currentUser.UserId.HasValue)
        {
            guardianId = await _dbContext.Guardians
                .AsNoTracking()
                .Where(g => g.UserId == _currentUser.UserId.Value)
                .Select(g => (Guid?)g.Id)
                .FirstOrDefaultAsync(ct);
        }

        if (!guardianId.HasValue)
        {
            throw new ForbiddenException("No guardian record associated with current user.");
        }

        // BR-16: Only a Parent linked to the child can acknowledge
        var isLinked = await _dbContext.ChildGuardians
            .AsNoTracking()
            .AnyAsync(cg => cg.ChildId == incident.ChildId && cg.GuardianId == guardianId.Value, ct);

        if (!isLinked)
        {
            throw new ForbiddenException("Only a guardian linked to the child can acknowledge this incident.");
        }

        // BR-16: Acknowledge once
        if (incident.AcknowledgedAt.HasValue)
        {
            throw new DomainRuleException("ALREADY_ACKNOWLEDGED", "Incident has already been acknowledged.");
        }

        var ackTime = _dateTimeProvider.UtcNow;
        incident.Acknowledge(guardianId.Value, ackTime);

        await _dbContext.SaveChangesAsync(ct);

        var guardian = await _dbContext.Guardians
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == guardianId.Value, ct);

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
            guardian != null ? guardian.FirstName + " " + guardian.LastName : null,
            incident.Severity == IncidentSeverity.Serious);
    }
}


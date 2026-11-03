using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Incidents.DTOs;
using Daycare.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Incidents.GetIncidentById;

public record GetIncidentByIdRequest(Guid Id);

public class GetIncidentByIdValidator : AbstractValidator<GetIncidentByIdRequest>
{
    public GetIncidentByIdValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Incident ID is required.");
    }
}

public class GetIncidentByIdUseCase : UseCase<GetIncidentByIdRequest, IncidentDetailDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAccessGuard _accessGuard;

    public GetIncidentByIdUseCase(
        IApplicationDbContext dbContext,
        IAccessGuard accessGuard,
        IValidator<GetIncidentByIdRequest> validator)
        : base(validator)
    {
        _dbContext = dbContext;
        _accessGuard = accessGuard;
    }

    protected override async Task<IncidentDetailDto> HandleAsync(
        GetIncidentByIdRequest request,
        CancellationToken ct)
    {
        var incident = await _dbContext.Incidents
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(i => i.Child)
            .Include(i => i.Classroom)
            .Include(i => i.AcknowledgedByGuardian)
            .FirstOrDefaultAsync(i => i.Id == request.Id, ct);

        if (incident == null)
        {
            throw new NotFoundException("Incident", request.Id);
        }

        await _accessGuard.EnsureCanViewChildAsync(incident.ChildId, ct);

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


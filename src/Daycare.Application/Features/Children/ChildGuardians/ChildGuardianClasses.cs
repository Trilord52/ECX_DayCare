using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Children.DTOs;
using Daycare.Domain.Entities;
using Daycare.Domain.Enums;
using Daycare.Domain.Exceptions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Children.ChildGuardians;

public record GetChildGuardiansRequest(Guid ChildId);

public class GetChildGuardiansUseCase : UseCase<GetChildGuardiansRequest, List<ChildGuardianDto>>
{
    private readonly IAccessGuard _accessGuard;
    private readonly IApplicationDbContext _dbContext;

    public GetChildGuardiansUseCase(IAccessGuard accessGuard, IApplicationDbContext dbContext)
    {
        _accessGuard = accessGuard;
        _dbContext = dbContext;
    }

    protected override async Task<List<ChildGuardianDto>> HandleAsync(
        GetChildGuardiansRequest request,
        CancellationToken ct)
    {
        await _accessGuard.EnsureCanViewChildAsync(request.ChildId, ct);

        var guardians = await _dbContext.ChildGuardians
            .AsNoTracking()
            .Where(cg => cg.ChildId == request.ChildId)
            .Include(cg => cg.Guardian)
            .Select(cg => new ChildGuardianDto(
                cg.GuardianId,
                cg.Guardian.FirstName + " " + cg.Guardian.LastName,
                cg.Relationship,
                cg.Guardian.Phone,
                cg.Guardian.Email,
                cg.IsPrimaryContact,
                cg.CanPickUp,
                cg.IsPayer,
                cg.IsEmergencyContact,
                cg.Guardian.Photo != null,
                cg.Guardian.Photo != null ? cg.Guardian.Photo.UpdatedAt : null
            ))
            .ToListAsync(ct);

        return guardians;
    }
}

public record LinkGuardianRequest(
    Guid ChildId,
    Guid GuardianId,
    Relationship Relationship,
    bool IsPrimaryContact = false,
    bool CanPickUp = true,
    bool IsPayer = false,
    bool IsEmergencyContact = false
);

public class LinkGuardianValidator : AbstractValidator<LinkGuardianRequest>
{
    public LinkGuardianValidator()
    {
        RuleFor(x => x.ChildId).NotEmpty();
        RuleFor(x => x.GuardianId).NotEmpty();
    }
}

public class LinkGuardianUseCase : UseCase<LinkGuardianRequest, ChildGuardianDto>
{
    private readonly IApplicationDbContext _dbContext;

    public LinkGuardianUseCase(IApplicationDbContext dbContext, IValidator<LinkGuardianRequest> validator)
        : base(validator)
    {
        _dbContext = dbContext;
    }

    protected override async Task<ChildGuardianDto> HandleAsync(LinkGuardianRequest request, CancellationToken ct)
    {
        var child = await _dbContext.Children
            .Include(c => c.Guardians)
            .FirstOrDefaultAsync(c => c.Id == request.ChildId, ct);

        if (child == null) throw new NotFoundException("Child", request.ChildId);

        var guardian = await _dbContext.Guardians
            .FirstOrDefaultAsync(g => g.Id == request.GuardianId, ct);

        if (guardian == null) throw new NotFoundException("Guardian", request.GuardianId);

        var existingLink = child.Guardians.FirstOrDefault(cg => cg.GuardianId == request.GuardianId);
        if (existingLink != null)
        {
            throw new ConflictException("Guardian is already linked to this child.", "DUPLICATE_LINK");
        }

        // BR-06: At most one primary contact per child
        if (request.IsPrimaryContact)
        {
            foreach (var link in child.Guardians.Where(cg => cg.IsPrimaryContact))
            {
                link.Update(link.Relationship, false, link.CanPickUp, link.IsPayer, link.IsEmergencyContact);
            }
        }

        var newLink = new ChildGuardian(
            request.ChildId,
            request.GuardianId,
            request.Relationship,
            request.IsPrimaryContact,
            request.CanPickUp,
            request.IsPayer,
            request.IsEmergencyContact);

        _dbContext.ChildGuardians.Add(newLink);
        await _dbContext.SaveChangesAsync(ct);

        return new ChildGuardianDto(
            guardian.Id,
            guardian.FirstName + " " + guardian.LastName,
            newLink.Relationship,
            guardian.Phone,
            guardian.Email,
            newLink.IsPrimaryContact,
            newLink.CanPickUp,
            newLink.IsPayer,
            newLink.IsEmergencyContact,
            guardian.Photo != null,
            guardian.Photo?.UpdatedAt);
    }
}

public record UpdateChildGuardianRequest(
    Guid ChildId,
    Guid GuardianId,
    Relationship Relationship,
    bool IsPrimaryContact,
    bool CanPickUp,
    bool IsPayer,
    bool IsEmergencyContact
);

public class UpdateChildGuardianValidator : AbstractValidator<UpdateChildGuardianRequest>
{
    public UpdateChildGuardianValidator()
    {
        RuleFor(x => x.ChildId).NotEmpty();
        RuleFor(x => x.GuardianId).NotEmpty();
    }
}

public class UpdateChildGuardianUseCase : UseCase<UpdateChildGuardianRequest, ChildGuardianDto>
{
    private readonly IApplicationDbContext _dbContext;

    public UpdateChildGuardianUseCase(
        IApplicationDbContext dbContext,
        IValidator<UpdateChildGuardianRequest> validator)
        : base(validator)
    {
        _dbContext = dbContext;
    }

    protected override async Task<ChildGuardianDto> HandleAsync(UpdateChildGuardianRequest request, CancellationToken ct)
    {
        var child = await _dbContext.Children
            .Include(c => c.Guardians)
                .ThenInclude(cg => cg.Guardian)
            .FirstOrDefaultAsync(c => c.Id == request.ChildId, ct);

        if (child == null) throw new NotFoundException("Child", request.ChildId);

        var targetLink = child.Guardians.FirstOrDefault(cg => cg.GuardianId == request.GuardianId);
        if (targetLink == null) throw new NotFoundException("Guardian link not found for this child.");

        // BR-07: Removing pickup capability when it's the last pickup guardian
        if (!request.CanPickUp && targetLink.CanPickUp)
        {
            var otherPickupCount = child.Guardians.Count(cg => cg.GuardianId != request.GuardianId && cg.CanPickUp);
            if (otherPickupCount == 0)
            {
                throw new DomainRuleException(
                    "NO_PICKUP_GUARDIAN",
                    "A child must have at least one authorized pickup contact.");
            }
        }

        // BR-06: At most one primary contact per child
        if (request.IsPrimaryContact)
        {
            foreach (var link in child.Guardians.Where(cg => cg.GuardianId != request.GuardianId && cg.IsPrimaryContact))
            {
                link.Update(link.Relationship, false, link.CanPickUp, link.IsPayer, link.IsEmergencyContact);
            }
        }

        targetLink.Update(
            request.Relationship,
            request.IsPrimaryContact,
            request.CanPickUp,
            request.IsPayer,
            request.IsEmergencyContact);

        await _dbContext.SaveChangesAsync(ct);

        return new ChildGuardianDto(
            targetLink.Guardian.Id,
            targetLink.Guardian.FirstName + " " + targetLink.Guardian.LastName,
            targetLink.Relationship,
            targetLink.Guardian.Phone,
            targetLink.Guardian.Email,
            targetLink.IsPrimaryContact,
            targetLink.CanPickUp,
            targetLink.IsPayer,
            targetLink.IsEmergencyContact,
            targetLink.Guardian.Photo != null,
            targetLink.Guardian.Photo?.UpdatedAt);
    }
}

public record UnlinkGuardianRequest(Guid ChildId, Guid GuardianId);

public record UnlinkGuardianResponse(bool Success = true);

public class UnlinkGuardianUseCase : UseCase<UnlinkGuardianRequest, UnlinkGuardianResponse>
{
    private readonly IApplicationDbContext _dbContext;

    public UnlinkGuardianUseCase(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    protected override async Task<UnlinkGuardianResponse> HandleAsync(
        UnlinkGuardianRequest request,
        CancellationToken ct)
    {
        var child = await _dbContext.Children
            .Include(c => c.Guardians)
            .FirstOrDefaultAsync(c => c.Id == request.ChildId, ct);

        if (child == null) throw new NotFoundException("Child", request.ChildId);

        var targetLink = child.Guardians.FirstOrDefault(cg => cg.GuardianId == request.GuardianId);
        if (targetLink == null) throw new NotFoundException("Guardian link not found for this child.");

        // BR-07: Removing the link that makes a child's last can_pick_up guardian is rejected with NO_PICKUP_GUARDIAN
        if (targetLink.CanPickUp)
        {
            var otherPickupCount = child.Guardians.Count(cg => cg.GuardianId != request.GuardianId && cg.CanPickUp);
            if (otherPickupCount == 0)
            {
                throw new DomainRuleException(
                    "NO_PICKUP_GUARDIAN",
                    "Cannot remove the only authorized pickup contact for this child.");
            }
        }

        _dbContext.ChildGuardians.Remove(targetLink);
        await _dbContext.SaveChangesAsync(ct);

        return new UnlinkGuardianResponse(true);
    }
}

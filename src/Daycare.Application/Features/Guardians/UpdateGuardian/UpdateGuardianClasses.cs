using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Guardians.DTOs;
using Daycare.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Guardians.UpdateGuardian;

public record UpdateGuardianRequest(
    Guid Id,
    string FirstName,
    string LastName,
    string Phone,
    string? Email = null,
    string? Address = null
);

public class UpdateGuardianValidator : AbstractValidator<UpdateGuardianRequest>
{
    public UpdateGuardianValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Guardian ID is required.");
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(50);
        When(x => !string.IsNullOrEmpty(x.Email), () =>
        {
            RuleFor(x => x.Email).EmailAddress().MaximumLength(256);
        });
        RuleFor(x => x.Address).MaximumLength(300);
    }
}

public class UpdateGuardianUseCase : UseCase<UpdateGuardianRequest, GuardianDto>
{
    private readonly IApplicationDbContext _dbContext;

    public UpdateGuardianUseCase(IApplicationDbContext dbContext, IValidator<UpdateGuardianRequest> validator)
        : base(validator)
    {
        _dbContext = dbContext;
    }

    protected override async Task<GuardianDto> HandleAsync(UpdateGuardianRequest request, CancellationToken ct)
    {
        var guardian = await _dbContext.Guardians
            .Include(g => g.Children)
                .ThenInclude(cg => cg.Child)
            .FirstOrDefaultAsync(g => g.Id == request.Id, ct);

        if (guardian == null)
        {
            throw new NotFoundException("Guardian", request.Id);
        }

        guardian.Update(
            request.FirstName,
            request.LastName,
            request.Phone,
            request.Email,
            request.Address);

        await _dbContext.SaveChangesAsync(ct);

        return new GuardianDto(
            guardian.Id,
            guardian.UserId,
            guardian.FirstName,
            guardian.LastName,
            guardian.FirstName + " " + guardian.LastName,
            guardian.Phone,
            guardian.Email,
            guardian.Address,
            guardian.Children.Select(cg => new ChildBriefDto(
                cg.Child.Id,
                cg.Child.FirstName + " " + cg.Child.LastName,
                cg.Child.Status.ToString()
            )).ToList());
    }
}

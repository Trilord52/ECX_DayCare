using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Children.DTOs;
using Daycare.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Children.UpdateChild;

public record UpdateChildRequest(
    Guid Id,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    Gender Gender,
    DateOnly EnrollmentDate,
    string? Allergies = null,
    string? MedicalNotes = null,
    string? DietaryNotes = null
);

public class UpdateChildValidator : AbstractValidator<UpdateChildRequest>
{
    public UpdateChildValidator(IDateTimeProvider dateTimeProvider)
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Child ID is required.");
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DateOfBirth)
            .NotEmpty()
            .LessThanOrEqualTo(_ => dateTimeProvider.LocalToday)
            .WithMessage("Date of birth cannot be in the future.");
        RuleFor(x => x.Allergies).MaximumLength(1000);
        RuleFor(x => x.MedicalNotes).MaximumLength(2000);
        RuleFor(x => x.DietaryNotes).MaximumLength(1000);
    }
}

public class UpdateChildUseCase : UseCase<UpdateChildRequest, ChildListItemDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateChildUseCase(
        IApplicationDbContext dbContext,
        IDateTimeProvider dateTimeProvider,
        IValidator<UpdateChildRequest> validator)
        : base(validator)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    protected override async Task<ChildListItemDto> HandleAsync(UpdateChildRequest request, CancellationToken ct)
    {
        var child = await _dbContext.Children
            .Include(c => c.Classroom)
            .FirstOrDefaultAsync(c => c.Id == request.Id, ct);

        if (child == null)
        {
            throw new NotFoundException("Child", request.Id);
        }

        child.Update(
            request.FirstName,
            request.LastName,
            request.DateOfBirth,
            request.Gender,
            request.EnrollmentDate,
            request.Allergies,
            request.MedicalNotes,
            request.DietaryNotes);

        await _dbContext.SaveChangesAsync(ct);

        var today = _dateTimeProvider.LocalToday;

        return new ChildListItemDto(
            child.Id,
            child.FirstName,
            child.LastName,
            child.FirstName + " " + child.LastName,
            child.DateOfBirth,
            child.CalculateAgeInMonths(today),
            child.Gender,
            child.ClassroomId,
            child.Classroom?.Name,
            child.Status,
            child.EnrollmentDate,
            child.Allergies);
    }
}

using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Children.DTOs;
using Daycare.Domain.Entities;
using Daycare.Domain.Enums;
using Daycare.Domain.Exceptions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Children.CreateChild;

public record CreateChildRequest(
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    Gender Gender,
    DateOnly EnrollmentDate,
    Guid? ClassroomId = null,
    Guid? FeePlanId = null,
    string? Allergies = null,
    string? MedicalNotes = null,
    string? DietaryNotes = null
);

public class CreateChildValidator : AbstractValidator<CreateChildRequest>
{
    public CreateChildValidator(IDateTimeProvider dateTimeProvider)
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters.");

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Date of birth is required.")
            .LessThanOrEqualTo(_ => dateTimeProvider.LocalToday)
            .WithMessage("Date of birth cannot be in the future.");

        RuleFor(x => x.Allergies)
            .MaximumLength(1000).WithMessage("Allergies notes must not exceed 1000 characters.");

        RuleFor(x => x.MedicalNotes)
            .MaximumLength(2000).WithMessage("Medical notes must not exceed 2000 characters.");

        RuleFor(x => x.DietaryNotes)
            .MaximumLength(1000).WithMessage("Dietary notes must not exceed 1000 characters.");
    }
}

public class CreateChildUseCase : UseCase<CreateChildRequest, ChildListItemDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateChildUseCase(
        IApplicationDbContext dbContext,
        IDateTimeProvider dateTimeProvider,
        IValidator<CreateChildRequest> validator)
        : base(validator)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    protected override async Task<ChildListItemDto> HandleAsync(CreateChildRequest request, CancellationToken ct)
    {
        string? classroomName = null;

        // BR-05: Check classroom active and capacity
        if (request.ClassroomId.HasValue)
        {
            var classroom = await _dbContext.Classrooms
                .FirstOrDefaultAsync(c => c.Id == request.ClassroomId.Value, ct);

            if (classroom == null)
            {
                throw new NotFoundException("Classroom", request.ClassroomId.Value);
            }

            if (!classroom.IsActive)
            {
                throw new DomainRuleException("CLASSROOM_INACTIVE", "Cannot assign child to an inactive classroom.");
            }

            var activeCount = await _dbContext.Children
                .CountAsync(c => c.ClassroomId == classroom.Id && c.Status == ChildStatus.Active, ct);

            if (activeCount >= classroom.Capacity)
            {
                throw new DomainRuleException("CLASSROOM_FULL", "Classroom is already at maximum capacity.");
            }

            classroomName = classroom.Name;
        }

        if (request.FeePlanId.HasValue)
        {
            var feePlanExists = await _dbContext.FeePlans
                .AnyAsync(f => f.Id == request.FeePlanId.Value, ct);

            if (!feePlanExists)
            {
                throw new NotFoundException("FeePlan", request.FeePlanId.Value);
            }
        }

        var child = new Child(
            request.FirstName,
            request.LastName,
            request.DateOfBirth,
            request.Gender,
            request.EnrollmentDate,
            request.ClassroomId,
            request.FeePlanId,
            request.Allergies,
            request.MedicalNotes,
            request.DietaryNotes);

        _dbContext.Children.Add(child);
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
            classroomName,
            child.Status,
            child.EnrollmentDate,
            child.Allergies);
    }
}

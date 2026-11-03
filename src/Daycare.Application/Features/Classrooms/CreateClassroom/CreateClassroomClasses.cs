using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Classrooms.DTOs;
using Daycare.Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Classrooms.CreateClassroom;

public record CreateClassroomRequest(
    string Name,
    string? Description,
    int MinAgeMonths,
    int MaxAgeMonths,
    int Capacity,
    int MaxChildrenPerStaff
);

public class CreateClassroomValidator : AbstractValidator<CreateClassroomRequest>
{
    public CreateClassroomValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Classroom name is required.")
            .MaximumLength(100).WithMessage("Classroom name must not exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");

        RuleFor(x => x.MinAgeMonths)
            .GreaterThanOrEqualTo(0).WithMessage("Minimum age must be greater than or equal to 0.");

        RuleFor(x => x.MaxAgeMonths)
            .GreaterThanOrEqualTo(x => x.MinAgeMonths)
            .WithMessage("Maximum age must be greater than or equal to minimum age.");

        RuleFor(x => x.Capacity)
            .GreaterThan(0).WithMessage("Capacity must be greater than 0.");

        RuleFor(x => x.MaxChildrenPerStaff)
            .GreaterThan(0).WithMessage("Max children per staff must be greater than 0.");
    }
}

public class CreateClassroomUseCase : UseCase<CreateClassroomRequest, ClassroomDto>
{
    private readonly IApplicationDbContext _dbContext;

    public CreateClassroomUseCase(IApplicationDbContext dbContext, IValidator<CreateClassroomRequest> validator)
        : base(validator)
    {
        _dbContext = dbContext;
    }

    protected override async Task<ClassroomDto> HandleAsync(CreateClassroomRequest request, CancellationToken ct)
    {
        var trimmedName = request.Name.Trim();
        var exists = await _dbContext.Classrooms
            .AnyAsync(c => c.Name.ToLower() == trimmedName.ToLower(), ct);

        if (exists)
        {
            throw new ConflictException($"Classroom with name '{trimmedName}' already exists.", "DUPLICATE_NAME");
        }

        var classroom = new Classroom(
            trimmedName,
            request.Description?.Trim(),
            request.MinAgeMonths,
            request.MaxAgeMonths,
            request.Capacity,
            request.MaxChildrenPerStaff);

        _dbContext.Classrooms.Add(classroom);
        await _dbContext.SaveChangesAsync(ct);

        return new ClassroomDto(
            classroom.Id,
            classroom.Name,
            classroom.Description,
            classroom.MinAgeMonths,
            classroom.MaxAgeMonths,
            classroom.Capacity,
            classroom.MaxChildrenPerStaff,
            classroom.IsActive,
            0,
            0);
    }
}

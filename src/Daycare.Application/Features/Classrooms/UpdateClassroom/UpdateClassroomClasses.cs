using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Classrooms.DTOs;
using Daycare.Domain.Enums;
using Daycare.Domain.Exceptions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Classrooms.UpdateClassroom;

public record UpdateClassroomRequest(
    Guid Id,
    string Name,
    string? Description,
    int MinAgeMonths,
    int MaxAgeMonths,
    int Capacity,
    int MaxChildrenPerStaff,
    bool IsActive
);

public class UpdateClassroomValidator : AbstractValidator<UpdateClassroomRequest>
{
    public UpdateClassroomValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Classroom ID is required.");

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

public class UpdateClassroomUseCase : UseCase<UpdateClassroomRequest, ClassroomDto>
{
    private readonly IApplicationDbContext _dbContext;

    public UpdateClassroomUseCase(IApplicationDbContext dbContext, IValidator<UpdateClassroomRequest> validator)
        : base(validator)
    {
        _dbContext = dbContext;
    }

    protected override async Task<ClassroomDto> HandleAsync(UpdateClassroomRequest request, CancellationToken ct)
    {
        var classroom = await _dbContext.Classrooms
            .FirstOrDefaultAsync(c => c.Id == request.Id, ct);

        if (classroom == null)
        {
            throw new NotFoundException("Classroom", request.Id);
        }

        var trimmedName = request.Name.Trim();
        var exists = await _dbContext.Classrooms
            .AnyAsync(c => c.Id != request.Id && c.Name.ToLower() == trimmedName.ToLower(), ct);

        if (exists)
        {
            throw new ConflictException($"Classroom with name '{trimmedName}' already exists.", "DUPLICATE_NAME");
        }

        if (!request.IsActive && classroom.IsActive)
        {
            var activeChildrenCount = await _dbContext.Children
                .CountAsync(c => c.ClassroomId == request.Id && c.Status == ChildStatus.Active, ct);

            if (activeChildrenCount > 0)
            {
                throw new DomainRuleException(
                    "CLASSROOM_HAS_ACTIVE_CHILDREN",
                    "Cannot deactivate classroom while active children are enrolled.");
            }
        }

        classroom.Update(
            trimmedName,
            request.Description?.Trim(),
            request.MinAgeMonths,
            request.MaxAgeMonths,
            request.Capacity,
            request.MaxChildrenPerStaff,
            request.IsActive);

        await _dbContext.SaveChangesAsync(ct);

        var enrolledCount = await _dbContext.Children
            .CountAsync(c => c.ClassroomId == request.Id && c.Status == ChildStatus.Active, ct);
        var staffCount = await _dbContext.StaffClassrooms
            .CountAsync(sc => sc.ClassroomId == request.Id, ct);

        return new ClassroomDto(
            classroom.Id,
            classroom.Name,
            classroom.Description,
            classroom.MinAgeMonths,
            classroom.MaxAgeMonths,
            classroom.Capacity,
            classroom.MaxChildrenPerStaff,
            classroom.IsActive,
            enrolledCount,
            staffCount);
    }
}

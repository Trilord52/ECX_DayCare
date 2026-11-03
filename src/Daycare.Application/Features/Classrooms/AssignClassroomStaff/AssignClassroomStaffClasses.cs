using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Classrooms.AssignClassroomStaff;

public record AssignClassroomStaffRequest(Guid ClassroomId, List<Guid> StaffIds);

public record AssignClassroomStaffResponse(bool Success = true);

public class AssignClassroomStaffValidator : AbstractValidator<AssignClassroomStaffRequest>
{
    public AssignClassroomStaffValidator()
    {
        RuleFor(x => x.ClassroomId)
            .NotEmpty().WithMessage("Classroom ID is required.");
        RuleFor(x => x.StaffIds)
            .NotNull().WithMessage("Staff IDs list cannot be null.");
    }
}

public class AssignClassroomStaffUseCase : UseCase<AssignClassroomStaffRequest, AssignClassroomStaffResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AssignClassroomStaffUseCase(
        IApplicationDbContext dbContext,
        IDateTimeProvider dateTimeProvider,
        IValidator<AssignClassroomStaffRequest> validator)
        : base(validator)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    protected override async Task<AssignClassroomStaffResponse> HandleAsync(
        AssignClassroomStaffRequest request,
        CancellationToken ct)
    {
        var classroom = await _dbContext.Classrooms
            .FirstOrDefaultAsync(c => c.Id == request.ClassroomId, ct);

        if (classroom == null)
        {
            throw new NotFoundException("Classroom", request.ClassroomId);
        }

        // Validate that all staff IDs exist
        var existingStaffIds = await _dbContext.Staff
            .Where(s => request.StaffIds.Contains(s.Id))
            .Select(s => s.Id)
            .ToListAsync(ct);

        var missingStaffIds = request.StaffIds.Except(existingStaffIds).ToList();
        if (missingStaffIds.Any())
        {
            throw new NotFoundException($"Staff members not found: {string.Join(", ", missingStaffIds)}");
        }

        // Remove existing assignments
        var existingAssignments = await _dbContext.StaffClassrooms
            .Where(sc => sc.ClassroomId == request.ClassroomId)
            .ToListAsync(ct);

        _dbContext.StaffClassrooms.RemoveRange(existingAssignments);

        // Add new assignments
        foreach (var staffId in request.StaffIds.Distinct())
        {
            _dbContext.StaffClassrooms.Add(new StaffClassroom(staffId, request.ClassroomId, _dateTimeProvider.UtcNow));
        }

        await _dbContext.SaveChangesAsync(ct);

        return new AssignClassroomStaffResponse(true);
    }
}

using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Staff.DTOs;
using Daycare.Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Staff.UpdateStaff;

public record UpdateStaffRequest(
    Guid Id,
    string FirstName,
    string LastName,
    string Phone,
    string JobTitle,
    DateOnly HireDate,
    List<Guid>? ClassroomIds = null
);

public class UpdateStaffValidator : AbstractValidator<UpdateStaffRequest>
{
    public UpdateStaffValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Staff ID is required.");
        RuleFor(x => x.FirstName).NotEmpty().WithMessage("First name is required.").MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().WithMessage("Last name is required.").MaximumLength(100);
        RuleFor(x => x.Phone).NotEmpty().WithMessage("Phone is required.").MaximumLength(50);
        RuleFor(x => x.JobTitle).NotEmpty().WithMessage("Job title is required.").MaximumLength(100);
    }
}

public class UpdateStaffUseCase : UseCase<UpdateStaffRequest, StaffDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateStaffUseCase(
        IApplicationDbContext dbContext,
        IDateTimeProvider dateTimeProvider,
        IValidator<UpdateStaffRequest> validator)
        : base(validator)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    protected override async Task<StaffDto> HandleAsync(UpdateStaffRequest request, CancellationToken ct)
    {
        var staff = await _dbContext.Staff
            .Include(s => s.StaffClassrooms)
            .FirstOrDefaultAsync(s => s.Id == request.Id, ct);

        if (staff == null)
        {
            throw new NotFoundException("Staff member", request.Id);
        }

        staff.Update(request.FirstName, request.LastName, request.Phone, staff.Email, request.JobTitle, request.HireDate, staff.IsActive);

        if (request.ClassroomIds != null)
        {
            var existingAssignments = await _dbContext.StaffClassrooms
                .Where(sc => sc.StaffId == request.Id)
                .ToListAsync(ct);
            _dbContext.StaffClassrooms.RemoveRange(existingAssignments);

            foreach (var classroomId in request.ClassroomIds.Distinct())
            {
                _dbContext.StaffClassrooms.Add(new StaffClassroom(staff.Id, classroomId, _dateTimeProvider.UtcNow));
            }
        }

        await _dbContext.SaveChangesAsync(ct);

        var assignedClassrooms = await _dbContext.StaffClassrooms
            .AsNoTracking()
            .Where(sc => sc.StaffId == staff.Id)
            .Include(sc => sc.Classroom)
            .Select(sc => new ClassroomBriefDto(sc.ClassroomId, sc.Classroom.Name))
            .ToListAsync(ct);

        return new StaffDto(
            staff.Id,
            staff.UserId,
            staff.FirstName,
            staff.LastName,
            staff.FirstName + " " + staff.LastName,
            staff.Email,
            staff.Phone,
            staff.JobTitle,
            staff.HireDate,
            staff.IsActive,
            assignedClassrooms);
    }
}

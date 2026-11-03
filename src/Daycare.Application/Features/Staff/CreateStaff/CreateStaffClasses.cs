using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Staff.DTOs;
using Daycare.Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Staff.CreateStaff;

public record CreateStaffRequest(
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    string JobTitle,
    DateOnly HireDate,
    List<Guid>? ClassroomIds = null
);

public record CreateStaffResponse(
    StaffDto Staff,
    string TemporaryPassword
);

public class CreateStaffValidator : AbstractValidator<CreateStaffRequest>
{
    public CreateStaffValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(256).WithMessage("Email must not exceed 256 characters.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone is required.")
            .MaximumLength(50).WithMessage("Phone must not exceed 50 characters.");

        RuleFor(x => x.JobTitle)
            .NotEmpty().WithMessage("Job title is required.")
            .MaximumLength(100).WithMessage("Job title must not exceed 100 characters.");
    }
}

public class CreateStaffUseCase : UseCase<CreateStaffRequest, CreateStaffResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IIdentityService _identityService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateStaffUseCase(
        IApplicationDbContext dbContext,
        IIdentityService identityService,
        IDateTimeProvider dateTimeProvider,
        IValidator<CreateStaffRequest> validator)
        : base(validator)
    {
        _dbContext = dbContext;
        _identityService = identityService;
        _dateTimeProvider = dateTimeProvider;
    }

    protected override async Task<CreateStaffResponse> HandleAsync(CreateStaffRequest request, CancellationToken ct)
    {
        var trimmedEmail = request.Email.Trim().ToLowerInvariant();

        // BR-02: Check email uniqueness
        var existingStaff = await _dbContext.Staff.AnyAsync(s => s.Email.ToLower() == trimmedEmail, ct);
        if (existingStaff)
        {
            throw new ConflictException("Email is already in use.", "EMAIL_IN_USE");
        }

        // BR-01: Create Identity user
        var (userSuccess, userErrorCode, userErrorMessage, userId, tempPassword) =
            await _identityService.CreateUserAsync(
                trimmedEmail,
                $"{request.FirstName.Trim()} {request.LastName.Trim()}",
                "Staff",
                ct);

        if (!userSuccess || !userId.HasValue || tempPassword == null)
        {
            if (userErrorCode == "EMAIL_IN_USE")
            {
                throw new ConflictException("Email is already in use.", "EMAIL_IN_USE");
            }

            throw new ValidationFailedException("email", userErrorMessage ?? "Failed to create user account.");
        }

        var staff = new Domain.Entities.Staff(
            request.FirstName,
            request.LastName,
            request.Phone,
            trimmedEmail,
            request.JobTitle,
            request.HireDate,
            userId.Value);

        _dbContext.Staff.Add(staff);

        var assignedClassroomBriefs = new List<ClassroomBriefDto>();
        if (request.ClassroomIds != null && request.ClassroomIds.Any())
        {
            var classrooms = await _dbContext.Classrooms
                .Where(c => request.ClassroomIds.Contains(c.Id))
                .ToListAsync(ct);

            foreach (var classroom in classrooms)
            {
                staff.StaffClassrooms.Add(new StaffClassroom(staff.Id, classroom.Id, _dateTimeProvider.UtcNow));
                assignedClassroomBriefs.Add(new ClassroomBriefDto(classroom.Id, classroom.Name));
            }
        }

        await _dbContext.SaveChangesAsync(ct);

        var staffDto = new StaffDto(
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
            assignedClassroomBriefs);

        return new CreateStaffResponse(staffDto, tempPassword);
    }
}

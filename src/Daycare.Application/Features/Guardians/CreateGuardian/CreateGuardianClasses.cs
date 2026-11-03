using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Guardians.DTOs;
using Daycare.Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Guardians.CreateGuardian;

public record CreateGuardianRequest(
    string FirstName,
    string LastName,
    string Phone,
    string? Email = null,
    string? Address = null,
    bool CreateAccount = false
);

public record CreateGuardianResponse(
    GuardianDto Guardian,
    string? TemporaryPassword
);

public class CreateGuardianValidator : AbstractValidator<CreateGuardianRequest>
{
    public CreateGuardianValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone is required.")
            .MaximumLength(50).WithMessage("Phone must not exceed 50 characters.");

        When(x => !string.IsNullOrEmpty(x.Email), () =>
        {
            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("A valid email address is required.")
                .MaximumLength(256).WithMessage("Email must not exceed 256 characters.");
        });

        When(x => x.CreateAccount, () =>
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required when creating an account.");
        });

        RuleFor(x => x.Address)
            .MaximumLength(300).WithMessage("Address must not exceed 300 characters.");
    }
}

public class CreateGuardianUseCase : UseCase<CreateGuardianRequest, CreateGuardianResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IIdentityService _identityService;

    public CreateGuardianUseCase(
        IApplicationDbContext dbContext,
        IIdentityService identityService,
        IValidator<CreateGuardianRequest> validator)
        : base(validator)
    {
        _dbContext = dbContext;
        _identityService = identityService;
    }

    protected override async Task<CreateGuardianResponse> HandleAsync(
        CreateGuardianRequest request,
        CancellationToken ct)
    {
        var trimmedEmail = request.Email?.Trim().ToLowerInvariant();

        Guid? userId = null;
        string? tempPassword = null;

        if (request.CreateAccount && !string.IsNullOrEmpty(trimmedEmail))
        {
            // BR-02: Check email uniqueness
            var existingGuardian = await _dbContext.Guardians
                .AnyAsync(g => g.Email != null && g.Email.ToLower() == trimmedEmail, ct);

            if (existingGuardian)
            {
                throw new ConflictException("Email is already in use.", "EMAIL_IN_USE");
            }

            // BR-01: Create user account
            var (userSuccess, userErrorCode, userErrorMessage, newUserId, password) =
                await _identityService.CreateUserAsync(
                    trimmedEmail,
                    $"{request.FirstName.Trim()} {request.LastName.Trim()}",
                    "Parent",
                    ct);

            if (!userSuccess || !newUserId.HasValue || password == null)
            {
                if (userErrorCode == "EMAIL_IN_USE")
                {
                    throw new ConflictException("Email is already in use.", "EMAIL_IN_USE");
                }

                throw new ValidationFailedException("email", userErrorMessage ?? "Failed to create user account.");
            }

            userId = newUserId;
            tempPassword = password;
        }

        var guardian = new Guardian(
            request.FirstName,
            request.LastName,
            request.Phone,
            trimmedEmail,
            request.Address,
            userId);

        _dbContext.Guardians.Add(guardian);
        await _dbContext.SaveChangesAsync(ct);

        var guardianDto = new GuardianDto(
            guardian.Id,
            guardian.UserId,
            guardian.FirstName,
            guardian.LastName,
            guardian.FirstName + " " + guardian.LastName,
            guardian.Phone,
            guardian.Email,
            guardian.Address,
            []);

        return new CreateGuardianResponse(guardianDto, tempPassword);
    }
}

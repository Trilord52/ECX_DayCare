using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Guardians.CreateGuardianAccount;

public record CreateGuardianAccountRequest(Guid GuardianId, string Email);

public record CreateGuardianAccountResponse(Guid UserId, string TemporaryPassword);

public class CreateGuardianAccountValidator : AbstractValidator<CreateGuardianAccountRequest>
{
    public CreateGuardianAccountValidator()
    {
        RuleFor(x => x.GuardianId).NotEmpty().WithMessage("Guardian ID is required.");
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");
    }
}

public class CreateGuardianAccountUseCase : UseCase<CreateGuardianAccountRequest, CreateGuardianAccountResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IIdentityService _identityService;

    public CreateGuardianAccountUseCase(
        IApplicationDbContext dbContext,
        IIdentityService identityService,
        IValidator<CreateGuardianAccountRequest> validator)
        : base(validator)
    {
        _dbContext = dbContext;
        _identityService = identityService;
    }

    protected override async Task<CreateGuardianAccountResponse> HandleAsync(
        CreateGuardianAccountRequest request,
        CancellationToken ct)
    {
        var guardian = await _dbContext.Guardians
            .FirstOrDefaultAsync(g => g.Id == request.GuardianId, ct);

        if (guardian == null)
        {
            throw new NotFoundException("Guardian", request.GuardianId);
        }

        if (guardian.UserId.HasValue)
        {
            throw new ConflictException("Guardian already has an associated user account.", "ACCOUNT_EXISTS");
        }

        var trimmedEmail = request.Email.Trim().ToLowerInvariant();

        var (success, errorCode, errorMessage, userId, tempPassword) =
            await _identityService.CreateUserAsync(
                trimmedEmail,
                $"{guardian.FirstName} {guardian.LastName}",
                "Parent",
                ct);

        if (!success || !userId.HasValue || tempPassword == null)
        {
            if (errorCode == "EMAIL_IN_USE")
            {
                throw new ConflictException("Email is already in use.", "EMAIL_IN_USE");
            }

            throw new ValidationFailedException("email", errorMessage ?? "Failed to create user account.");
        }

        guardian.LinkUser(userId.Value);
        guardian.Update(
            guardian.FirstName,
            guardian.LastName,
            guardian.Phone,
            trimmedEmail,
            guardian.Address);

        await _dbContext.SaveChangesAsync(ct);

        return new CreateGuardianAccountResponse(userId.Value, tempPassword);
    }
}

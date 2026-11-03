using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Guardians.ResetGuardianPassword;

public record ResetGuardianPasswordRequest(Guid Id);

public record ResetGuardianPasswordResponse(string TemporaryPassword);

public class ResetGuardianPasswordUseCase : UseCase<ResetGuardianPasswordRequest, ResetGuardianPasswordResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IIdentityService _identityService;

    public ResetGuardianPasswordUseCase(
        IApplicationDbContext dbContext,
        IIdentityService identityService)
    {
        _dbContext = dbContext;
        _identityService = identityService;
    }

    protected override async Task<ResetGuardianPasswordResponse> HandleAsync(
        ResetGuardianPasswordRequest request,
        CancellationToken ct)
    {
        var guardian = await _dbContext.Guardians
            .FirstOrDefaultAsync(g => g.Id == request.Id, ct);

        if (guardian == null)
        {
            throw new NotFoundException("Guardian", request.Id);
        }

        if (!guardian.UserId.HasValue)
        {
            throw new ValidationFailedException("userId", "Guardian does not have an associated user account.");
        }

        var (success, errorCode, errorMessage, tempPassword) =
            await _identityService.ResetPasswordAsync(guardian.UserId.Value, ct);

        if (!success || tempPassword == null)
        {
            throw new ValidationFailedException("password", errorMessage ?? "Failed to reset password.");
        }

        return new ResetGuardianPasswordResponse(tempPassword);
    }
}

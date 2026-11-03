using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Staff.ResetStaffPassword;

public record ResetStaffPasswordRequest(Guid Id);

public record ResetStaffPasswordResponse(string TemporaryPassword);

public class ResetStaffPasswordUseCase : UseCase<ResetStaffPasswordRequest, ResetStaffPasswordResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IIdentityService _identityService;

    public ResetStaffPasswordUseCase(
        IApplicationDbContext dbContext,
        IIdentityService identityService)
    {
        _dbContext = dbContext;
        _identityService = identityService;
    }

    protected override async Task<ResetStaffPasswordResponse> HandleAsync(
        ResetStaffPasswordRequest request,
        CancellationToken ct)
    {
        var staff = await _dbContext.Staff
            .FirstOrDefaultAsync(s => s.Id == request.Id, ct);

        if (staff == null)
        {
            throw new NotFoundException("Staff member", request.Id);
        }

        if (!staff.UserId.HasValue)
        {
            throw new ValidationFailedException("userId", "Staff member does not have an associated user account.");
        }

        var (success, errorCode, errorMessage, tempPassword) =
            await _identityService.ResetPasswordAsync(staff.UserId.Value, ct);

        if (!success || tempPassword == null)
        {
            throw new ValidationFailedException("password", errorMessage ?? "Failed to reset password.");
        }

        return new ResetStaffPasswordResponse(tempPassword);
    }
}

using System.Security.Claims;
using Daycare.Application.Abstractions;

namespace Daycare.Api.Services;

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var idString = User?.FindFirstValue("sub") ?? User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(idString, out var guid) ? guid : null;
        }
    }

    public string? Email => User?.FindFirstValue("email") ?? User?.FindFirstValue(ClaimTypes.Email);

    public string? FullName => User?.FindFirstValue("name") ?? User?.FindFirstValue(ClaimTypes.Name);

    public string? Role => User?.FindFirstValue("role") ?? User?.FindFirstValue(ClaimTypes.Role);

    public Guid? GuardianId
    {
        get
        {
            var idString = User?.FindFirstValue("guardian_id");
            return Guid.TryParse(idString, out var guid) ? guid : null;
        }
    }

    public Guid? StaffId
    {
        get
        {
            var idString = User?.FindFirstValue("staff_id");
            return Guid.TryParse(idString, out var guid) ? guid : null;
        }
    }

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
}

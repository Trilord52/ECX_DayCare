using Microsoft.AspNetCore.Identity;

namespace Daycare.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = null!;
    public bool MustChangePassword { get; set; }
    public bool IsActive { get; set; } = true;
}

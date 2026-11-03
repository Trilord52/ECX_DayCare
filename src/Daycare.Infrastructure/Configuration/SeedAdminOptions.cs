namespace Daycare.Infrastructure.Configuration;

public class SeedAdminOptions
{
    public const string SectionName = "SeedAdmin";

    public string Email { get; set; } = "admin@daycare.local";
    public string Password { get; set; } = "Admin@12345";
    public string FullName { get; set; } = "Daycare Admin";
}

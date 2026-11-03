namespace Daycare.Infrastructure.Configuration;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "daycare-api";
    public string Audience { get; set; } = "daycare-web";
    public string Key { get; set; } = null!;
    public int ExpiryMinutes { get; set; } = 120;
}

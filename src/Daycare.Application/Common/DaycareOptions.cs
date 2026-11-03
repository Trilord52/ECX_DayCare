namespace Daycare.Application.Common;

public class DaycareOptions
{
    public const string SectionName = "Daycare";

    public string Name { get; set; } = "Little Steps Daycare";
    public string TimeZoneId { get; set; } = "Africa/Addis_Ababa";
    public string Currency { get; set; } = "ETB";
    public string OpeningTime { get; set; } = "07:00";
    public string ClosingTime { get; set; } = "18:00";
    public int LateFeeBlockMinutes { get; set; } = 15;
    public decimal LateFeePerBlock { get; set; } = 50.00m;
    public int InvoiceDueDays { get; set; } = 10;
}

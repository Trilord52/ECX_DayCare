using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Microsoft.Extensions.Options;

namespace Daycare.Infrastructure.Services;

public class DateTimeProvider : IDateTimeProvider
{
    private readonly TimeZoneInfo _timeZone;

    public DateTimeProvider(IOptions<DaycareOptions> options)
    {
        var tzId = options.Value.TimeZoneId;
        try
        {
            _timeZone = TimeZoneInfo.FindSystemTimeZoneById(tzId);
        }
        catch (TimeZoneNotFoundException)
        {
            // Windows fallback
            _timeZone = TimeZoneInfo.FindSystemTimeZoneById("E. Africa Standard Time");
        }
    }

    public DateTime UtcNow => DateTime.UtcNow;

    public DateOnly LocalToday
    {
        get
        {
            var localTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timeZone);
            return DateOnly.FromDateTime(localTime);
        }
    }

    public DateTime ToLocal(DateTime utc)
    {
        var utcKind = utc.Kind == DateTimeKind.Utc
            ? utc
            : DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utcKind, _timeZone);
    }
}

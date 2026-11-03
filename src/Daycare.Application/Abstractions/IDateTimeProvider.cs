namespace Daycare.Application.Abstractions;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
    DateOnly LocalToday { get; }
    DateTime ToLocal(DateTime utc);
}

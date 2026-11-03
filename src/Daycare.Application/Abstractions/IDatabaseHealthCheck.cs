namespace Daycare.Application.Abstractions;

public interface IDatabaseHealthCheck
{
    Task<bool> CanConnectAsync(CancellationToken ct = default);
}

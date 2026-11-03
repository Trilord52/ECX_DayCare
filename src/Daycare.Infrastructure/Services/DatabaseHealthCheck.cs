using Daycare.Application.Abstractions;
using Daycare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Infrastructure.Services;

public class DatabaseHealthCheck : IDatabaseHealthCheck
{
    private readonly ApplicationDbContext _dbContext;

    public DatabaseHealthCheck(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> CanConnectAsync(CancellationToken ct = default)
    {
        return await _dbContext.Database.CanConnectAsync(ct);
    }
}

using CompanySignage.Application.Interfaces;
using CompanySignage.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CompanySignage.Application.Services;

public class LogService : ILogService
{
    private readonly IDbContextFactory _dbContextFactory;

    public LogService(IDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task LogAsync(int? userId, string action, string? description, string? ipAddress)
    {
        using var context = _dbContextFactory.CreateDbContext();
        context.ActivityLogs.Add(new ActivityLog
        {
            UserId = userId,
            Action = action,
            Description = description,
            IpAddress = ipAddress
        });
        await context.SaveChangesAsync();
    }

    public async Task<List<ActivityLog>> GetRecentAsync(int count = 100)
    {
        using var context = _dbContextFactory.CreateDbContext();
        return await context.ActivityLogs
            .Include(l => l.User)
            .OrderByDescending(l => l.CreatedAt)
            .Take(count)
            .ToListAsync();
    }
}

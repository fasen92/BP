using Microsoft.EntityFrameworkCore;
using WifiLocator.Infrastructure;

public class DbCleaner
{
    private readonly IDbContextFactory<WifiLocatorDbContext> _factory;

    public DbCleaner(IDbContextFactory<WifiLocatorDbContext> factory)
    {
        _factory = factory;
    }

    public async Task ClearAsync()
    {
        using var context = await _factory.CreateDbContextAsync();

        await context.Database.ExecuteSqlRawAsync("""
        TRUNCATE TABLE "WifiEntity","LocationEntity" CASCADE;
        """);
    }
}
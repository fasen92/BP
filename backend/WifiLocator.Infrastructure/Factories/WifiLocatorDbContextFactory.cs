using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WifiLocator.Infrastructure.Factories
{
    public class WifiLocatorDbContextFactory : IDesignTimeDbContextFactory<WifiLocatorDbContext>
    {
        public WifiLocatorDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<WifiLocatorDbContext>();

            // Specify the connection string directly here
            var connectionString = "Host=localhost;Port=5432;User Id=postgres;Database=postgres;Password=147899;SSL Mode=Disable";

            optionsBuilder.UseNpgsql(connectionString);

            return new WifiLocatorDbContext(optionsBuilder.Options);
        }
    }
}
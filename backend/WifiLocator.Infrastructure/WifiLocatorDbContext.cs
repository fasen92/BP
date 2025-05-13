using Microsoft.EntityFrameworkCore;
using WifiLocator.Infrastructure.Entities;


namespace WifiLocator.Infrastructure
{
    public class WifiLocatorDbContext(DbContextOptions<WifiLocatorDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<WifiEntity>()
                .HasMany(e => e.Locations)
                .WithOne(e => e.Wifi)
                .HasForeignKey(e => e.WifiId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WifiEntity>()
                .HasOne(e => e.Address)
                .WithMany()
                .HasForeignKey(e => e.AddressId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

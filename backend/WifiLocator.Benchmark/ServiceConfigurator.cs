using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using WifiLocator.Infrastructure;
using WifiLocator.Infrastructure.Repositories;
using WifiLocator.Infrastructure.UnitOfWork;
using WifiLocator.Core.Mappers;
using WifiLocator.Core.Models;
using WifiLocator.Infrastructure.Entities;
using WifiLocator.Core.Approximation;
using WifiLocator.Core.Approximation.Interfaces;
using WifiLocator.Core.Services;
using WifiLocator.Core.Services.Interfaces;

public static class ServiceConfigurator
{
    public static void Configure(IServiceCollection services, IConfiguration config)
    {
        // DB
        services.AddDbContextFactory<WifiLocatorDbContext>(options =>
            options.UseNpgsql(config.GetConnectionString("PostgresConnection")));

        // Unit of work & Repositories
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUnitOfWorkFactory, UnitOfWorkFactory>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // Mappers
        services.AddScoped<CompositeWifiMapper>();
        services.AddScoped<WifiMapper>();
        services.AddScoped<WifiDisplayMapper>();
        services.AddScoped<ModelMapper<WifiEntity, WifiModel>, WifiMapper>();
        services.AddScoped<ModelMapper<LocationEntity, LocationModel>, LocationMapper>();
        services.AddScoped<ModelMapper<AddressEntity, AddressModel>, AddressMapper>();

        // Approximation
        services.AddScoped<IWifiLocationApproximator, WifiLocationApproximator>();
        services.AddScoped<IGeoConverter, GeoCoordinateConverter>();
        services.AddScoped<IClustering, Clustering>();

        // Services
        services.AddScoped<IWifiService, WifiService>();
        services.AddScoped<ILocationService, LocationService>();
        services.AddScoped<IFileService, FileService>();
        services.AddScoped<IAddressService, AddressService>();

        // GeoService (ak je potrebné, môžeš zakomentovať ak nie je použitý)
        services.AddHttpClient<IGeoService, GeoService>();
        services.AddScoped<IGeoService, GeoService>();
    }
}

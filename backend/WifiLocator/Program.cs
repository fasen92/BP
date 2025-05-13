using Microsoft.EntityFrameworkCore;
using WifiLocator.Core.Models;
using WifiLocator.Core.Mappers;
using WifiLocator.Infrastructure;
using WifiLocator.Infrastructure.Entities;
using WifiLocator.Infrastructure.Repositories;
using WifiLocator.Infrastructure.UnitOfWork;
using WifiLocator.Core.Approximation;
using WifiLocator.Core.Services.Interfaces;
using WifiLocator.Core.Services;
using WifiLocator.Core.Approximation.Interfaces;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContextFactory<WifiLocatorDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgresConnection")));

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IUnitOfWorkFactory, UnitOfWorkFactory>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

builder.Services.AddScoped<CompositeWifiMapper>();
builder.Services.AddScoped<WifiMapper>();
builder.Services.AddScoped<WifiDisplayMapper>();
builder.Services.AddScoped<ModelMapper<WifiEntity, WifiModel>, WifiMapper>();
builder.Services.AddScoped<ModelMapper<LocationEntity, LocationModel>, LocationMapper>();
builder.Services.AddScoped<ModelMapper<AddressEntity, AddressModel>, AddressMapper>();

builder.Services.AddScoped<IWifiLocationApproximator, WifiLocationApproximator>();
builder.Services.AddScoped<IGeoConverter, GeoCoordinateConverter>();
builder.Services.AddScoped<IClustering, Clustering>();
builder.Services.AddScoped<IWifiService, WifiService>();
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IAddressService, AddressService>();

builder.Services.AddSingleton<LocationUpdateBackgroundService>();
builder.Services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<LocationUpdateBackgroundService>());
builder.Services.AddSingleton<ILocationUpdateController>(sp => sp.GetRequiredService<LocationUpdateBackgroundService>());
builder.Services.AddHttpClient<IGeoService, GeoService>();
builder.Services.AddScoped<IGeoService, GeoService>();
builder.Services.AddHostedService<FileProcessingBackgroundService>();
builder.Services.AddSingleton<IFileQueueManager, FileQueueManager>();



builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors("AllowAll");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseRouting();

app.MapControllers();

app.Run();

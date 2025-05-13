using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WifiLocator.Core.Models;
using WifiLocator.Core.Services.Interfaces;

namespace WifiLocator.Core.Services
{
    public class LocationUpdateBackgroundService(IServiceScopeFactory scopeFactory) : BackgroundService, ILocationUpdateController
    {
        private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
        private volatile bool _paused = false;
        public void Pause()
        {
            _paused = true;
        }

        public void Resume()
        {
            _paused = false;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {

                    if (_paused)
                    {
                        await Task.Delay(5000, stoppingToken);
                        continue;
                    }


                    using var scope = _scopeFactory.CreateScope();

                    IWifiService wifiService = scope.ServiceProvider.GetRequiredService<IWifiService>();
                    IAddressService addressService = scope.ServiceProvider.GetRequiredService<IAddressService>();
                    IGeoService geoService = scope.ServiceProvider.GetRequiredService<IGeoService>();

                    List<WifiModel> wifiRecords = await wifiService.GetByMissingLocationAsync();
                    if (wifiRecords.Count == 0)
                    {
                        await Task.Delay(10000, stoppingToken);
                        continue;
                    }

                    foreach (var wifi in wifiRecords)
                    {
                        if (_paused)
                        {
                            break;
                        }

                        AddressModel addressModel = await geoService.GetAddressFromLocation(wifi);
                        addressModel = await addressService.GetByAddressAsync(addressModel);

                        await wifiService.SaveAsync(wifi, addressModel.Id);
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // no action needed
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Background service crashed: {ex.Message}");
            }

        }
    }
}

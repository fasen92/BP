using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WifiLocator.Core.Models;
using WifiLocator.Core.Services.Interfaces;
using System.Diagnostics;

namespace WifiLocator.Core.Services
{
    public class FileProcessingBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IFileQueueManager _queueManager;
        private readonly ILocationUpdateController _locationController;

        public FileProcessingBackgroundService(IServiceScopeFactory scopeFactory, IFileQueueManager queueManager, ILocationUpdateController locationController)
        {
            _scopeFactory = scopeFactory;
            _queueManager = queueManager;
            _locationController = locationController;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_queueManager.TryGetNextFile(out FileProcessModel fileProcess))
                {
                    _locationController.Pause();

                    if (!File.Exists(fileProcess.FilePath))
                    { 
                        _queueManager.RemoveFileFromQueue(fileProcess);
                        continue;
                    }

                    using var scope = _scopeFactory.CreateScope();
                    var fileService = scope.ServiceProvider.GetRequiredService<IFileService>();
                    var wifiService = scope.ServiceProvider.GetRequiredService<IWifiService>();

                    List<WifiModel> wifiBatch = [];
                    List<LocationModel> locationBatch = [];
                    int batchSize = 1000;
                    try
                    {
                        var stopwatch = Stopwatch.StartNew();
                        await foreach (var (wifi, location) in fileService.ProcessCSVAsync(
                            fileProcess.FilePath,
                            fileProcess.ProcessedRecords,
                            count =>
                            {
                                if (fileProcess.TotalRecords == 0)
                                {
                                    fileProcess.TotalRecords = count;
                                    fileProcess = _queueManager.SaveProcessed(fileProcess);
                                }
                            }))
                        {
                            wifiBatch.Add(wifi);
                            locationBatch.Add(location);

                            if (wifiBatch.Count >= batchSize)
                            {
                                await wifiService.ProcessApproximationAsync(locationBatch, wifiBatch);
                                stopwatch.Stop();
                                using (StreamWriter writer = new StreamWriter("log.txt", append: true))
                                {
                                    writer.WriteLine($"Time to load and process first batch with count{wifiBatch.Count}: {stopwatch.ElapsedMilliseconds} ms");
                                }
                                fileProcess.ProcessedRecords += wifiBatch.Count;
                                fileProcess = _queueManager.SaveProcessed(fileProcess);
                                wifiBatch.Clear();
                                locationBatch.Clear();

                                stopwatch = Stopwatch.StartNew();
                            }
                        }

                        if (wifiBatch.Count > 0)
                        {
                            await wifiService.ProcessApproximationAsync(locationBatch, wifiBatch);
                            fileProcess.ProcessedRecords += wifiBatch.Count;
                            fileProcess = _queueManager.SaveProcessed(fileProcess);
                            stopwatch.Stop();
                            using (StreamWriter writer = new StreamWriter("log.txt", append: true))
                            {
                                writer.WriteLine($"Time to load and process last batch with count {wifiBatch.Count}: {stopwatch.ElapsedMilliseconds} ms");
                            }
                        }

                    }
                    catch (Exception)
                    {
                        fileProcess.Error = true;
                    }
                    finally {
                        await _queueManager.TryDeleteFile(fileProcess);
                        _locationController.Resume();
                    }
                }

                await Task.Delay(500, stoppingToken);
            }
        }
    }

}


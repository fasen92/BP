using WifiLocator.Core.Models;
using WifiLocator.Core.Services.Interfaces;

public class FileProcessor
{
    private readonly IFileService _fileService;
    private readonly IWifiService _wifiService;

    public FileProcessor(IFileService fileService, IWifiService wifiService)
    {
        _fileService = fileService;
        _wifiService = wifiService;
    }

    public async Task RunAsync(string filePath)
    {
        List<WifiModel> wifiBatch = [];
        List<LocationModel> locationBatch = [];

        await foreach (var (wifi, location) in _fileService.ProcessCSVAsync(filePath, 0, _ => { }))
        {
            wifiBatch.Add(wifi);
            locationBatch.Add(location);

            if (wifiBatch.Count >= 1000)
            {
                await _wifiService.ProcessApproximationAsync(locationBatch, wifiBatch);
                wifiBatch.Clear();
                locationBatch.Clear();
            }
        }

        if (wifiBatch.Count > 0)
        {
            await _wifiService.ProcessApproximationAsync(locationBatch, wifiBatch);
        }
    }
}

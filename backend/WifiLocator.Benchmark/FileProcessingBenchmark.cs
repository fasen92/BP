using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WifiLocator.Core.Services.Interfaces;
using System.Runtime;


[MemoryDiagnoser]
[SimpleJob(RunStrategy.ColdStart,warmupCount: 0, iterationCount:5, launchCount:1)]
public class FileProcessingBenchmark
{
    private FileProcessor _processor = null!;
    private DbCleaner _dbCleaner = null!;


    public static IEnumerable<string> CsvFiles => Directory
    .GetFiles(Path.Combine(AppContext.BaseDirectory, "data"), "*.csv")
    .OrderBy(f => f);

    [ParamsSource(nameof(CsvFiles))]
    public string FilePath { get; set; } = "";

    [GlobalSetup]
    public void Setup()
    {

        GCSettings.LatencyMode = GCLatencyMode.LowLatency;

        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        

        var services = new ServiceCollection();
        ServiceConfigurator.Configure(services, config);

        services.AddSingleton<DbCleaner>();

        var provider = services.BuildServiceProvider();
        _processor = new FileProcessor(
            provider.GetRequiredService<IFileService>(),
            provider.GetRequiredService<IWifiService>()
        );

        _dbCleaner = provider.GetRequiredService<DbCleaner>();

        _dbCleaner.ClearAsync().Wait();
    }

    [IterationCleanup]
    public void ClearDatabaseAfterIteration()
    {
        _dbCleaner.ClearAsync().Wait();
    }

    [Benchmark]
    public async Task ProcessSingleFile()
    {
        await _processor.RunAsync(FilePath);
    }
}

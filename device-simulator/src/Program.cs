using System.CommandLine;
using Microsoft.Extensions.Logging;
using DeviceSimulator;

var rootCommand = new RootCommand("Device Simulator for testing manifest APIs");

// Options
var apiUrlOption = new Option<string>(
    name: "--api-url",
    description: "API base URL",
    getDefaultValue: () => "http://localhost:5000");

var deviceCountOption = new Option<int>(
    name: "--device-count",
    description: "Number of devices to simulate",
    getDefaultValue: () => 100);

var parallelismOption = new Option<int>(
    name: "--parallelism",
    description: "Max parallel requests",
    getDefaultValue: () => 10);

var ttlOption = new Option<int>(
    name: "--ttl",
    description: "Presigned URL TTL in minutes",
    getDefaultValue: () => 10);

var downloadFilesOption = new Option<bool>(
    name: "--download-files",
    description: "Actually download files (not just get manifest)",
    getDefaultValue: () => false);

var useConditionalGetOption = new Option<bool>(
    name: "--use-conditional-get",
    description: "Use If-None-Match headers for conditional GET",
    getDefaultValue: () => true);

var outputFileOption = new Option<string?>(
    name: "--output",
    description: "Output file for metrics (JSON)",
    getDefaultValue: () => null);

rootCommand.AddOption(apiUrlOption);
rootCommand.AddOption(deviceCountOption);
rootCommand.AddOption(parallelismOption);
rootCommand.AddOption(ttlOption);
rootCommand.AddOption(downloadFilesOption);
rootCommand.AddOption(useConditionalGetOption);
rootCommand.AddOption(outputFileOption);

rootCommand.SetHandler(async (apiUrl, deviceCount, parallelism, ttl, downloadFiles, useConditionalGet, outputFile) =>
{
    using var loggerFactory = LoggerFactory.Create(builder =>
    {
        builder.AddConsole();
        builder.SetMinimumLevel(LogLevel.Information);
    });

    var logger = loggerFactory.CreateLogger<Simulator>();

    var config = new SimulatorConfig
    {
        ApiBaseUrl = apiUrl,
        DeviceCount = deviceCount,
        MaxParallelism = parallelism,
        TtlMinutes = ttl,
        DownloadFiles = downloadFiles,
        UseConditionalGet = useConditionalGet,
        OutputFile = outputFile
    };

    var simulator = new Simulator(config, logger);
    await simulator.RunAsync();

}, apiUrlOption, deviceCountOption, parallelismOption, ttlOption, downloadFilesOption, useConditionalGetOption, outputFileOption);

return await rootCommand.InvokeAsync(args);

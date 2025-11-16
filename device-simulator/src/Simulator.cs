using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using DeviceManifest.Shared;
using Microsoft.Extensions.Logging;

namespace DeviceSimulator;

/// <summary>
/// Simulates multiple devices calling the manifest API
/// </summary>
public class Simulator
{
    private readonly SimulatorConfig _config;
    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;
    private readonly ConcurrentDictionary<string, DeviceState> _deviceStates;

    public Simulator(SimulatorConfig config, ILogger logger)
    {
        _config = config;
        _logger = logger;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        _deviceStates = new ConcurrentDictionary<string, DeviceState>();
    }

    public async Task RunAsync()
    {
        _logger.LogInformation("Starting device simulator");
        _logger.LogInformation("API URL: {ApiUrl}", _config.ApiBaseUrl);
        _logger.LogInformation("Device count: {DeviceCount}", _config.DeviceCount);
        _logger.LogInformation("Parallelism: {Parallelism}", _config.MaxParallelism);
        _logger.LogInformation("Download files: {DownloadFiles}", _config.DownloadFiles);
        _logger.LogInformation("Use conditional GET: {UseConditionalGet}", _config.UseConditionalGet);

        var metrics = new SimulatorMetrics
        {
            StartTime = DateTimeOffset.UtcNow,
            DeviceCount = _config.DeviceCount,
            ApiBaseUrl = _config.ApiBaseUrl
        };

        var stopwatch = Stopwatch.StartNew();

        // Generate CEBs
        var cebs = GenerateCebs(_config.DeviceCount);

        // Run simulation with parallelism
        var options = new ParallelOptions { MaxDegreeOfParallelism = _config.MaxParallelism };

        await Parallel.ForEachAsync(cebs, options, async (ceb, ct) =>
        {
            try
            {
                await SimulateDeviceAsync(ceb, metrics, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error simulating device {Ceb}", ceb);
                metrics.ErrorCount++;
            }
        });

        stopwatch.Stop();

        metrics.EndTime = DateTimeOffset.UtcNow;
        metrics.TotalDurationSeconds = stopwatch.Elapsed.TotalSeconds;

        // Log summary
        LogSummary(metrics);

        // Save metrics to file if specified
        if (!string.IsNullOrWhiteSpace(_config.OutputFile))
        {
            await SaveMetricsAsync(metrics, _config.OutputFile);
        }
    }

    private async Task SimulateDeviceAsync(string ceb, SimulatorMetrics metrics, CancellationToken cancellationToken)
    {
        var deviceState = _deviceStates.GetOrAdd(ceb, _ => new DeviceState { Ceb = ceb });

        _logger.LogDebug("Simulating device {Ceb}", ceb);

        // Step 1: Get manifest
        var manifestStopwatch = Stopwatch.StartNew();
        var manifest = await GetManifestAsync(ceb, cancellationToken);
        manifestStopwatch.Stop();

        if (manifest == null)
        {
            _logger.LogWarning("Failed to get manifest for device {Ceb}", ceb);
            metrics.ErrorCount++;
            return;
        }

        metrics.ManifestRequestCount++;
        metrics.TotalManifestTimeMs += manifestStopwatch.ElapsedMilliseconds;

        _logger.LogInformation("Device {Ceb}: Received manifest with {FileCount} files", ceb, manifest.Files.Count);

        // Step 2: Process files
        foreach (var file in manifest.Files)
        {
            // Check if we already have this file (by ETag)
            if (_config.UseConditionalGet && deviceState.FileCache.TryGetValue(file.Name, out var cachedETag))
            {
                if (cachedETag == file.ETag)
                {
                    _logger.LogDebug("Device {Ceb}: File {FileName} unchanged (ETag: {ETag})", ceb, file.Name, file.ETag);
                    metrics.CacheHitCount++;
                    continue;
                }
            }

            // Download file if enabled
            if (_config.DownloadFiles)
            {
                var downloadStopwatch = Stopwatch.StartNew();
                var downloaded = await DownloadFileAsync(file, cancellationToken);
                downloadStopwatch.Stop();

                if (downloaded)
                {
                    metrics.FileDownloadCount++;
                    metrics.TotalBytesDownloaded += file.Size;
                    metrics.TotalDownloadTimeMs += downloadStopwatch.ElapsedMilliseconds;

                    // Update cache
                    deviceState.FileCache[file.Name] = file.ETag;

                    _logger.LogInformation("Device {Ceb}: Downloaded {FileName} ({Size} bytes) in {Duration}ms",
                        ceb, file.Name, file.Size, downloadStopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogWarning("Device {Ceb}: Failed to download {FileName}", ceb, file.Name);
                    metrics.ErrorCount++;
                }
            }
            else
            {
                // Just mark as "would download"
                metrics.FileDownloadCount++;
                metrics.TotalBytesDownloaded += file.Size;
                deviceState.FileCache[file.Name] = file.ETag;
            }
        }
    }

    private async Task<DeviceManifest.Shared.DeviceManifest?> GetManifestAsync(string ceb, CancellationToken cancellationToken)
    {
        try
        {
            var url = $"{_config.ApiBaseUrl}/api/manifest/{ceb}?ttl={_config.TtlMinutes}";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<DeviceManifest.Shared.DeviceManifest>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting manifest for {Ceb}", ceb);
            return null;
        }
    }

    private async Task<bool> DownloadFileAsync(FileItem file, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync(file.PresignedUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            // Read to byte array to actually download
            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

            // Verify size
            if (bytes.Length != file.Size)
            {
                _logger.LogWarning("File size mismatch for {FileName}: expected {Expected}, got {Actual}",
                    file.Name, file.Size, bytes.Length);
                return false;
            }

            // In real scenario, verify SHA256 hash
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file {FileName}", file.Name);
            return false;
        }
    }

    private List<string> GenerateCebs(int count)
    {
        var cebs = new List<string>();
        for (int i = 0; i < count; i++)
        {
            // Generate 19-character CEB (simplified: use formatted number)
            var ceb = $"CEB{i:D16}"; // CEB + 16 digits = 19 chars
            cebs.Add(ceb);
        }
        return cebs;
    }

    private void LogSummary(SimulatorMetrics metrics)
    {
        _logger.LogInformation("========== SIMULATION SUMMARY ==========");
        _logger.LogInformation("Total duration: {Duration:F2} seconds", metrics.TotalDurationSeconds);
        _logger.LogInformation("Devices: {DeviceCount}", metrics.DeviceCount);
        _logger.LogInformation("Manifest requests: {Count}", metrics.ManifestRequestCount);
        _logger.LogInformation("Files downloaded: {Count}", metrics.FileDownloadCount);
        _logger.LogInformation("Total bytes downloaded: {Bytes:N0} ({GB:F2} GB)", metrics.TotalBytesDownloaded, metrics.TotalBytesDownloaded / 1024.0 / 1024.0 / 1024.0);
        _logger.LogInformation("Cache hits: {Count}", metrics.CacheHitCount);
        _logger.LogInformation("Errors: {Count}", metrics.ErrorCount);

        if (metrics.ManifestRequestCount > 0)
        {
            _logger.LogInformation("Avg manifest time: {Avg:F2} ms", metrics.TotalManifestTimeMs / metrics.ManifestRequestCount);
        }

        if (metrics.FileDownloadCount > 0 && metrics.TotalDownloadTimeMs > 0)
        {
            _logger.LogInformation("Avg download time: {Avg:F2} ms", metrics.TotalDownloadTimeMs / metrics.FileDownloadCount);
            _logger.LogInformation("Avg download speed: {Speed:F2} MB/s",
                (metrics.TotalBytesDownloaded / 1024.0 / 1024.0) / (metrics.TotalDownloadTimeMs / 1000.0));
        }

        _logger.LogInformation("========================================");
    }

    private async Task SaveMetricsAsync(SimulatorMetrics metrics, string outputFile)
    {
        try
        {
            var json = JsonSerializer.Serialize(metrics, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(outputFile, json);
            _logger.LogInformation("Metrics saved to {OutputFile}", outputFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving metrics to {OutputFile}", outputFile);
        }
    }
}

/// <summary>
/// Tracks state for a single device
/// </summary>
public class DeviceState
{
    public string Ceb { get; set; } = string.Empty;
    public Dictionary<string, string> FileCache { get; set; } = new(); // fileName -> ETag
}

/// <summary>
/// Metrics collected during simulation
/// </summary>
public class SimulatorMetrics
{
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public double TotalDurationSeconds { get; set; }
    public int DeviceCount { get; set; }
    public string ApiBaseUrl { get; set; } = string.Empty;
    public int ManifestRequestCount { get; set; }
    public int FileDownloadCount { get; set; }
    public long TotalBytesDownloaded { get; set; }
    public int CacheHitCount { get; set; }
    public int ErrorCount { get; set; }
    public long TotalManifestTimeMs { get; set; }
    public long TotalDownloadTimeMs { get; set; }
}

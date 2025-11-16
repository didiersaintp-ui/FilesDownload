namespace DeviceSimulator;

/// <summary>
/// Configuration for the device simulator
/// </summary>
public class SimulatorConfig
{
    public string ApiBaseUrl { get; set; } = "http://localhost:5000";
    public int DeviceCount { get; set; } = 100;
    public int MaxParallelism { get; set; } = 10;
    public int TtlMinutes { get; set; } = 10;
    public bool DownloadFiles { get; set; } = false;
    public bool UseConditionalGet { get; set; } = true;
    public string? OutputFile { get; set; }
}

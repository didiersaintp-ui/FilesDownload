namespace DeviceManifest.Shared;

/// <summary>
/// Manifest returned to devices listing all available files
/// </summary>
public class DeviceManifest
{
    /// <summary>
    /// Device CEB identifier (19 characters)
    /// </summary>
    public string Ceb { get; set; } = string.Empty;

    /// <summary>
    /// Manifest generation timestamp
    /// </summary>
    public DateTimeOffset GeneratedAt { get; set; }

    /// <summary>
    /// List of files available for this device
    /// </summary>
    public List<FileItem> Files { get; set; } = new();

    /// <summary>
    /// Manifest version (for tracking)
    /// </summary>
    public string Version { get; set; } = "1.0";
}

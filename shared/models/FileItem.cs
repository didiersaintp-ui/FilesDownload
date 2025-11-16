namespace DeviceManifest.Shared;

/// <summary>
/// Represents a single file in the manifest
/// </summary>
public class FileItem
{
    /// <summary>
    /// Name of the file
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Size in bytes
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// ETag for cache validation
    /// </summary>
    public string ETag { get; set; } = string.Empty;

    /// <summary>
    /// SHA256 hash for integrity verification
    /// </summary>
    public string Sha256 { get; set; } = string.Empty;

    /// <summary>
    /// Presigned URL (SAS or MinIO presigned)
    /// </summary>
    public string PresignedUrl { get; set; } = string.Empty;

    /// <summary>
    /// Expiration time of the presigned URL
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }
}

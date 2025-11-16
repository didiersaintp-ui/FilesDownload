namespace DeviceManifest.Shared;

/// <summary>
/// Storage service interface for generating presigned URLs
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Get manifest for a specific device CEB
    /// </summary>
    /// <param name="ceb">Device CEB identifier</param>
    /// <param name="ttlMinutes">Time-to-live for presigned URLs in minutes (default: 10)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Device manifest with presigned URLs</returns>
    Task<DeviceManifest> GetManifestAsync(string ceb, int ttlMinutes = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get file metadata (ETag, SHA256) without downloading
    /// </summary>
    /// <param name="fileName">File name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File metadata</returns>
    Task<FileItem?> GetFileMetadataAsync(string fileName, CancellationToken cancellationToken = default);
}

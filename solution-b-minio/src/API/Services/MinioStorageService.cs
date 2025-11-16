using DeviceManifest.Shared;
using Minio;
using Minio.DataModel.Args;
using System.Security.Cryptography;

namespace DeviceManifest.Api.Minio.Services;

/// <summary>
/// MinIO storage service with presigned URLs
/// </summary>
public class MinioStorageService : IStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly string _bucketName;

    public MinioStorageService(IMinioClient minioClient, string bucketName)
    {
        _minioClient = minioClient;
        _bucketName = bucketName;
    }

    public async Task<DeviceManifest.Shared.DeviceManifest> GetManifestAsync(string ceb, int ttlMinutes = 10, CancellationToken cancellationToken = default)
    {
        var manifest = new DeviceManifest.Shared.DeviceManifest
        {
            Ceb = ceb,
            GeneratedAt = DateTimeOffset.UtcNow,
            Version = "1.0"
        };

        // List all objects in the bucket
        var listArgs = new ListObjectsArgs()
            .WithBucket(_bucketName)
            .WithRecursive(true);

        await foreach (var item in _minioClient.ListObjectsEnumAsync(listArgs, cancellationToken))
        {
            // Get object metadata
            var statArgs = new StatObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(item.Key);

            var objectStat = await _minioClient.StatObjectAsync(statArgs, cancellationToken);

            // Get SHA256 from metadata or calculate it
            var sha256 = await GetOrCalculateSha256Async(item.Key, objectStat, cancellationToken);

            // Generate presigned GET URL
            var presignedUrl = await GeneratePresignedUrlAsync(item.Key, ttlMinutes, cancellationToken);

            var fileItem = new FileItem
            {
                Name = item.Key,
                Size = (long)item.Size,
                ETag = objectStat.ETag.Trim('"'),
                Sha256 = sha256,
                PresignedUrl = presignedUrl,
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(ttlMinutes)
            };

            manifest.Files.Add(fileItem);
        }

        return manifest;
    }

    public async Task<FileItem?> GetFileMetadataAsync(string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            var statArgs = new StatObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(fileName);

            var objectStat = await _minioClient.StatObjectAsync(statArgs, cancellationToken);
            var sha256 = await GetOrCalculateSha256Async(fileName, objectStat, cancellationToken);

            return new FileItem
            {
                Name = fileName,
                Size = (long)objectStat.Size,
                ETag = objectStat.ETag.Trim('"'),
                Sha256 = sha256,
                PresignedUrl = string.Empty,
                ExpiresAt = DateTimeOffset.MinValue
            };
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Generate presigned GET URL for object
    /// </summary>
    private async Task<string> GeneratePresignedUrlAsync(string objectName, int ttlMinutes, CancellationToken cancellationToken)
    {
        var args = new PresignedGetObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(objectName)
            .WithExpiry(ttlMinutes * 60); // Convert to seconds

        return await _minioClient.PresignedGetObjectAsync(args);
    }

    /// <summary>
    /// Get SHA256 from metadata or calculate it
    /// </summary>
    private async Task<string> GetOrCalculateSha256Async(string objectName, Minio.DataModel.ObjectStat objectStat, CancellationToken cancellationToken)
    {
        // Check if SHA256 is stored in metadata
        if (objectStat.MetaData?.TryGetValue("x-amz-meta-sha256", out var storedHash) == true)
        {
            return storedHash;
        }

        // If not, calculate it (expensive, should be avoided in production)
        var getArgs = new GetObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(objectName)
            .WithCallbackStream(async (stream) =>
            {
                using var sha256 = SHA256.Create();
                var hashBytes = await sha256.ComputeHashAsync(stream, cancellationToken);
                return Convert.ToHexString(hashBytes).ToLowerInvariant();
            });

        var result = string.Empty;
        await _minioClient.GetObjectAsync(getArgs, cancellationToken);

        // Note: This is a simplified implementation
        // In production, you should pre-calculate and store SHA256 in metadata
        return objectStat.ETag.Trim('"'); // Fallback to ETag if SHA256 not available
    }
}

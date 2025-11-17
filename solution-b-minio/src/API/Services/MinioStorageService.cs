using DeviceManifest.Shared;
using Minio;
using Minio.DataModel;
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
    /// Get SHA256 from metadata or use ETag as fallback
    /// </summary>
    private async Task<string> GetOrCalculateSha256Async(string objectName, ObjectStat objectStat, CancellationToken cancellationToken)
    {
        // Check if SHA256 is stored in metadata
        if (objectStat.MetaData?.TryGetValue("x-amz-meta-sha256", out var storedHash) == true)
        {
            return storedHash;
        }

        // Fallback to ETag (which is MD5 for MinIO)
        // NOTE: In production, SHA256 should be pre-calculated and stored in metadata when uploading files
        // This is just for POC purposes - ETag is sufficient for cache validation
        return await Task.FromResult(objectStat.ETag.Trim('"'));
    }
}

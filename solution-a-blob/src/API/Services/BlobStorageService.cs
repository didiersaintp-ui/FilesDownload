using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using DeviceManifest.Shared;
using System.Security.Cryptography;

namespace DeviceManifest.Api.Services;

/// <summary>
/// Azure Blob Storage service with User Delegation SAS
/// </summary>
public class BlobStorageService : IStorageService
{
    private readonly BlobContainerClient _containerClient;
    private readonly string _storageAccountName;
    private readonly DefaultAzureCredential _credential;

    public BlobStorageService(string storageAccountName, string containerName, DefaultAzureCredential credential)
    {
        _storageAccountName = storageAccountName;
        _credential = credential;

        var blobServiceClient = new BlobServiceClient(
            new Uri($"https://{storageAccountName}.blob.core.windows.net"),
            credential);

        _containerClient = blobServiceClient.GetBlobContainerClient(containerName);
    }

    public async Task<DeviceManifest.Shared.DeviceManifest> GetManifestAsync(string ceb, int ttlMinutes = 10, CancellationToken cancellationToken = default)
    {
        var manifest = new DeviceManifest.Shared.DeviceManifest
        {
            Ceb = ceb,
            GeneratedAt = DateTimeOffset.UtcNow,
            Version = "1.0"
        };

        // List all blobs in the container
        await foreach (var blobItem in _containerClient.GetBlobsAsync(cancellationToken: cancellationToken))
        {
            var blobClient = _containerClient.GetBlobClient(blobItem.Name);

            // Get blob properties for ETag
            var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);

            // Calculate SHA256 (in real scenario, this should be stored as metadata)
            var sha256 = await CalculateSha256Async(blobClient, cancellationToken);

            // Generate User Delegation SAS
            var sasUri = await GenerateUserDelegationSasAsync(blobClient, ttlMinutes, cancellationToken);

            var fileItem = new FileItem
            {
                Name = blobItem.Name,
                Size = blobItem.Properties.ContentLength ?? 0,
                ETag = properties.Value.ETag.ToString().Trim('"'),
                Sha256 = sha256,
                PresignedUrl = sasUri,
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(ttlMinutes)
            };

            manifest.Files.Add(fileItem);
        }

        return manifest;
    }

    public async Task<FileItem?> GetFileMetadataAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var blobClient = _containerClient.GetBlobClient(fileName);

        if (!await blobClient.ExistsAsync(cancellationToken))
        {
            return null;
        }

        var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
        var sha256 = await CalculateSha256Async(blobClient, cancellationToken);

        return new FileItem
        {
            Name = fileName,
            Size = properties.Value.ContentLength,
            ETag = properties.Value.ETag.ToString().Trim('"'),
            Sha256 = sha256,
            PresignedUrl = string.Empty, // Not needed for metadata
            ExpiresAt = DateTimeOffset.MinValue
        };
    }

    /// <summary>
    /// Generate User Delegation SAS (AAD-based, no account keys needed)
    /// </summary>
    private async Task<string> GenerateUserDelegationSasAsync(BlobClient blobClient, int ttlMinutes, CancellationToken cancellationToken)
    {
        var blobServiceClient = _containerClient.GetParentBlobServiceClient();

        // Get user delegation key
        var userDelegationKey = await blobServiceClient.GetUserDelegationKeyAsync(
            startsOn: DateTimeOffset.UtcNow,
            expiresOn: DateTimeOffset.UtcNow.AddMinutes(ttlMinutes),
            cancellationToken: cancellationToken);

        // Create SAS token
        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _containerClient.Name,
            BlobName = blobClient.Name,
            Resource = "b", // b = blob
            StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5), // Allow 5 min clock skew
            ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(ttlMinutes),
        };

        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        var sasToken = sasBuilder.ToSasQueryParameters(userDelegationKey, _storageAccountName).ToString();

        return $"{blobClient.Uri}?{sasToken}";
    }

    /// <summary>
    /// Calculate SHA256 hash of blob content
    /// NOTE: In production, SHA256 should be pre-calculated and stored as blob metadata
    /// </summary>
    private async Task<string> CalculateSha256Async(BlobClient blobClient, CancellationToken cancellationToken)
    {
        // First check if SHA256 is stored in metadata
        var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
        if (properties.Value.Metadata.TryGetValue("sha256", out var storedHash))
        {
            return storedHash;
        }

        // If not, calculate it (this is expensive, should be avoided in prod)
        using var stream = await blobClient.OpenReadAsync(cancellationToken: cancellationToken);
        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(stream, cancellationToken);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}

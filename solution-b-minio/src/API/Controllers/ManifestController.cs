using Microsoft.AspNetCore.Mvc;
using DeviceManifest.Api.Minio.Services;

namespace DeviceManifest.Api.Minio.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ManifestController : ControllerBase
{
    private readonly MinioStorageService _storageService;
    private readonly ILogger<ManifestController> _logger;

    public ManifestController(MinioStorageService storageService, ILogger<ManifestController> logger)
    {
        _storageService = storageService;
        _logger = logger;
    }

    /// <summary>
    /// Get manifest for a specific device CEB
    /// </summary>
    /// <param name="ceb">Device CEB identifier (19 characters)</param>
    /// <param name="ttl">TTL for presigned URLs in minutes (default: 10)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Device manifest with presigned URLs</returns>
    [HttpGet("{ceb}")]
    [ProducesResponseType(typeof(DeviceManifest.Shared.DeviceManifest), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetManifest(
        string ceb,
        [FromQuery] int ttl = 10,
        CancellationToken cancellationToken = default)
    {
        // Validate CEB format (19 characters)
        if (string.IsNullOrWhiteSpace(ceb) || ceb.Length != 19)
        {
            _logger.LogWarning("Invalid CEB format: {Ceb}", ceb);
            return BadRequest(new { error = "CEB must be exactly 19 characters" });
        }

        // Validate TTL range
        if (ttl < 5 || ttl > 60)
        {
            _logger.LogWarning("Invalid TTL: {Ttl}", ttl);
            return BadRequest(new { error = "TTL must be between 5 and 60 minutes" });
        }

        try
        {
            _logger.LogInformation("Generating manifest for CEB: {Ceb}, TTL: {Ttl} minutes", ceb, ttl);

            var manifest = await _storageService.GetManifestAsync(ceb, ttl, cancellationToken);

            _logger.LogInformation("Manifest generated for CEB: {Ceb}, Files: {FileCount}", ceb, manifest.Files.Count);

            return Ok(manifest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating manifest for CEB: {Ceb}", ceb);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get file metadata without presigned URL (for conditional requests)
    /// </summary>
    /// <param name="fileName">File name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File metadata (ETag, SHA256, size)</returns>
    [HttpGet("file/{fileName}")]
    [ProducesResponseType(typeof(DeviceManifest.Shared.FileItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFileMetadata(string fileName, CancellationToken cancellationToken = default)
    {
        var fileItem = await _storageService.GetFileMetadataAsync(fileName, cancellationToken);

        if (fileItem == null)
        {
            return NotFound(new { error = "File not found" });
        }

        return Ok(fileItem);
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("/health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTimeOffset.UtcNow });
    }
}

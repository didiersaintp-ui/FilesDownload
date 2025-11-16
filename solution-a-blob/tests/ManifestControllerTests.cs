using DeviceManifest.Api.Controllers;
using DeviceManifest.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace DeviceManifest.Api.Tests;

public class ManifestControllerTests
{
    private readonly Mock<BlobStorageService> _mockStorageService;
    private readonly Mock<ILogger<ManifestController>> _mockLogger;
    private readonly ManifestController _controller;

    public ManifestControllerTests()
    {
        _mockStorageService = new Mock<BlobStorageService>();
        _mockLogger = new Mock<ILogger<ManifestController>>();
        _controller = new ManifestController(_mockStorageService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetManifest_WithValidCeb_ReturnsOk()
    {
        // Arrange
        var ceb = "CEB0000000000000001";
        var manifest = new DeviceManifest.Shared.DeviceManifest
        {
            Ceb = ceb,
            GeneratedAt = DateTimeOffset.UtcNow,
            Files = new List<DeviceManifest.Shared.FileItem>()
        };

        _mockStorageService
            .Setup(x => x.GetManifestAsync(ceb, 10, default))
            .ReturnsAsync(manifest);

        // Act
        var result = await _controller.GetManifest(ceb);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(manifest);
    }

    [Theory]
    [InlineData("")] // Empty CEB
    [InlineData("ABC")] // Too short
    [InlineData("12345678901234567890")] // Too long
    public async Task GetManifest_WithInvalidCeb_ReturnsBadRequest(string invalidCeb)
    {
        // Act
        var result = await _controller.GetManifest(invalidCeb);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Theory]
    [InlineData(3)] // Too low
    [InlineData(100)] // Too high
    public async Task GetManifest_WithInvalidTtl_ReturnsBadRequest(int invalidTtl)
    {
        // Arrange
        var ceb = "CEB0000000000000001";

        // Act
        var result = await _controller.GetManifest(ceb, invalidTtl);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public void Health_ReturnsOk()
    {
        // Act
        var result = _controller.Health();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }
}

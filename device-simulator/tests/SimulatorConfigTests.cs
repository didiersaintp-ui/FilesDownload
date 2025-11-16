using Xunit;
using FluentAssertions;

namespace DeviceSimulator.Tests;

public class SimulatorConfigTests
{
    [Fact]
    public void SimulatorConfig_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var config = new SimulatorConfig();

        // Assert
        config.ApiBaseUrl.Should().Be("http://localhost:5000");
        config.DeviceCount.Should().Be(100);
        config.MaxParallelism.Should().Be(10);
        config.TtlMinutes.Should().Be(10);
        config.DownloadFiles.Should().BeFalse();
        config.UseConditionalGet.Should().BeTrue();
    }

    [Fact]
    public void SimulatorConfig_CustomValues_AreSet()
    {
        // Arrange & Act
        var config = new SimulatorConfig
        {
            ApiBaseUrl = "http://custom-api:5000",
            DeviceCount = 1000,
            MaxParallelism = 50,
            TtlMinutes = 15,
            DownloadFiles = true,
            UseConditionalGet = false
        };

        // Assert
        config.ApiBaseUrl.Should().Be("http://custom-api:5000");
        config.DeviceCount.Should().Be(1000);
        config.MaxParallelism.Should().Be(50);
        config.TtlMinutes.Should().Be(15);
        config.DownloadFiles.Should().BeTrue();
        config.UseConditionalGet.Should().BeFalse();
    }
}

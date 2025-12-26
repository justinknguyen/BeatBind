using BeatBind.Application.Services;
using BeatBind.Core.Entities;
using BeatBind.Core.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BeatBind.Tests.Application.Services
{
    public class ConfigurationApplicationServiceTests
    {
        private readonly Mock<IConfigurationService> _mockConfigService;
        private readonly Mock<ILogger<ConfigurationApplicationService>> _mockLogger;
        private readonly ConfigurationApplicationService _service;

        public ConfigurationApplicationServiceTests()
        {
            _mockConfigService = new Mock<IConfigurationService>();
            _mockLogger = new Mock<ILogger<ConfigurationApplicationService>>();
            
            _service = new ConfigurationApplicationService(
                _mockConfigService.Object,
                _mockLogger.Object);
        }

        [Fact]
        public void SaveConfiguration_WithValidConfiguration_ShouldReturnSuccess()
        {
            // Arrange
            var config = new ApplicationConfiguration
            {
                ClientId = "test-id",
                ClientSecret = "test-secret"
            };

            // Act
            var result = _service.SaveConfiguration(config);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockConfigService.Verify(x => x.SaveConfiguration(config), Times.Once);
        }

        [Fact]
        public void SaveConfiguration_WhenExceptionThrown_ShouldReturnFailure()
        {
            // Arrange
            var config = new ApplicationConfiguration();
            _mockConfigService.Setup(x => x.SaveConfiguration(config)).Throws(new Exception("Save error"));

            // Act
            var result = _service.SaveConfiguration(config);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("save configuration");
        }
    }
}

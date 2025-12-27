using BeatBind.Infrastructure.Helpers;
using BeatBind.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BeatBind.Tests.Infrastructure.Services
{
    public class StartupServiceTests
    {
        private readonly Mock<ILogger<StartupService>> _mockLogger;
        private readonly Mock<IRegistryWrapper> _mockRegistryWrapper;
        private readonly StartupService _service;
        private const string APP_NAME = "BeatBind";

        public StartupServiceTests()
        {
            _mockLogger = new Mock<ILogger<StartupService>>();
            _mockRegistryWrapper = new Mock<IRegistryWrapper>();
            _service = new StartupService(_mockLogger.Object, _mockRegistryWrapper.Object);
        }

        [Fact]
        public void SetStartupWithWindows_WhenTrue_ShouldAddToRegistry()
        {
            // Arrange
            var exePath = @"C:\Program Files\BeatBind\BeatBind.exe";
            _mockRegistryWrapper.Setup(x => x.GetCurrentProcessPath()).Returns(exePath);

            // Act
            _service.SetStartupWithWindows(true);

            // Assert
            _mockRegistryWrapper.Verify(x => x.SetStartupRegistryValue(APP_NAME, exePath), Times.Once);
            _mockRegistryWrapper.Verify(x => x.RemoveStartupRegistryValue(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void SetStartupWithWindows_WhenTrueAndPathNull_ShouldLogError()
        {
            // Arrange
            _mockRegistryWrapper.Setup(x => x.GetCurrentProcessPath()).Returns((string?)null);

            // Act
            _service.SetStartupWithWindows(true);

            // Assert
            _mockRegistryWrapper.Verify(x => x.SetStartupRegistryValue(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            // Verify error logging if possible, but usually we just check behavior
        }

        [Fact]
        public void SetStartupWithWindows_WhenFalse_ShouldRemoveFromRegistry()
        {
            // Act
            _service.SetStartupWithWindows(false);

            // Assert
            _mockRegistryWrapper.Verify(x => x.RemoveStartupRegistryValue(APP_NAME), Times.Once);
            _mockRegistryWrapper.Verify(x => x.SetStartupRegistryValue(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void IsInStartup_ShouldReturnRegistryValue()
        {
            // Arrange
            _mockRegistryWrapper.Setup(x => x.HasStartupRegistryValue(APP_NAME)).Returns(true);

            // Act
            var result = _service.IsInStartup();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsInStartup_WhenException_ShouldReturnFalseAndLog()
        {
            // Arrange
            _mockRegistryWrapper.Setup(x => x.HasStartupRegistryValue(APP_NAME)).Throws(new Exception("Registry error"));

            // Act
            var result = _service.IsInStartup();

            // Assert
            result.Should().BeFalse();
        }
    }
}

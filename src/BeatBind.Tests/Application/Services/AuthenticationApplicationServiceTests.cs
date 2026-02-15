using BeatBind.Application.Services;
using BeatBind.Core.Entities;
using BeatBind.Core.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BeatBind.Tests.Application.Services
{
    public class AuthenticationApplicationServiceTests
    {
        private readonly Mock<IAuthenticationService> _mockAuthService;
        private readonly Mock<IConfigurationService> _mockConfigService;
        private readonly Mock<ILogger<AuthenticationApplicationService>> _mockLogger;
        private readonly AuthenticationApplicationService _service;

        public AuthenticationApplicationServiceTests()
        {
            _mockAuthService = new Mock<IAuthenticationService>();
            _mockConfigService = new Mock<IConfigurationService>();
            _mockLogger = new Mock<ILogger<AuthenticationApplicationService>>();

            _service = new AuthenticationApplicationService(
                _mockAuthService.Object,
                _mockConfigService.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task AuthenticateUserAsync_WithValidCredentials_ShouldReturnSuccess()
        {
            // Arrange
            var config = new ApplicationConfiguration
            {
                ClientId = "client-id",
                ClientSecret = "client-secret"
            };
            _mockConfigService.Setup(x => x.GetConfiguration()).Returns(config);
            _mockAuthService.Setup(x => x.AuthenticateAsync()).ReturnsAsync(new AuthenticationResult { Success = true });

            // Act
            var result = await _service.AuthenticateUserAsync();

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task AuthenticateUserAsync_WithMissingClientId_ShouldReturnFailure()
        {
            // Arrange
            var config = new ApplicationConfiguration
            {
                ClientId = "",
                ClientSecret = "client-secret"
            };
            _mockConfigService.Setup(x => x.GetConfiguration()).Returns(config);

            // Act
            var result = await _service.AuthenticateUserAsync();

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("not configured");
        }

        [Fact]
        public async Task AuthenticateUserAsync_WithMissingClientSecret_ShouldReturnFailure()
        {
            // Arrange
            var config = new ApplicationConfiguration
            {
                ClientId = "client-id",
                ClientSecret = ""
            };
            _mockConfigService.Setup(x => x.GetConfiguration()).Returns(config);

            // Act
            var result = await _service.AuthenticateUserAsync();

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("not configured");
        }

        [Fact]
        public async Task AuthenticateUserAsync_WhenAuthenticationFails_ShouldReturnFailure()
        {
            // Arrange
            var config = new ApplicationConfiguration
            {
                ClientId = "client-id",
                ClientSecret = "client-secret"
            };
            _mockConfigService.Setup(x => x.GetConfiguration()).Returns(config);
            _mockAuthService.Setup(x => x.AuthenticateAsync()).ReturnsAsync(new AuthenticationResult
            {
                Success = false,
                Error = "Authentication failed"
            });

            // Act
            var result = await _service.AuthenticateUserAsync();

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("failed");
        }

        [Fact]
        public async Task AuthenticateUserAsync_WhenExceptionThrown_ShouldReturnFailure()
        {
            // Arrange
            var config = new ApplicationConfiguration
            {
                ClientId = "client-id",
                ClientSecret = "client-secret"
            };
            _mockConfigService.Setup(x => x.GetConfiguration()).Returns(config);
            _mockAuthService.Setup(x => x.AuthenticateAsync()).ThrowsAsync(new Exception("Network error"));

            // Act
            var result = await _service.AuthenticateUserAsync();

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("error");
        }

        [Fact]
        public void UpdateClientCredentialsAsync_WithValidCredentials_ShouldReturnSuccess()
        {
            // Arrange
            var config = new ApplicationConfiguration();
            _mockConfigService.Setup(x => x.GetConfiguration()).Returns(config);

            // Act
            var result = _service.UpdateClientCredentials("new-client-id", "new-secret");

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockConfigService.Verify(x => x.SaveConfiguration(It.IsAny<ApplicationConfiguration>()), Times.Once);
        }

        [Fact]
        public void UpdateClientCredentialsAsync_WithEmptyClientId_ShouldReturnFailure()
        {
            // Act
            var result = _service.UpdateClientCredentials("", "secret");

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("required");
        }

        [Fact]
        public void UpdateClientCredentialsAsync_WithEmptyClientSecret_ShouldReturnFailure()
        {
            // Act
            var result = _service.UpdateClientCredentials("client-id", "");

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("required");
        }

        [Fact]
        public void UpdateClientCredentialsAsync_WhenExceptionThrown_ShouldReturnFailure()
        {
            // Arrange
            _mockConfigService.Setup(x => x.GetConfiguration()).Throws(new Exception("Config error"));

            // Act
            var result = _service.UpdateClientCredentials("client-id", "secret");

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("error");
        }
    }
}

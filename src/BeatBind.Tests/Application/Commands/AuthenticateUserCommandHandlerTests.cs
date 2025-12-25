using BeatBind.Application.Commands;
using BeatBind.Core.Entities;
using BeatBind.Core.Interfaces;
using FluentAssertions;
using Moq;

namespace BeatBind.Tests.Application.Commands
{
    public class AuthenticateUserCommandHandlerTests
    {
        private readonly Mock<ISpotifyService> _spotifyServiceMock;
        private readonly Mock<IConfigurationService> _configurationServiceMock;
        private readonly AuthenticateUserCommandHandler _handler;

        public AuthenticateUserCommandHandlerTests()
        {
            _spotifyServiceMock = new Mock<ISpotifyService>();
            _configurationServiceMock = new Mock<IConfigurationService>();
            _handler = new AuthenticateUserCommandHandler(
                _spotifyServiceMock.Object,
                _configurationServiceMock.Object);
        }

        [Fact]
        public async Task Handle_WithValidCredentials_ShouldReturnSuccess()
        {
            // Arrange
            var config = new ApplicationConfiguration
            {
                ClientId = "test-client-id",
                ClientSecret = "test-client-secret"
            };

            _configurationServiceMock
                .Setup(x => x.GetConfiguration())
                .Returns(config);

            _spotifyServiceMock
                .Setup(x => x.AuthenticateAsync())
                .ReturnsAsync(true);

            var command = new AuthenticateUserCommand();

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _spotifyServiceMock.Verify(x => x.AuthenticateAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_WithMissingClientId_ShouldReturnFailure()
        {
            // Arrange
            var config = new ApplicationConfiguration
            {
                ClientId = "",
                ClientSecret = "test-client-secret"
            };

            _configurationServiceMock
                .Setup(x => x.GetConfiguration())
                .Returns(config);

            var command = new AuthenticateUserCommand();

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Contain("credentials");
            _spotifyServiceMock.Verify(x => x.AuthenticateAsync(), Times.Never);
        }

        [Fact]
        public async Task Handle_WithMissingClientSecret_ShouldReturnFailure()
        {
            // Arrange
            var config = new ApplicationConfiguration
            {
                ClientId = "test-client-id",
                ClientSecret = ""
            };

            _configurationServiceMock
                .Setup(x => x.GetConfiguration())
                .Returns(config);

            var command = new AuthenticateUserCommand();

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Contain("credentials");
            _spotifyServiceMock.Verify(x => x.AuthenticateAsync(), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenAuthenticationFails_ShouldReturnFailure()
        {
            // Arrange
            var config = new ApplicationConfiguration
            {
                ClientId = "test-client-id",
                ClientSecret = "test-client-secret"
            };

            _configurationServiceMock
                .Setup(x => x.GetConfiguration())
                .Returns(config);

            _spotifyServiceMock
                .Setup(x => x.AuthenticateAsync())
                .ReturnsAsync(false);

            var command = new AuthenticateUserCommand();

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Contain("failed");
            _spotifyServiceMock.Verify(x => x.AuthenticateAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenSpotifyServiceThrows_ShouldThrowException()
        {
            // Arrange
            var config = new ApplicationConfiguration
            {
                ClientId = "test-client-id",
                ClientSecret = "test-client-secret"
            };

            _configurationServiceMock
                .Setup(x => x.GetConfiguration())
                .Returns(config);

            _spotifyServiceMock
                .Setup(x => x.AuthenticateAsync())
                .ThrowsAsync(new Exception("Spotify error"));

            var command = new AuthenticateUserCommand();

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(async () =>
                await _handler.Handle(command, CancellationToken.None));
        }
    }
}

using BeatBind.Application.Commands.UpdateClientCredentials;
using BeatBind.Core.Interfaces;
using FluentAssertions;
using Moq;

namespace BeatBind.Tests.Application.Commands
{
    public class UpdateClientCredentialsCommandHandlerTests
    {
        private readonly Mock<IConfigurationService> _configurationServiceMock;
        private readonly UpdateClientCredentialsCommandHandler _handler;

        public UpdateClientCredentialsCommandHandlerTests()
        {
            _configurationServiceMock = new Mock<IConfigurationService>();
            _handler = new UpdateClientCredentialsCommandHandler(_configurationServiceMock.Object);
        }

        [Fact]
        public async Task Handle_WithValidCredentials_ShouldReturnSuccess()
        {
            // Arrange
            var command = new UpdateClientCredentialsCommand("client-id", "client-secret");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _configurationServiceMock.Verify(
                x => x.UpdateClientCredentials("client-id", "client-secret"),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WhenServiceThrows_ShouldThrowException()
        {
            // Arrange
            var command = new UpdateClientCredentialsCommand("client-id", "client-secret");

            _configurationServiceMock
                .Setup(x => x.UpdateClientCredentials(It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new Exception("Update failed"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(async () =>
                await _handler.Handle(command, CancellationToken.None));
        }
    }
}

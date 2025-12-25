using BeatBind.Application.Configuration.Commands.SaveConfiguration;
using BeatBind.Domain.Entities;
using BeatBind.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace BeatBind.Tests.Application.Commands
{
    public class SaveConfigurationCommandHandlerTests
    {
        private readonly Mock<IConfigurationService> _configurationServiceMock;
        private readonly SaveConfigurationCommandHandler _handler;

        public SaveConfigurationCommandHandlerTests()
        {
            _configurationServiceMock = new Mock<IConfigurationService>();
            _handler = new SaveConfigurationCommandHandler(_configurationServiceMock.Object);
        }

        [Fact]
        public async Task Handle_WithValidConfiguration_ShouldReturnSuccess()
        {
            // Arrange
            var config = new ApplicationConfiguration
            {
                ClientId = "test-id",
                ClientSecret = "test-secret",
                DarkMode = true
            };

            var command = new SaveConfigurationCommand(config);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _configurationServiceMock.Verify(x => x.SaveConfiguration(config), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenServiceThrows_ShouldThrowException()
        {
            // Arrange
            var config = new ApplicationConfiguration();
            var command = new SaveConfigurationCommand(config);

            _configurationServiceMock
                .Setup(x => x.SaveConfiguration(It.IsAny<ApplicationConfiguration>()))
                .Throws(new Exception("Save failed"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(async () =>
                await _handler.Handle(command, CancellationToken.None));
        }
    }
}

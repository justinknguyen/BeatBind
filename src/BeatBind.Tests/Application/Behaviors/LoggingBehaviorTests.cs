using BeatBind.Application.Behaviors;
using BeatBind.Core.Common;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;

namespace BeatBind.Tests.Application.Behaviors
{
    public class LoggingBehaviorTests
    {
        private readonly Mock<ILogger<LoggingBehavior<TestRequestForLogging, Result>>> _loggerMock;
        private readonly LoggingBehavior<TestRequestForLogging, Result> _behavior;
        private readonly Mock<RequestHandlerDelegate<Result>> _nextMock;

        public LoggingBehaviorTests()
        {
            _loggerMock = new Mock<ILogger<LoggingBehavior<TestRequestForLogging, Result>>>();
            _behavior = new LoggingBehavior<TestRequestForLogging, Result>(_loggerMock.Object);
            _nextMock = new Mock<RequestHandlerDelegate<Result>>();
        }

        [Fact]
        public async Task Handle_WithSuccessfulRequest_ShouldLogInfoMessages()
        {
            // Arrange
            var request = new TestRequestForLogging();
            var expectedResult = Result.Success();
            _nextMock.Setup(x => x()).ReturnsAsync(expectedResult);

            // Act
            var result = await _behavior.Handle(request, _nextMock.Object, CancellationToken.None);

            // Assert
            result.Should().Be(expectedResult);

            // Verify processing log
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Processing")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            // Verify completed log
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Completed")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WithFailedRequest_ShouldLogWarning()
        {
            // Arrange
            var request = new TestRequestForLogging();
            var expectedResult = Result.Failure("Test error");
            _nextMock.Setup(x => x()).ReturnsAsync(expectedResult);

            // Act
            var result = await _behavior.Handle(request, _nextMock.Object, CancellationToken.None);

            // Assert
            result.Should().Be(expectedResult);

            // Verify warning log
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("failed") && v.ToString()!.Contains("Test error")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WhenExceptionThrown_ShouldLogError()
        {
            // Arrange
            var request = new TestRequestForLogging();
            var exception = new Exception("Test exception");
            _nextMock.Setup(x => x()).ThrowsAsync(exception);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(async () =>
                await _behavior.Handle(request, _nextMock.Object, CancellationToken.None));

            // Verify error log
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("failed with exception")),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }

    public class TestRequestForLogging : IRequest<Result> { }
}

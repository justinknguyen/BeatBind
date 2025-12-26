using BeatBind.Core.Common;
using FluentAssertions;

namespace BeatBind.Tests.Core
{
    public class ResultTests
    {
        [Fact]
        public void Success_ShouldCreateSuccessfulResult()
        {
            // Act
            var result = Result.Success();

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.IsFailure.Should().BeFalse();
            result.Error.Should().BeEmpty();
        }

        [Fact]
        public void Failure_ShouldCreateFailedResult()
        {
            // Arrange
            var errorMessage = "Test error";

            // Act
            var result = Result.Failure(errorMessage);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(errorMessage);
        }

        [Fact]
        public void Success_Generic_ShouldCreateSuccessfulResultWithValue()
        {
            // Arrange
            var value = "test value";

            // Act
            var result = Result.Success(value);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.IsFailure.Should().BeFalse();
            result.Value.Should().Be(value);
            result.Error.Should().BeEmpty();
        }

        [Fact]
        public void Failure_Generic_ShouldCreateFailedResultWithDefaultValue()
        {
            // Arrange
            var errorMessage = "Test error";

            // Act
            var result = Result.Failure<string>(errorMessage);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(errorMessage);
            result.Value.Should().BeNull();
        }

        [Fact]
        public void IsFailure_ShouldBeOppositeOfIsSuccess()
        {
            // Arrange
            var successResult = Result.Success();
            var failureResult = Result.Failure("error");

            // Assert
            successResult.IsFailure.Should().Be(!successResult.IsSuccess);
            failureResult.IsFailure.Should().Be(!failureResult.IsSuccess);
        }
    }
}

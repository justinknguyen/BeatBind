using BeatBind.Application.Behaviors;
using BeatBind.Domain.Common;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Moq;
using Xunit;

namespace BeatBind.Tests.Application.Behaviors
{
    public class ValidationBehaviorTests
    {
        private readonly Mock<IValidator<TestRequestForValidation>> _validatorMock;
        private readonly ValidationBehavior<TestRequestForValidation, Result> _behavior;
        private readonly Mock<RequestHandlerDelegate<Result>> _nextMock;

        public ValidationBehaviorTests()
        {
            _validatorMock = new Mock<IValidator<TestRequestForValidation>>();
            _behavior = new ValidationBehavior<TestRequestForValidation, Result>(new[] { _validatorMock.Object });
            _nextMock = new Mock<RequestHandlerDelegate<Result>>();
        }

        [Fact]
        public async Task Handle_WithNoValidators_ShouldCallNext()
        {
            // Arrange
            var behaviorNoValidators = new ValidationBehavior<TestRequestForValidation, Result>(Array.Empty<IValidator<TestRequestForValidation>>());
            var request = new TestRequestForValidation();
            var expectedResult = Result.Success();
            _nextMock.Setup(x => x()).ReturnsAsync(expectedResult);

            // Act
            var result = await behaviorNoValidators.Handle(request, _nextMock.Object, CancellationToken.None);

            // Assert
            result.Should().Be(expectedResult);
            _nextMock.Verify(x => x(), Times.Once);
        }

        [Fact]
        public async Task Handle_WithValidRequest_ShouldCallNext()
        {
            // Arrange
            var request = new TestRequestForValidation();
            var expectedResult = Result.Success();
            
            _validatorMock
                .Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequestForValidation>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _nextMock.Setup(x => x()).ReturnsAsync(expectedResult);

            // Act
            var result = await _behavior.Handle(request, _nextMock.Object, CancellationToken.None);

            // Assert
            result.Should().Be(expectedResult);
            _nextMock.Verify(x => x(), Times.Once);
        }

        [Fact]
        public async Task Handle_WithInvalidRequest_ShouldReturnFailure()
        {
            // Arrange
            var request = new TestRequestForValidation();
            var validationFailures = new List<ValidationFailure>
            {
                new ValidationFailure("Property1", "Error message 1"),
                new ValidationFailure("Property2", "Error message 2")
            };

            _validatorMock
                .Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequestForValidation>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(validationFailures));

            // Act
            var result = await _behavior.Handle(request, _nextMock.Object, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Contain("Error message 1");
            result.Error.Should().Contain("Error message 2");
            _nextMock.Verify(x => x(), Times.Never);
        }

        [Fact]
        public async Task Handle_WithMultipleErrors_ShouldCombineErrorMessages()
        {
            // Arrange
            var request = new TestRequestForValidation();
            var validationFailures = new List<ValidationFailure>
            {
                new ValidationFailure("Field1", "First error"),
                new ValidationFailure("Field2", "Second error"),
                new ValidationFailure("Field3", "Third error")
            };

            _validatorMock
                .Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequestForValidation>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(validationFailures));

            // Act
            var result = await _behavior.Handle(request, _nextMock.Object, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be("First error; Second error; Third error");
            _nextMock.Verify(x => x(), Times.Never);
        }
    }

    public class TestRequestForValidation : IRequest<Result> { }
}

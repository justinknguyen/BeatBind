using BeatBind.Application.Configuration.Commands.UpdateClientCredentials;
using FluentValidation.TestHelper;

namespace BeatBind.Tests.Application.Validators
{
    public class UpdateClientCredentialsCommandValidatorTests
    {
        private readonly UpdateClientCredentialsCommandValidator _validator;

        public UpdateClientCredentialsCommandValidatorTests()
        {
            _validator = new UpdateClientCredentialsCommandValidator();
        }

        [Fact]
        public void Validate_WithValidCredentials_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new UpdateClientCredentialsCommand("client-id", "client-secret");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_WithEmptyClientId_ShouldHaveError()
        {
            // Arrange
            var command = new UpdateClientCredentialsCommand("", "client-secret");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.ClientId)
                .WithErrorMessage("Client ID is required.");
        }

        [Fact]
        public void Validate_WithEmptyClientSecret_ShouldHaveError()
        {
            // Arrange
            var command = new UpdateClientCredentialsCommand("client-id", "");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.ClientSecret)
                .WithErrorMessage("Client Secret is required.");
        }

        [Fact]
        public void Validate_WithBothEmpty_ShouldHaveBothErrors()
        {
            // Arrange
            var command = new UpdateClientCredentialsCommand("", "");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.ClientId);
            result.ShouldHaveValidationErrorFor(x => x.ClientSecret);
        }
    }
}

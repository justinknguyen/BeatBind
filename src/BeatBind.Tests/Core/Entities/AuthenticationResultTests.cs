using BeatBind.Core.Entities;
using FluentAssertions;

namespace BeatBind.Tests.Core.Entities
{
    public class AuthenticationResultTests
    {
        [Fact]
        public void AuthenticationResult_ShouldInitializeWithDefaults()
        {
            // Act
            var result = new AuthenticationResult();

            // Assert
            result.Success.Should().BeFalse();
            result.AccessToken.Should().BeEmpty();
            result.RefreshToken.Should().BeEmpty();
            result.ExpiresIn.Should().Be(0);
            result.Error.Should().BeEmpty();
        }

        [Fact]
        public void AuthenticationResult_ShouldSetProperties()
        {
            // Act
            var result = new AuthenticationResult
            {
                Success = true,
                AccessToken = "access-token-123",
                RefreshToken = "refresh-token-456",
                ExpiresIn = 3600,
                Error = string.Empty
            };

            // Assert
            result.Success.Should().BeTrue();
            result.AccessToken.Should().Be("access-token-123");
            result.RefreshToken.Should().Be("refresh-token-456");
            result.ExpiresIn.Should().Be(3600);
            result.Error.Should().BeEmpty();
        }

        [Fact]
        public void AuthenticationResult_ExpiresAt_ShouldBeSetCorrectly()
        {
            // Arrange
            var beforeCreation = DateTime.UtcNow;
            var expiresAt = DateTime.UtcNow.AddHours(1);

            // Act
            var result = new AuthenticationResult
            {
                Success = true,
                AccessToken = "token",
                ExpiresIn = 3600,
                ExpiresAt = expiresAt
            };

            // Assert
            result.ExpiresAt.Should().BeAfter(beforeCreation);
            result.ExpiresAt.Should().BeCloseTo(expiresAt, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void AuthenticationResult_WhenFailed_ShouldHaveError()
        {
            // Act
            var result = new AuthenticationResult
            {
                Success = false,
                Error = "Authentication failed"
            };

            // Assert
            result.Success.Should().BeFalse();
            result.Error.Should().Be("Authentication failed");
        }

        [Fact]
        public void AuthenticationResult_WhenSuccessful_ShouldHaveTokens()
        {
            // Act
            var result = new AuthenticationResult
            {
                Success = true,
                AccessToken = "valid-access-token",
                RefreshToken = "valid-refresh-token",
                ExpiresIn = 3600,
                ExpiresAt = DateTime.UtcNow.AddSeconds(3600)
            };

            // Assert
            result.Success.Should().BeTrue();
            result.AccessToken.Should().NotBeEmpty();
            result.RefreshToken.Should().NotBeEmpty();
            result.ExpiresIn.Should().BeGreaterThan(0);
        }
    }
}

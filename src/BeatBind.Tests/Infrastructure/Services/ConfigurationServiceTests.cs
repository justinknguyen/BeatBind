using BeatBind.Core.Entities;
using BeatBind.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BeatBind.Tests.Infrastructure.Services
{
    public class ConfigurationServiceTests : IDisposable
    {
        private readonly Mock<ILogger<ConfigurationService>> _mockLogger;
        private readonly string _testConfigPath;
        private readonly ConfigurationService _service;

        public ConfigurationServiceTests()
        {
            _mockLogger = new Mock<ILogger<ConfigurationService>>();
            
            // Use a temporary directory for tests
            _testConfigPath = Path.Combine(Path.GetTempPath(), "BeatBindTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testConfigPath);
            
            var configFilePath = Path.Combine(_testConfigPath, "config.json");
            _service = new ConfigurationService(_mockLogger.Object, configFilePath);
        }

        [Fact]
        public void GetConfiguration_ShouldReturnConfiguration()
        {
            // Act
            var config = _service.GetConfiguration();

            // Assert
            config.Should().NotBeNull();
            config.Should().BeOfType<ApplicationConfiguration>();
        }

        [Fact]
        public void SaveConfiguration_ShouldPersistConfiguration()
        {
            // Arrange
            var config = new ApplicationConfiguration
            {
                ClientId = "test-client-id",
                ClientSecret = "test-secret",
                VolumeSteps = 15
            };

            // Act
            _service.SaveConfiguration(config);
            var loadedConfig = _service.GetConfiguration();

            // Assert
            loadedConfig.ClientId.Should().Be("test-client-id");
            loadedConfig.ClientSecret.Should().Be("test-secret");
            loadedConfig.VolumeSteps.Should().Be(15);
        }

        [Fact]
        public void UpdateClientCredentials_ShouldUpdateAndSave()
        {
            // Act
            _service.UpdateClientCredentials("new-client-id", "new-secret");
            var config = _service.GetConfiguration();

            // Assert
            config.ClientId.Should().Be("new-client-id");
            config.ClientSecret.Should().Be("new-secret");
        }

        [Fact]
        public void AddHotkey_ShouldAssignIdAndAddToList()
        {
            // Arrange
            var hotkey = new Hotkey
            {
                Action = HotkeyAction.PlayPause,
                IsEnabled = true,
                KeyCode = 0xB3
            };

            // Act
            _service.AddHotkey(hotkey);
            var hotkeys = _service.GetHotkeys();

            // Assert
            hotkey.Id.Should().BeGreaterThan(0);
            hotkeys.Should().Contain(h => h.Id == hotkey.Id);
        }

        [Fact]
        public void AddHotkey_ShouldIncrementIds()
        {
            // Arrange
            var hotkey1 = new Hotkey { Action = HotkeyAction.PlayPause, IsEnabled = true, KeyCode = 0xB3 };
            var hotkey2 = new Hotkey { Action = HotkeyAction.NextTrack, IsEnabled = true, KeyCode = 0xB0 };

            // Act
            _service.AddHotkey(hotkey1);
            _service.AddHotkey(hotkey2);

            // Assert
            hotkey2.Id.Should().Be(hotkey1.Id + 1);
        }

        [Fact]
        public void RemoveHotkey_ShouldRemoveFromList()
        {
            // Arrange
            var hotkey = new Hotkey { Action = HotkeyAction.PlayPause, IsEnabled = true, KeyCode = 0xB3 };
            _service.AddHotkey(hotkey);
            var hotkeyId = hotkey.Id;

            // Act
            _service.RemoveHotkey(hotkeyId);
            var hotkeys = _service.GetHotkeys();

            // Assert
            hotkeys.Should().NotContain(h => h.Id == hotkeyId);
        }

        [Fact]
        public void UpdateHotkey_ShouldModifyExistingHotkey()
        {
            // Arrange
            var hotkey = new Hotkey { Action = HotkeyAction.PlayPause, IsEnabled = true, KeyCode = 0xB3 };
            _service.AddHotkey(hotkey);
            var hotkeyId = hotkey.Id;

            // Modify the hotkey
            hotkey.Action = HotkeyAction.NextTrack;
            hotkey.KeyCode = 0xB0;

            // Act
            _service.UpdateHotkey(hotkey);
            var hotkeys = _service.GetHotkeys();
            var updatedHotkey = hotkeys.First(h => h.Id == hotkeyId);

            // Assert
            updatedHotkey.Action.Should().Be(HotkeyAction.NextTrack);
            updatedHotkey.KeyCode.Should().Be(0xB0);
        }

        [Fact]
        public void GetHotkeys_ShouldReturnAllHotkeys()
        {
            // Arrange - Start with empty config to ensure clean state
            _service.SaveConfiguration(new ApplicationConfiguration());
            
            var hotkey1 = new Hotkey { Action = HotkeyAction.PlayPause, IsEnabled = true, KeyCode = 0xB3 };
            var hotkey2 = new Hotkey { Action = HotkeyAction.NextTrack, IsEnabled = true, KeyCode = 0xB0 };
            _service.AddHotkey(hotkey1);
            _service.AddHotkey(hotkey2);

            // Act
            var hotkeys = _service.GetHotkeys();

            // Assert
            hotkeys.Should().HaveCount(2);
            hotkeys.Should().Contain(h => h.Action == HotkeyAction.PlayPause);
            hotkeys.Should().Contain(h => h.Action == HotkeyAction.NextTrack);
        }

        [Fact]
        public void RemoveHotkey_WithNonExistentId_ShouldNotThrow()
        {
            // Arrange
            var nonExistentId = 9999;

            // Act
            var act = () => _service.RemoveHotkey(nonExistentId);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void UpdateHotkey_WithNonExistentId_ShouldNotThrow()
        {
            // Arrange
            var hotkey = new Hotkey { Id = 9999, Action = HotkeyAction.PlayPause, IsEnabled = true, KeyCode = 0xB3 };

            // Act
            var act = () => _service.UpdateHotkey(hotkey);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void SaveConfiguration_WhenDirectoryCreationFails_ShouldThrow()
        {
            // Arrange - This test verifies exception handling is present
            // We'll use a directory with invalid characters to force an exception
            var invalidPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), new string(Path.GetInvalidFileNameChars()));
            var config = new ApplicationConfiguration { ClientId = "test" };

            // Act & Assert
            var act = () => new ConfigurationService(_mockLogger.Object, invalidPath);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void LoadConfiguration_WhenFileDoesNotExist_ShouldCreateDefaultConfig()
        {
            // Arrange - Create a new service with non-existent file
            var newConfigPath = Path.Combine(_testConfigPath, "new_config.json");

            // Act
            var newService = new ConfigurationService(_mockLogger.Object, newConfigPath);
            var config = newService.GetConfiguration();

            // Assert
            config.Should().NotBeNull();
            File.Exists(newConfigPath).Should().BeTrue();
        }

        [Fact]
        public void LoadConfiguration_WithInvalidJson_ShouldUseDefaults()
        {
            // Arrange - Create file with invalid JSON
            var invalidJsonPath = Path.Combine(_testConfigPath, "invalid_config.json");
            Directory.CreateDirectory(Path.GetDirectoryName(invalidJsonPath)!);
            File.WriteAllText(invalidJsonPath, "{ invalid json }");

            // Act
            var newService = new ConfigurationService(_mockLogger.Object, invalidJsonPath);
            var config = newService.GetConfiguration();

            // Assert
            config.Should().NotBeNull();
            config.Should().BeOfType<ApplicationConfiguration>();
        }

        [Fact]
        public void AddHotkey_WithExistingId_ShouldUseExistingId()
        {
            // Arrange
            var hotkey = new Hotkey { Id = 42, Action = HotkeyAction.PlayPause, IsEnabled = true, KeyCode = 0xB3 };

            // Act
            _service.AddHotkey(hotkey);
            var hotkeys = _service.GetHotkeys();

            // Assert
            hotkey.Id.Should().Be(42);
            hotkeys.Should().Contain(h => h.Id == 42);
        }

        [Fact]
        public void Constructor_WithDefaultConstructor_ShouldUseAppDataPath()
        {
            // Act
            var service = new ConfigurationService(_mockLogger.Object);
            var config = service.GetConfiguration();

            // Assert
            config.Should().NotBeNull();
        }

        public void Dispose()
        {
            // Cleanup test directory
            try
            {
                if (Directory.Exists(_testConfigPath))
                {
                    Directory.Delete(_testConfigPath, true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}

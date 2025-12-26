using System.Text.Json;
using BeatBind.Core.Entities;
using BeatBind.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace BeatBind.Infrastructure.Services
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly ILogger<ConfigurationService> _logger;
        private readonly string _configPath;
        private ApplicationConfiguration _config;

        public ConfigurationService(ILogger<ConfigurationService> logger)
            : this(logger, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BeatBind", "config.json"))
        {
        }

        public ConfigurationService(ILogger<ConfigurationService> logger, string configPath)
        {
            _logger = logger;
            _configPath = configPath;
            _config = new ApplicationConfiguration();

            EnsureConfigDirectoryExists();
            LoadConfiguration();
        }

        public ApplicationConfiguration GetConfiguration()
        {
            return _config;
        }

        public void SaveConfiguration(ApplicationConfiguration configuration)
        {
            try
            {
                _config = configuration;
                var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_configPath, json);
                _logger.LogInformation("Configuration saved to {ConfigPath}", _configPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save configuration");
                throw;
            }
        }

        public void UpdateClientCredentials(string clientId, string clientSecret)
        {
            _config.ClientId = clientId;
            _config.ClientSecret = clientSecret;
            SaveConfiguration(_config);
        }

        public void AddHotkey(Hotkey hotkey)
        {
            // Assign a new ID if not already set
            if (hotkey.Id == 0)
            {
                hotkey.Id = _config.Hotkeys.Count > 0 ? _config.Hotkeys.Max(h => h.Id) + 1 : 1;
            }

            _config.Hotkeys.Add(hotkey);
            SaveConfiguration(_config);
        }

        public void RemoveHotkey(int hotkeyId)
        {
            var hotkey = _config.Hotkeys.FirstOrDefault(h => h.Id == hotkeyId);
            if (hotkey != null)
            {
                _config.Hotkeys.Remove(hotkey);
                SaveConfiguration(_config);
            }
        }

        public void UpdateHotkey(Hotkey hotkey)
        {
            var existingHotkey = _config.Hotkeys.FirstOrDefault(h => h.Id == hotkey.Id);
            if (existingHotkey != null)
            {
                var index = _config.Hotkeys.IndexOf(existingHotkey);
                _config.Hotkeys[index] = hotkey;
                SaveConfiguration(_config);
            }
        }

        public List<Hotkey> GetHotkeys()
        {
            return _config.Hotkeys.ToList();
        }

        private void EnsureConfigDirectoryExists()
        {
            var directory = Path.GetDirectoryName(_configPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.LogInformation("Created configuration directory: {Directory}", directory);
            }
        }

        private void LoadConfiguration()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var json = File.ReadAllText(_configPath);
                    var config = JsonSerializer.Deserialize<ApplicationConfiguration>(json);
                    if (config != null)
                    {
                        _config = config;
                        _logger.LogInformation("Configuration loaded from {ConfigPath}", _configPath);
                    }
                }
                else
                {
                    _logger.LogInformation("No configuration file found, using defaults");
                    SaveConfiguration(_config); // Create default config file
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load configuration, using defaults");
                _config = new ApplicationConfiguration();
            }
        }
    }
}

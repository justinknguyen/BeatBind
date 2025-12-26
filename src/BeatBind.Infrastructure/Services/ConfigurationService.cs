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

        /// <summary>
        /// Initializes a new instance of the ConfigurationService class with default config path.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public ConfigurationService(ILogger<ConfigurationService> logger)
            : this(logger, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BeatBind", "config.json"))
        {
        }

        /// <summary>
        /// Initializes a new instance of the ConfigurationService class with a custom config path.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="configPath">The path to the configuration file.</param>
        public ConfigurationService(ILogger<ConfigurationService> logger, string configPath)
        {
            _logger = logger;
            _configPath = configPath;
            _config = new ApplicationConfiguration();

            EnsureConfigDirectoryExists();
            LoadConfiguration();
        }

        /// <summary>
        /// Gets the current application configuration.
        /// </summary>
        /// <returns>The application configuration object.</returns>
        public ApplicationConfiguration GetConfiguration()
        {
            return _config;
        }

        /// <summary>
        /// Saves the application configuration to disk.
        /// </summary>
        /// <param name="configuration">The configuration to save.</param>
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

        /// <summary>
        /// Updates the Spotify client credentials and saves the configuration.
        /// </summary>
        /// <param name="clientId">The Spotify application client ID.</param>
        /// <param name="clientSecret">The Spotify application client secret.</param>
        public void UpdateClientCredentials(string clientId, string clientSecret)
        {
            _config.ClientId = clientId;
            _config.ClientSecret = clientSecret;
            SaveConfiguration(_config);
        }

        /// <summary>
        /// Adds a new hotkey to the configuration. Assigns a new ID if not already set.
        /// </summary>
        /// <param name="hotkey">The hotkey to add.</param>
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

        /// <summary>
        /// Removes a hotkey from the configuration by its ID.
        /// </summary>
        /// <param name="hotkeyId">The ID of the hotkey to remove.</param>
        public void RemoveHotkey(int hotkeyId)
        {
            var hotkey = _config.Hotkeys.FirstOrDefault(h => h.Id == hotkeyId);
            if (hotkey != null)
            {
                _config.Hotkeys.Remove(hotkey);
                SaveConfiguration(_config);
            }
        }

        /// <summary>
        /// Updates an existing hotkey in the configuration.
        /// </summary>
        /// <param name="hotkey">The hotkey with updated values.</param>
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

        /// <summary>
        /// Gets all configured hotkeys.
        /// </summary>
        /// <returns>A list of all hotkeys.</returns>
        public List<Hotkey> GetHotkeys()
        {
            return _config.Hotkeys.ToList();
        }

        /// <summary>
        /// Ensures that the configuration directory exists, creating it if necessary.
        /// </summary>
        private void EnsureConfigDirectoryExists()
        {
            var directory = Path.GetDirectoryName(_configPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.LogInformation("Created configuration directory: {Directory}", directory);
            }
        }

        /// <summary>
        /// Loads the configuration from disk or creates a new default configuration if the file doesn't exist.
        /// </summary>
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

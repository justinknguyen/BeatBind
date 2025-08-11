using System;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace BeatBind
{
    public class ConfigurationManager
    {
        private readonly ILogger<ConfigurationManager> _logger;
        private readonly string _configPath;
        private BeatBindConfig _config;

        public ConfigurationManager()
        {
            _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<ConfigurationManager>();
            _configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BeatBind", "config.json");
            _config = new BeatBindConfig();
            
            EnsureConfigDirectoryExists();
            LoadConfiguration();
        }

        public BeatBindConfig Config => _config;

        public string ClientId
        {
            get => _config.ClientId ?? string.Empty;
            set
            {
                _config.ClientId = value;
                SaveConfiguration();
            }
        }

        public string ClientSecret
        {
            get => _config.ClientSecret ?? string.Empty;
            set
            {
                _config.ClientSecret = value;
                SaveConfiguration();
            }
        }

        public string RedirectUri
        {
            get => _config.RedirectUri ?? "http://127.0.0.1:8888/callback";
            set
            {
                _config.RedirectUri = value;
                SaveConfiguration();
            }
        }

        public HotkeyConfiguration Hotkeys => _config.Hotkeys;

        public void LoadConfiguration()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var json = File.ReadAllText(_configPath);
                    var config = JsonSerializer.Deserialize<BeatBindConfig>(json);
                    if (config != null)
                    {
                        _config = config;
                        _logger.LogInformation("Configuration loaded successfully");
                    }
                }
                else
                {
                    _logger.LogInformation("No configuration file found, using defaults");
                    SaveConfiguration(); // Create default config file
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading configuration");
                _config = new BeatBindConfig(); // Use defaults on error
            }
        }

        public void SaveConfiguration()
        {
            try
            {
                var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_configPath, json);
                _logger.LogInformation("Configuration saved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving configuration");
            }
        }

        private void EnsureConfigDirectoryExists()
        {
            var directory = Path.GetDirectoryName(_configPath);
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        public bool HasValidSpotifyCredentials()
        {
            return !string.IsNullOrEmpty(_config.ClientId) && !string.IsNullOrEmpty(_config.ClientSecret);
        }
    }

    public class BeatBindConfig
    {
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public string? RedirectUri { get; set; } = "http://127.0.0.1:8888/callback";
        public HotkeyConfiguration Hotkeys { get; set; } = new();
        public bool StartWithWindows { get; set; } = false;
        public bool MinimizeToTray { get; set; } = true;
        public int VolumeStep { get; set; } = 5;
        public int SeekStep { get; set; } = 10000; // 10 seconds in milliseconds
    }

    public class HotkeyConfiguration
    {
        public string PlayPause { get; set; } = "Ctrl+Alt+Space";
        public string NextTrack { get; set; } = "Ctrl+Alt+Right";
        public string PreviousTrack { get; set; } = "Ctrl+Alt+Left";
        public string VolumeUp { get; set; } = "Ctrl+Alt+Up";
        public string VolumeDown { get; set; } = "Ctrl+Alt+Down";
        public string Mute { get; set; } = "Ctrl+Alt+M";
        public string SeekForward { get; set; } = "Ctrl+Alt+F";
        public string SeekBackward { get; set; } = "Ctrl+Alt+B";
        public string SaveTrack { get; set; } = "Ctrl+Alt+S";
        public string RemoveTrack { get; set; } = "Ctrl+Alt+R";
    }
}

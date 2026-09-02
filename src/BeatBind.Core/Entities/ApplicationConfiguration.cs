namespace BeatBind.Core.Entities
{
    public class ApplicationConfiguration
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public int RedirectPort { get; set; } = 8888;
        public string RedirectUri { get; set; } = "http://127.0.0.1:8888/callback";
        public List<Hotkey> Hotkeys { get; set; } = new();
        public bool StartWithWindows { get; set; }
        public bool StartMinimized { get; set; } = true;
        public bool MinimizeToTray { get; set; } = true;

        // Audio Control Settings
        public bool PreviousTrackRewindToStart { get; set; } = true;
        public int VolumeSteps { get; set; } = 10;
        public int SeekMilliseconds { get; set; } = 10000;

        // Device Settings
        // The name is kept alongside the id so the UI can label a device that is
        // currently offline, and so playback can be re-targeted when Spotify
        // rotates the device id (common for Cast/AirPlay-style speakers).
        public string FavoriteDeviceId { get; set; } = string.Empty;
        public string FavoriteDeviceName { get; set; } = string.Empty;

        // Authentication Storage
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime TokenExpiresAt { get; set; }
    }
}

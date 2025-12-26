namespace BeatBind.Core.Entities
{
    public class ApplicationConfiguration
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public int RedirectPort { get; set; } = 8888;
        public string RedirectUri { get; set; } = "http://127.0.0.1:8888/callback";
        public List<Hotkey> Hotkeys { get; set; } = new();
        public bool StartMinimized { get; set; }
        public bool MinimizeToTray { get; set; } = true;
        public bool ShowNotifications { get; set; } = true;
        public int DefaultVolume { get; set; } = 50;
        
        // Audio Control Settings
        public bool PreviousTrackRewindToStart { get; set; } = true;
        public int VolumeSteps { get; set; } = 10;
        public int SeekMilliseconds { get; set; } = 10000;

        // Authentication Storage
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime TokenExpiresAt { get; set; }
    }
}

namespace BeatBind.Domain.Entities
{
    public class ApplicationConfiguration
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = "http://127.0.0.1:8888/callback";
        public List<Hotkey> Hotkeys { get; set; } = new();
        public bool StartMinimized { get; set; }
        public bool MinimizeToTray { get; set; } = true;
        public bool ShowNotifications { get; set; } = true;
        public int DefaultVolume { get; set; } = 50;
    }
}

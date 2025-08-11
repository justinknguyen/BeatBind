namespace BeatBind.Domain.Entities
{
    public class Hotkey
    {
        public int Id { get; set; }
        public int KeyCode { get; set; } // Store as int instead of Keys enum
        public ModifierKeys Modifiers { get; set; }
        public HotkeyAction Action { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
    }

    [Flags]
    public enum ModifierKeys
    {
        None = 0,
        Alt = 1,
        Control = 2,
        Shift = 4,
        Windows = 8
    }

    public enum HotkeyAction
    {
        PlayPause,
        NextTrack,
        PreviousTrack,
        VolumeUp,
        VolumeDown,
        Mute,
        SaveTrack,
        RemoveTrack,
        ToggleShuffle,
        ToggleRepeat
    }
}

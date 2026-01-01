namespace BeatBind.Core.Entities
{
    public class Hotkey
    {
        public int Id { get; set; }
        public int KeyCode { get; set; } // Store as int instead of Keys enum
        public ModifierKeys Modifiers { get; set; }
        public HotkeyAction Action { get; set; }
        public bool IsEnabled { get; set; } = true;

        // Available keys for hotkey binding (stored as int to avoid Windows.Forms dependency)
        public static readonly int[] AvailableKeyCodes = new[]
        {
            // Function Keys
            112, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 123, // F1-F12
            
            // Letters
            65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, // A-M
            78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, // N-Z
            
            // Numbers
            48, 49, 50, 51, 52, 53, 54, 55, 56, 57, // 0-9
            
            // Navigation
            37, 38, 39, 40, // Left, Up, Right, Down
            36, 35, 33, 34, // Home, End, PageUp, PageDown
            45, 46, // Insert, Delete
            
            // Special Keys
            32, 13, 9, 27, // Space, Enter, Tab, Escape
            
            // Punctuation & Symbols (OEM Keys)
            186, // OemSemicolon (;:)
            187, // Oemplus (=+)
            188, // Oemcomma (,<)
            189, // OemMinus (-_)
            190, // OemPeriod (.>)
            191, // OemQuestion (/?)
            192, // Oemtilde (`~)
            219, // OemOpenBrackets ([{)
            220, // OemPipe (\|)
            221, // OemCloseBrackets (]})
            222, // OemQuotes ('")
            
            // Media Keys
            179, // MediaPlayPause
            176, // MediaNextTrack
            177, // MediaPreviousTrack
            178, // MediaStop
            173, // VolumeMute
            175, // VolumeUp
            174, // VolumeDown
            
            // Numpad
            96, 97, 98, 99, 100, 101, 102, 103, 104, 105, // Numpad 0-9
        };

        public static string GetActionDisplayName(HotkeyAction action)
        {
            return action switch
            {
                HotkeyAction.PlayPause => "Play/Pause",
                HotkeyAction.Play => "Play",
                HotkeyAction.Pause => "Pause",
                HotkeyAction.NextTrack => "Next Track",
                HotkeyAction.PreviousTrack => "Previous Track",
                HotkeyAction.VolumeUp => "Volume Up",
                HotkeyAction.VolumeDown => "Volume Down",
                HotkeyAction.MuteUnmute => "Mute/Unmute",
                HotkeyAction.Mute => "Mute",
                HotkeyAction.Unmute => "Unmute",
                HotkeyAction.SeekForward => "Seek Forward",
                HotkeyAction.SeekBackward => "Seek Backward",
                HotkeyAction.SaveTrack => "Save Track",
                HotkeyAction.RemoveTrack => "Remove Track",
                HotkeyAction.ToggleShuffle => "Toggle Shuffle",
                HotkeyAction.ToggleRepeat => "Toggle Repeat",
                _ => action.ToString()
            };
        }

        public static string GetKeyDisplayName(int keyCode)
        {
            // Key codes from System.Windows.Forms.Keys enum
            return keyCode switch
            {
                // Numbers (D0-D9)
                48 => "0",
                49 => "1",
                50 => "2",
                51 => "3",
                52 => "4",
                53 => "5",
                54 => "6",
                55 => "7",
                56 => "8",
                57 => "9",

                // Punctuation & Symbols
                186 => "; (Semicolon / Ö / Ä)",
                187 => "= (Plus)",
                188 => ", (Comma)",
                189 => "- (Minus)",
                190 => ". (Period)",
                191 => "/ (Slash)",
                192 => "` (Tilde / Ø)",
                219 => "[ (Open Bracket / Å)",
                220 => "\\ (Backslash)",
                221 => "] (Close Bracket)",
                222 => "' (Quote / Æ)",

                // Numpad (96-105)
                96 => "Numpad 0",
                97 => "Numpad 1",
                98 => "Numpad 2",
                99 => "Numpad 3",
                100 => "Numpad 4",
                101 => "Numpad 5",
                102 => "Numpad 6",
                103 => "Numpad 7",
                104 => "Numpad 8",
                105 => "Numpad 9",

                // Media Keys
                179 => "Media Play/Pause",
                176 => "Media Next Track",
                177 => "Media Previous Track",
                178 => "Media Stop",
                173 => "Volume Mute",
                175 => "Volume Up",
                174 => "Volume Down",

                // Browser Keys
                166 => "Browser Back",
                167 => "Browser Forward",
                168 => "Browser Refresh",
                169 => "Browser Stop",
                170 => "Browser Search",
                171 => "Browser Favorites",
                172 => "Browser Home",

                // Navigation
                37 => "← (Left Arrow)",
                39 => "→ (Right Arrow)",
                38 => "↑ (Up Arrow)",
                40 => "↓ (Down Arrow)",
                33 => "Page Up",
                34 => "Page Down",

                // Letters (65-90 = A-Z)
                >= 65 and <= 90 => ((char)keyCode).ToString(),

                // Function Keys (112-123 = F1-F12)
                >= 112 and <= 123 => $"F{keyCode - 111}",

                // Special Keys
                32 => "Space",
                13 => "Enter",
                9 => "Tab",
                27 => "Escape",
                36 => "Home",
                35 => "End",
                45 => "Insert",
                46 => "Delete",

                // Default: return the key code
                _ => $"Key{keyCode}"
            };
        }
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
        Play,
        Pause,
        NextTrack,
        PreviousTrack,
        VolumeUp,
        VolumeDown,
        MuteUnmute,
        Mute,
        Unmute,
        SeekForward,
        SeekBackward,
        SaveTrack,
        RemoveTrack,
        ToggleShuffle,
        ToggleRepeat,
    }
}

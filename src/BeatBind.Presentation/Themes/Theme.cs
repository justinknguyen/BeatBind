using System.Drawing;

namespace BeatBind.Presentation.Themes
{
    public static class Theme
    {
        private static bool _isDarkMode = false;

        public static bool IsDarkMode
        {
            get => _isDarkMode;
            set
            {
                _isDarkMode = value;
                ThemeChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        public static event EventHandler? ThemeChanged;

        // Background Colors
        public static Color FormBackground => IsDarkMode ? Color.FromArgb(32, 33, 36) : Color.FromArgb(248, 249, 250);
        public static Color CardBackground => IsDarkMode ? Color.FromArgb(41, 42, 45) : Color.White;
        public static Color PanelBackground => IsDarkMode ? Color.FromArgb(32, 33, 36) : Color.White;
        public static Color InputBackground => IsDarkMode ? Color.FromArgb(50, 51, 54) : Color.White;
        public static Color HeaderBackground => IsDarkMode ? Color.FromArgb(50, 51, 54) : Color.FromArgb(248, 249, 250);

        // Text Colors
        public static Color PrimaryText => IsDarkMode ? Color.FromArgb(232, 234, 237) : Color.FromArgb(33, 37, 41);
        public static Color SecondaryText => IsDarkMode ? Color.FromArgb(154, 160, 166) : Color.FromArgb(108, 117, 125);
        public static Color LabelText => IsDarkMode ? Color.FromArgb(189, 193, 198) : Color.FromArgb(73, 80, 87);

        // Border Colors
        public static Color Border => IsDarkMode ? Color.FromArgb(60, 64, 67) : Color.FromArgb(220, 220, 220);
        public static Color InputBorder => IsDarkMode ? Color.FromArgb(95, 99, 104) : Color.FromArgb(206, 212, 218);

        // Status Colors
        public static Color Success => IsDarkMode ? Color.FromArgb(52, 168, 83) : Color.FromArgb(40, 167, 69);
        public static Color Error => IsDarkMode ? Color.FromArgb(217, 48, 37) : Color.FromArgb(220, 53, 69);
        public static Color Warning => IsDarkMode ? Color.FromArgb(251, 188, 4) : Color.FromArgb(255, 193, 7);
        public static Color Info => IsDarkMode ? Color.FromArgb(26, 115, 232) : Color.FromArgb(0, 123, 255);

        // Button Colors
        public static Color PrimaryButton => IsDarkMode ? Color.FromArgb(138, 180, 248) : Color.FromArgb(0, 123, 255);
        public static Color PrimaryButtonText => IsDarkMode ? Color.FromArgb(32, 33, 36) : Color.White;
        public static Color SecondaryButton => IsDarkMode ? Color.FromArgb(95, 99, 104) : Color.FromArgb(108, 117, 125);
        public static Color DangerButton => IsDarkMode ? Color.FromArgb(217, 48, 37) : Color.FromArgb(220, 53, 69);
    }
}

using System.Drawing;

namespace BeatBind.Presentation.Themes
{
    public static class Theme
    {
        // Background Colors
        public static Color FormBackground => Color.FromArgb(32, 33, 36);
        public static Color CardBackground => Color.FromArgb(41, 42, 45);
        public static Color PanelBackground => Color.FromArgb(32, 33, 36);
        public static Color InputBackground => Color.FromArgb(50, 51, 54);
        public static Color HeaderBackground => Color.FromArgb(50, 51, 54);

        // Text Colors
        public static Color PrimaryText => Color.FromArgb(232, 234, 237);
        public static Color SecondaryText => Color.FromArgb(154, 160, 166);
        public static Color LabelText => Color.FromArgb(189, 193, 198);

        // Border Colors
        public static Color Border => Color.FromArgb(60, 64, 67);
        public static Color InputBorder => Color.FromArgb(95, 99, 104);

        // Status Colors
        public static Color Success => Color.FromArgb(52, 168, 83);
        public static Color Error => Color.FromArgb(217, 48, 37);
        public static Color Warning => Color.FromArgb(251, 188, 4);
        public static Color Info => Color.FromArgb(26, 115, 232);

        // Button Colors
        public static Color PrimaryButton => Color.FromArgb(138, 180, 248);
        public static Color PrimaryButtonText => Color.FromArgb(32, 33, 36);
        public static Color SecondaryButton => Color.FromArgb(95, 99, 104);
        public static Color DangerButton => Color.FromArgb(217, 48, 37);
    }
}

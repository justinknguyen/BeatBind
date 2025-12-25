namespace BeatBind.Presentation.Themes
{
    public readonly struct Argb
    {
        public Argb(int r, int g, int b)
        {
            R = r;
            G = g;
            B = b;
        }

        public int R { get; }
        public int G { get; }
        public int B { get; }

        public Color ToColor() => Color.FromArgb(R, G, B);
    }

    public static class Colors
    {
        // Gray Scale
        public static readonly Argb DarkGray900 = new(32, 33, 36);
        public static readonly Argb DarkGray800 = new(41, 42, 45);
        public static readonly Argb DarkGray700 = new(50, 51, 54);
        public static readonly Argb DarkGray600 = new(60, 64, 67);
        public static readonly Argb Gray600 = new(95, 99, 104);
        public static readonly Argb Gray400 = new(154, 160, 166);
        public static readonly Argb Gray300 = new(189, 193, 198);
        public static readonly Argb LightGray100 = new(232, 234, 237);

        // Blues
        public static readonly Argb Blue300 = new(138, 180, 248);
        public static readonly Argb Blue500 = new(66, 133, 244);
        public static readonly Argb Blue600 = new(26, 115, 232);

        // Greens
        public static readonly Argb Green500 = new(52, 168, 83);
        public static readonly Argb Green600 = new(46, 125, 50);
        public static readonly Argb Green800 = new(27, 94, 32);

        // Reds
        public static readonly Argb Red600 = new(217, 48, 37);

        // Yellows
        public static readonly Argb Yellow600 = new(251, 188, 4);

        // Oranges
        public static readonly Argb Orange600 = new(255, 140, 0);
    }

    public static class Theme
    {
        // Background Colors
        public static Color FormBackground => Colors.DarkGray900.ToColor();
        public static Color CardBackground => Colors.DarkGray800.ToColor();
        public static Color PanelBackground => Colors.DarkGray900.ToColor();
        public static Color InputBackground => Colors.DarkGray700.ToColor();
        public static Color HeaderBackground => Colors.Green800.ToColor();
        public static Color InputFieldBackground => Colors.DarkGray600.ToColor();

        // Text Colors
        public static Color PrimaryText => Colors.LightGray100.ToColor();
        public static Color SecondaryText => Colors.Gray400.ToColor();
        public static Color LabelText => Colors.Gray300.ToColor();

        // Border Colors
        public static Color Border => Colors.DarkGray600.ToColor();
        public static Color InputBorder => Colors.Gray600.ToColor();

        // Status Colors
        public static Color Success => Colors.Green500.ToColor();
        public static Color Error => Colors.Red600.ToColor();
        public static Color Warning => Colors.Yellow600.ToColor();
        public static Color Info => Colors.Blue600.ToColor();

        // Button Colors
        public static Color PrimaryButton => Colors.Blue300.ToColor();
        public static Color PrimaryButtonText => Colors.DarkGray900.ToColor();
        public static Color SecondaryButton => Colors.Gray600.ToColor();
        public static Color DangerButton => Colors.Red600.ToColor();
        public static Color EditButton => Colors.Orange600.ToColor();
    }
}

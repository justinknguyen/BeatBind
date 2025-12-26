using BeatBind.Presentation.Themes;

namespace BeatBind.Presentation.Helpers;

/// <summary>
/// Helper class for applying themes and styling to controls.
/// Provides methods for recursive theme application and control positioning.
/// </summary>
public static class ThemeHelper
{
    /// <summary>
    /// Applies theme colors recursively to all controls in the hierarchy.
    /// This ensures consistent appearance across the application.
    /// Use this method after creating UI to apply proper theming.
    /// </summary>
    /// <param name="control">The root control to start applying theme from</param>
    public static void ApplyThemeToControlHierarchy(Control control)
    {
        foreach (Control child in control.Controls)
        {
            if (child is Panel panel)
            {
                if (panel.Tag?.ToString() == "headerPanel")
                {
                    panel.BackColor = Theme.HeaderBackground;
                }
                else if (IsLightColor(panel.BackColor))
                {
                    panel.BackColor = Theme.CardBackground;
                }
            }
            else if (child is Label label)
            {
                ApplyLabelTheme(label);
            }

            if (child.HasChildren)
            {
                ApplyThemeToControlHierarchy(child);
            }
        }
    }

    /// <summary>
    /// Centers a control within its parent container.
    /// Call this method in the parent's Resize event to keep the control centered.
    /// </summary>
    /// <param name="control">The control to center</param>
    /// <param name="parent">The parent container</param>
    public static void CenterControl(Control control, Control parent)
    {
        control.Location = new Point(
            (parent.Width - control.Width) / 2,
            (parent.Height - control.Height) / 2
        );
    }

    /// <summary>
    /// Applies appropriate theme colors to a label based on its tag.
    /// </summary>
    private static void ApplyLabelTheme(Label label)
    {
        if (label.Tag?.ToString() == "headerLabel")
        {
            label.ForeColor = Theme.PrimaryText;
            label.BackColor = Color.Transparent;
        }
        else if (label.Tag?.ToString() == "header")
        {
            label.BackColor = Theme.HeaderBackground;
            label.ForeColor = Theme.PrimaryText;
        }
        else if (IsDarkColor(label.ForeColor))
        {
            label.ForeColor = Theme.PrimaryText;
        }
    }

    /// <summary>
    /// Checks if a color is considered "light" (high RGB values).
    /// </summary>
    private static bool IsLightColor(Color color)
        => color.R > 200 && color.G > 200 && color.B > 200;

    /// <summary>
    /// Checks if a color is considered "dark" (low RGB values).
    /// </summary>
    private static bool IsDarkColor(Color color)
        => color.R < 100 && color.G < 100 && color.B < 100;
}

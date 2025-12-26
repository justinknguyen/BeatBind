using BeatBind.Presentation.Themes;
using MaterialSkin.Controls;

namespace BeatBind.Presentation.Helpers;

/// <summary>
/// Factory class for creating commonly-used controls with consistent styling.
/// Use these methods to ensure UI consistency and reduce repetitive code.
/// </summary>
public static class ControlFactory
{
    /// <summary>
    /// Creates a styled label with consistent appearance.
    /// </summary>
    /// <param name="text">The label text</param>
    /// <param name="bold">Whether the font should be bold</param>
    /// <param name="fontSize">Font size (default: 9f)</param>
    /// <returns>A configured Label control</returns>
    public static Label CreateLabel(string text, bool bold = false, float fontSize = 9f)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Top,
            Font = new Font("Segoe UI", fontSize, bold ? FontStyle.Bold : FontStyle.Regular),
            ForeColor = bold ? Theme.PrimaryText : Theme.LabelText,
            Margin = new Padding(0, 0, 0, 5),
            AutoSize = true
        };
    }

    /// <summary>
    /// Creates a header label for section titles.
    /// </summary>
    public static Label CreateHeaderLabel(string text, float fontSize = 9f)
    {
        return new Label
        {
            Text = text,
            Font = new Font("Segoe UI", fontSize, FontStyle.Bold),
            ForeColor = Theme.PrimaryText,
            AutoSize = true,
            Margin = new Padding(0, 5, 0, 3)
        };
    }

    /// <summary>
    /// Creates a MaterialTextBox with consistent styling.
    /// </summary>
    /// <param name="hint">Placeholder text</param>
    /// <param name="isPassword">Whether this is a password field</param>
    /// <param name="height">Control height (default: 48)</param>
    /// <returns>A configured MaterialTextBox</returns>
    public static MaterialTextBox CreateMaterialTextBox(string hint, bool isPassword = false, int height = 48)
    {
        return new MaterialTextBox
        {
            Dock = DockStyle.Top,
            Font = new Font("Segoe UI", 10f),
            Height = height,
            Margin = new Padding(0, 0, 0, 15),
            Hint = hint,
            Password = isPassword
        };
    }

    /// <summary>
    /// Creates a MaterialButton with consistent styling.
    /// </summary>
    /// <param name="text">Button text</param>
    /// <param name="width">Button width</param>
    /// <param name="height">Button height (default: 45)</param>
    /// <param name="useAccentColor">Whether to use accent color (default: false)</param>
    /// <returns>A configured MaterialButton</returns>
    public static MaterialButton CreateMaterialButton(string text, int width, int height = 45, bool useAccentColor = false)
    {
        return new MaterialButton
        {
            Text = text,
            Size = new Size(width, height),
            Type = MaterialButton.MaterialButtonType.Contained,
            Depth = 0,
            UseAccentColor = useAccentColor,
            AutoSize = false,
            Cursor = Cursors.Hand
        };
    }

    /// <summary>
    /// Creates a MaterialCheckbox with consistent styling.
    /// </summary>
    /// <param name="text">Checkbox label text</param>
    /// <param name="isChecked">Initial checked state (default: false)</param>
    /// <returns>A configured MaterialCheckbox</returns>
    public static MaterialCheckbox CreateMaterialCheckbox(string text, bool isChecked = false)
    {
        return new MaterialCheckbox
        {
            Text = text,
            Checked = isChecked,
            AutoSize = true,
            Depth = 0,
            Margin = new Padding(0, 0, 20, 0),
            TabStop = false
        };
    }

    /// <summary>
    /// Creates a MaterialLabel with consistent styling.
    /// </summary>
    /// <param name="text">Label text</param>
    /// <param name="highEmphasis">Whether to use high emphasis styling (default: false)</param>
    /// <param name="fontSize">Font size (default: 10f)</param>
    /// <returns>A configured MaterialLabel</returns>
    public static MaterialLabel CreateMaterialLabel(string text, bool highEmphasis = false, float fontSize = 10f)
    {
        return new MaterialLabel
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", fontSize),
            HighEmphasis = highEmphasis
        };
    }

    /// <summary>
    /// Creates a NumericUpDown control with consistent styling.
    /// </summary>
    /// <param name="minimum">Minimum value</param>
    /// <param name="maximum">Maximum value</param>
    /// <param name="value">Initial value</param>
    /// <param name="increment">Increment step (default: 1)</param>
    /// <param name="width">Control width (default: 80)</param>
    /// <returns>A configured NumericUpDown control</returns>
    public static NumericUpDown CreateNumericUpDown(
        decimal minimum, 
        decimal maximum, 
        decimal value, 
        decimal increment = 1, 
        int width = 80)
    {
        return new NumericUpDown
        {
            Minimum = minimum,
            Maximum = maximum,
            Value = value,
            Increment = increment,
            Font = new Font("Segoe UI", 8f),
            Width = width,
            Margin = new Padding(0, 3, 0, 3),
            BackColor = Theme.InputBackground,
            ForeColor = Theme.PrimaryText
        };
    }

    /// <summary>
    /// Creates a styled button for actions (edit, delete, etc.).
    /// </summary>
    /// <param name="emoji">Emoji or icon character</param>
    /// <param name="backgroundColor">Button background color</param>
    /// <param name="size">Button size (width and height)</param>
    /// <returns>A configured action button</returns>
    public static Button CreateActionButton(string emoji, Color backgroundColor, int size = 40)
    {
        var button = new Button
        {
            Text = emoji,
            Size = new Size(size, size - 8), // Slightly shorter for better visual alignment
            Font = new Font("Segoe UI", 10f),
            FlatStyle = FlatStyle.Flat,
            BackColor = backgroundColor,
            ForeColor = backgroundColor == Theme.EditButton ? Theme.PrimaryText : Color.White,
            Cursor = Cursors.Hand,
            Margin = new Padding(4, 5, 4, 5)
        };
        button.FlatAppearance.BorderSize = 0;
        return button;
    }

    /// <summary>
    /// Creates a TableLayoutPanel with a single column and auto-sizing rows.
    /// </summary>
    /// <param name="rowCount">Number of rows</param>
    /// <param name="padding">Panel padding (default: 15px all sides)</param>
    /// <returns>A configured TableLayoutPanel</returns>
    public static TableLayoutPanel CreateSingleColumnLayout(int rowCount, Padding? padding = null)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = rowCount,
            Padding = padding ?? new Padding(15),
            BackColor = Theme.CardBackground
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < rowCount; i++)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        return layout;
    }

    /// <summary>
    /// Creates a FlowLayoutPanel for checkboxes or similar controls.
    /// </summary>
    /// <param name="direction">Flow direction (default: LeftToRight)</param>
    /// <returns>A configured FlowLayoutPanel</returns>
    public static FlowLayoutPanel CreateFlowPanel(FlowDirection direction = FlowDirection.LeftToRight)
    {
        return new FlowLayoutPanel
        {
            FlowDirection = direction,
            WrapContents = true,
            Dock = DockStyle.Fill,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 8)
        };
    }
}

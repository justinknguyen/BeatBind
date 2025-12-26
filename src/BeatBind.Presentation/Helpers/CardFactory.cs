using BeatBind.Presentation.Themes;

namespace BeatBind.Presentation.Helpers;

/// <summary>
/// Factory class for creating card components with headers and styled borders.
/// Cards are container panels with a header section and content area.
/// </summary>
public static class CardFactory
{
    /// <summary>
    /// Creates a card panel with a header and content area.
    /// Use CompactCardStyle for smaller cards (30px header), ModernCardStyle for larger cards (40px header).
    /// </summary>
    /// <param name="title">The title displayed in the card header</param>
    /// <param name="content">The control to display in the card body</param>
    /// <param name="style">The visual style of the card (Compact or Modern)</param>
    /// <param name="fixedHeight">Optional fixed height for the card. If null, height is determined by content</param>
    /// <returns>A styled card panel ready to be added to a layout</returns>
    public static Panel CreateCard(string title, Control content, CardStyle style, int? fixedHeight = null)
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = style == CardStyle.Compact 
                ? new Padding(0, 0, 0, 8) 
                : new Padding(0, 0, 0, 15),
            BackColor = Theme.CardBackground,
            BorderStyle = BorderStyle.None
        };

        if (fixedHeight.HasValue)
        {
            card.Dock = DockStyle.Top;
            card.Height = fixedHeight.Value;
        }

        // Draw border around card
        card.Paint += (s, e) =>
        {
            var rect = card.ClientRectangle;
            rect.Width -= 1;
            rect.Height -= 1;
            using var borderPen = new Pen(Theme.Border);
            e.Graphics.DrawRectangle(borderPen, rect);
        };

        int headerHeight = style == CardStyle.Compact ? 30 : 40;
        var headerFont = style == CardStyle.Compact 
            ? new Font("Segoe UI", 10f, FontStyle.Bold)
            : new Font("Segoe UI", 12f, FontStyle.Bold);
        var headerPadding = style == CardStyle.Compact
            ? new Padding(10, 0, 10, 0)
            : new Padding(15, 0, 15, 0);
        var contentPadding = style == CardStyle.Compact
            ? new Padding(10)
            : new Padding(15, 15, 15, 15);

        var headerPanel = CreateHeaderPanel(title, headerHeight, headerFont, headerPadding);
        var contentPanel = CreateContentPanel(content, contentPadding);

        card.Controls.Add(contentPanel);
        card.Controls.Add(headerPanel);

        // Ensure proper z-order
        headerPanel.SendToBack();
        contentPanel.BringToFront();

        return card;
    }

    /// <summary>
    /// Creates a compact card (30px header height) - ideal for settings and configuration panels.
    /// </summary>
    /// <param name="title">The title text displayed in the card header</param>
    /// <param name="content">The control to display in the card body</param>
    /// <returns>A styled card panel with compact header</returns>
    public static Panel CreateCompactCard(string title, Control content) 
        => CreateCard(title, content, CardStyle.Compact);

    /// <summary>
    /// Creates a modern card (40px header height) - ideal for feature sections and primary content.
    /// </summary>
    /// <param name="title">The title text displayed in the card header</param>
    /// <param name="content">The control to display in the card body</param>
    /// <param name="fixedHeight">Optional fixed height for the card</param>
    /// <returns>A styled card panel with modern header</returns>
    public static Panel CreateModernCard(string title, Control content, int? fixedHeight = null) 
        => CreateCard(title, content, CardStyle.Modern, fixedHeight);

    /// <summary>
    /// Creates a styled header panel with consistent appearance.
    /// </summary>
    private static Panel CreateHeaderPanel(string title, int height, Font font, Padding padding)
    {
        var headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = height,
            BackColor = Theme.HeaderBackground,
            Tag = "headerPanel",
            Padding = padding
        };

        var titleLabel = new Label
        {
            Text = title,
            Font = font,
            ForeColor = Theme.PrimaryText,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.Transparent,
            Tag = "headerLabel"
        };

        headerPanel.Controls.Add(titleLabel);
        return headerPanel;
    }

    /// <summary>
    /// Creates a content panel to hold the card's main content.
    /// </summary>
    private static Panel CreateContentPanel(Control content, Padding padding)
    {
        var contentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = padding,
            BackColor = Theme.CardBackground
        };

        contentPanel.Controls.Add(content);
        return contentPanel;
    }
}

/// <summary>
/// Defines the visual style of a card component.
/// </summary>
public enum CardStyle
{
    /// <summary>Compact style with 30px header - for settings and configuration</summary>
    Compact,
    
    /// <summary>Modern style with 40px header - for main content areas</summary>
    Modern
}

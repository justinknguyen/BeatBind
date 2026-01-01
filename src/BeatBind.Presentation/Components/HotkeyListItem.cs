using System.ComponentModel;
using BeatBind.Core.Entities;
using BeatBind.Presentation.Helpers;
using BeatBind.Presentation.Themes;
using DomainModifierKeys = BeatBind.Core.Entities.ModifierKeys;

namespace BeatBind.Presentation.Components
{
    public partial class HotkeyListItem : UserControl
    {
        private readonly Hotkey _hotkey;
        private Button _editButton = null!;
        private Button _deleteButton = null!;
        private Label _descriptionLabel = null!;
        private Label _keysLabel = null!;
        private Panel _cardPanel = null!;
        private bool _isPending;

        public event EventHandler? EditRequested;
        public event EventHandler? DeleteRequested;

        public Hotkey Hotkey => _hotkey;

        /// <summary>
        /// Gets or sets whether this hotkey has unsaved changes.
        /// </summary>
        public bool IsPending
        {
            get => _isPending;
            set
            {
                if (_isPending != value)
                {
                    _isPending = value;
                    UpdatePendingState();
                }
            }
        }

        /// <summary>
        /// Parameterless constructor for WinForms designer support.
        /// </summary>
        public HotkeyListItem()
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            {
                _hotkey = new Hotkey
                {
                    Action = HotkeyAction.PlayPause,
                    KeyCode = (int)Keys.Space,
                    Modifiers = DomainModifierKeys.Control | DomainModifierKeys.Alt,
                    IsEnabled = true
                };

                InitializeComponent();
                UpdateDisplay();
                ApplyTheme();
            }
            else
            {
                throw new InvalidOperationException("Designer-only constructor; provide a Hotkey at runtime.");
            }
        }

        /// <summary>
        /// Initializes a new instance of the HotkeyListItem with a hotkey.
        /// </summary>
        /// <param name="hotkey">The hotkey to display</param>
        public HotkeyListItem(Hotkey hotkey)
        {
            _hotkey = hotkey;
            InitializeComponent();
            UpdateDisplay();
            ApplyTheme();
        }

        /// <summary>
        /// Initializes all UI components and layout for the hotkey list item.
        /// </summary>
        private void InitializeComponent()
        {
            Size = new Size(540, 60);
            BackColor = Theme.CardBackground;
            BorderStyle = BorderStyle.None;
            Margin = new Padding(0, 0, 0, 8);

            // Card container with shadow effect
            _cardPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.CardBackground,
                Padding = new Padding(2)
            };

            // Add shadow effect with pending state support
            _cardPanel.Paint += (s, e) =>
            {
                var rect = _cardPanel.ClientRectangle;
                rect.Width -= 1;
                rect.Height -= 1;

                // Draw border - thicker and colored if pending
                if (_isPending)
                {
                    using (var pen = new Pen(Theme.PendingHighlight, 3))
                    {
                        e.Graphics.DrawRectangle(pen, rect);
                    }
                }
                else
                {
                    using (var shadowPen = new Pen(Theme.Border))
                    {
                        e.Graphics.DrawRectangle(shadowPen, rect);
                    }
                }
            };

            var contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.CardBackground,
                Padding = new Padding(12, 8, 12, 8)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                BackColor = Theme.CardBackground
            };

            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            _descriptionLabel = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Theme.PrimaryText,
                BackColor = Color.Transparent
            };

            _keysLabel = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Consolas", 8.5f),
                ForeColor = Theme.SecondaryText,
                BackColor = Theme.InputFieldBackground,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 5, 8, 5),
                Padding = new Padding(8, 5, 8, 5)
            };

            // Use ControlFactory for consistent button creation
            _editButton = ControlFactory.CreateActionButton("✏️", Theme.EditButton);
            _editButton.Click += (s, e) => EditRequested?.Invoke(this, EventArgs.Empty);

            _deleteButton = ControlFactory.CreateActionButton("🗑️", Theme.DangerButton);
            _deleteButton.Margin = new Padding(4, 5, 0, 5);
            _deleteButton.Click += (s, e) => DeleteRequested?.Invoke(this, EventArgs.Empty);

            layout.Controls.Add(_descriptionLabel, 0, 0);
            layout.Controls.Add(_keysLabel, 1, 0);
            layout.Controls.Add(_editButton, 2, 0);
            layout.Controls.Add(_deleteButton, 3, 0);

            contentPanel.Controls.Add(layout);
            _cardPanel.Controls.Add(contentPanel);
            Controls.Add(_cardPanel);
        }

        /// <summary>
        /// Updates the hotkey display with new values.
        /// </summary>
        /// <param name="_">The updated hotkey (parameter not currently used)</param>
        public void UpdateHotkey(Hotkey _)
        {
            // Update the internal reference (note: this is a simplified approach)
            // In a more robust implementation, you'd need to properly handle the update
            UpdateDisplay();
        }

        /// <summary>
        /// Updates the display labels with current hotkey information.
        /// </summary>
        private void UpdateDisplay()
        {
            _descriptionLabel.Text = Hotkey.GetActionDisplayName(_hotkey.Action);
            _keysLabel.Text = FormatHotkeyString(_hotkey);
        }

        /// <summary>
        /// Applies theme colors to all UI components.
        /// </summary>
        private void ApplyTheme()
        {
            BackColor = Theme.CardBackground;
            _descriptionLabel.ForeColor = Theme.PrimaryText;
            _keysLabel.ForeColor = Theme.SecondaryText;
            _keysLabel.BackColor = Theme.InputFieldBackground;
        }

        /// <summary>
        /// Updates the visual appearance based on pending state.
        /// </summary>
        private void UpdatePendingState()
        {
            if (_cardPanel != null)
            {
                _cardPanel.Invalidate(); // Trigger repaint to show/hide pending border
            }
        }

        /// <summary>
        /// Formats a hotkey into a human-readable string (e.g., "Ctrl + Alt + P").
        /// </summary>
        /// <param name="hotkey">The hotkey to format</param>
        /// <returns>A formatted string representation of the hotkey</returns>
        private static string FormatHotkeyString(Hotkey hotkey)
        {
            var parts = new List<string>();

            if (hotkey.Modifiers.HasFlag(DomainModifierKeys.Control))
            {
                parts.Add("Ctrl");
            }

            if (hotkey.Modifiers.HasFlag(DomainModifierKeys.Alt))
            {
                parts.Add("Alt");
            }

            if (hotkey.Modifiers.HasFlag(DomainModifierKeys.Shift))
            {
                parts.Add("Shift");
            }

            if (hotkey.Modifiers.HasFlag(DomainModifierKeys.Windows))
            {
                parts.Add("Win");
            }

            parts.Add(Hotkey.GetKeyDisplayName(hotkey.KeyCode));

            return string.Join(" + ", parts);
        }
    }
}

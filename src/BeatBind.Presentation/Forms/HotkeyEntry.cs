using System.ComponentModel;
using BeatBind.Core.Entities;
using BeatBind.Presentation.Themes;
using DomainModifierKeys = BeatBind.Core.Entities.ModifierKeys;

namespace BeatBind.Presentation.Forms
{
    public partial class HotkeyEntry : UserControl
    {
        private readonly Hotkey _hotkey;
        private Button _editButton = null!;
        private Button _deleteButton = null!;
        private Label _descriptionLabel = null!;
        private Label _keysLabel = null!;

        public event EventHandler? EditRequested;
        public event EventHandler? DeleteRequested;

        public Hotkey Hotkey => _hotkey;

        // Parameterless ctor for WinForms designer support
        public HotkeyEntry()
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

        public HotkeyEntry(Hotkey hotkey)
        {
            _hotkey = hotkey;
            InitializeComponent();
            UpdateDisplay();
            ApplyTheme();
        }

        private void InitializeComponent()
        {
            Size = new Size(540, 60);
            BackColor = Theme.CardBackground;
            BorderStyle = BorderStyle.None;
            Margin = new Padding(0, 0, 0, 8);

            // Card container with shadow effect
            var cardPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.CardBackground,
                Padding = new Padding(1)
            };

            // Add shadow effect
            cardPanel.Paint += (s, e) =>
            {
                var rect = cardPanel.ClientRectangle;
                rect.Width -= 1;
                rect.Height -= 1;
                using (var shadowPen = new Pen(Theme.Border))
                {
                    e.Graphics.DrawRectangle(shadowPen, rect);
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

            _editButton = new Button
            {
                Text = "✏️",
                Size = new Size(40, 32),
                Font = new Font("Segoe UI", 10f),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.EditButton,
                ForeColor = Theme.PrimaryText,
                Cursor = Cursors.Hand,
                Margin = new Padding(4, 5, 4, 5)
            };
            _editButton.FlatAppearance.BorderSize = 0;
            _editButton.Click += (s, e) => EditRequested?.Invoke(this, EventArgs.Empty);

            _deleteButton = new Button
            {
                Text = "🗑️",
                Size = new Size(40, 32),
                Font = new Font("Segoe UI", 10f),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.DangerButton,
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Margin = new Padding(4, 5, 0, 5)
            };
            _deleteButton.FlatAppearance.BorderSize = 0;
            _deleteButton.Click += (s, e) => DeleteRequested?.Invoke(this, EventArgs.Empty);

            layout.Controls.Add(_descriptionLabel, 0, 0);
            layout.Controls.Add(_keysLabel, 1, 0);
            layout.Controls.Add(_editButton, 2, 0);
            layout.Controls.Add(_deleteButton, 3, 0);

            contentPanel.Controls.Add(layout);
            cardPanel.Controls.Add(contentPanel);
            Controls.Add(cardPanel);
        }

        public void UpdateHotkey(Hotkey hotkey)
        {
            // Update the internal reference (note: this is a simplified approach)
            // In a more robust implementation, you'd need to properly handle the update
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            _descriptionLabel.Text = _hotkey.Action.ToString();
            _keysLabel.Text = FormatHotkeyString(_hotkey);
        }

        private void ApplyTheme()
        {
            BackColor = Theme.CardBackground;
            _descriptionLabel.ForeColor = Theme.PrimaryText;
            _keysLabel.ForeColor = Theme.SecondaryText;
            _keysLabel.BackColor = Theme.InputFieldBackground;
        }

        private static string FormatHotkeyString(Hotkey hotkey)
        {
            var parts = new List<string>();

            if (hotkey.Modifiers.HasFlag(DomainModifierKeys.Control))
                parts.Add("Ctrl");
            if (hotkey.Modifiers.HasFlag(DomainModifierKeys.Alt))
                parts.Add("Alt");
            if (hotkey.Modifiers.HasFlag(DomainModifierKeys.Shift))
                parts.Add("Shift");
            if (hotkey.Modifiers.HasFlag(DomainModifierKeys.Windows))
                parts.Add("Win");

            parts.Add(((Keys)hotkey.KeyCode).ToString());

            return string.Join(" + ", parts);
        }
    }
}

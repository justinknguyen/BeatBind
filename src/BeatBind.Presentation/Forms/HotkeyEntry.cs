using System.Drawing;
using System.Windows.Forms;
using BeatBind.Domain.Entities;
using DomainModifierKeys = BeatBind.Domain.Entities.ModifierKeys;

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

        public HotkeyEntry(Hotkey hotkey)
        {
            _hotkey = hotkey;
            InitializeComponent();
            UpdateDisplay();
        }

        private void InitializeComponent()
        {
            Size = new Size(450, 40);
            BorderStyle = BorderStyle.FixedSingle;
            
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                Padding = new Padding(5)
            };

            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            _descriptionLabel = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(FontFamily.GenericSansSerif, 9, FontStyle.Bold)
            };

            _keysLabel = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font(FontFamily.GenericMonospace, 8)
            };

            _editButton = new Button
            {
                Text = "Edit",
                Size = new Size(50, 25),
                UseVisualStyleBackColor = true
            };
            _editButton.Click += (s, e) => EditRequested?.Invoke(this, EventArgs.Empty);

            _deleteButton = new Button
            {
                Text = "Delete",
                Size = new Size(55, 25),
                UseVisualStyleBackColor = true
            };
            _deleteButton.Click += (s, e) => DeleteRequested?.Invoke(this, EventArgs.Empty);

            layout.Controls.Add(_descriptionLabel, 0, 0);
            layout.Controls.Add(_keysLabel, 1, 0);
            layout.Controls.Add(_editButton, 2, 0);
            layout.Controls.Add(_deleteButton, 3, 0);

            Controls.Add(layout);
        }

        public void UpdateHotkey(Hotkey hotkey)
        {
            // Update the internal reference (note: this is a simplified approach)
            // In a more robust implementation, you'd need to properly handle the update
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            _descriptionLabel.Text = _hotkey.Description;
            _keysLabel.Text = FormatHotkeyString(_hotkey);
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

using BeatBind.Core.Entities;
using BeatBind.Presentation.Themes;
using DomainModifierKeys = BeatBind.Core.Entities.ModifierKeys;

namespace BeatBind.Presentation.Components
{
    public partial class HotkeyEditorDialog : Form
    {
        private ComboBox _actionComboBox = null!;
        private ComboBox _keyComboBox = null!;
        private CheckBox _ctrlCheckBox = null!;
        private CheckBox _altCheckBox = null!;
        private CheckBox _shiftCheckBox = null!;
        private CheckBox _winCheckBox = null!;
        private CheckBox _enabledCheckBox = null!;
        private Button _okButton = null!;
        private Button _cancelButton = null!;
        private Label _previewLabel = null!;

        public Hotkey Hotkey { get; private set; } = new();

        public HotkeyEditorDialog(Hotkey? existingHotkey = null)
        {
            if (existingHotkey != null)
            {
                Hotkey = existingHotkey;
            }

            InitializeComponent();
            PopulateActionComboBox();
            PopulateKeyComboBox();

            if (existingHotkey != null)
            {
                LoadHotkeyData(existingHotkey);
            }

            ApplyTheme();
        }

        private void ApplyTheme()
        {
            BackColor = Theme.FormBackground;
        }

        private void InitializeComponent()
        {
            Text = "Hotkey Configuration";
            Size = new Size(450, 350);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Theme.FormBackground;
            Font = new Font("Segoe UI", 9f);

            // Main container
            var mainContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                BackColor = Theme.FormBackground
            };

            // Content panel
            var contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.CardBackground,
                Padding = new Padding(20)
            };

            // Add border to content panel
            contentPanel.Paint += (s, e) =>
            {
                var rect = contentPanel.ClientRectangle;
                rect.Width -= 1;
                rect.Height -= 1;
                e.Graphics.DrawRectangle(new Pen(Theme.Border), rect);
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 6,
                BackColor = Theme.CardBackground,
                Padding = new Padding(10)
            };

            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // Action
            var actionLabel = new Label
            {
                Text = "Action:",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Theme.PrimaryText,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                Margin = new Padding(0, 5, 10, 5)
            };
            layout.Controls.Add(actionLabel, 0, 0);

            _actionComboBox = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9f),
                Margin = new Padding(0, 5, 0, 5),
                BackColor = Theme.InputBackground,
                ForeColor = Theme.PrimaryText
            };
            _actionComboBox.SelectedIndexChanged += (s, e) => UpdatePreview();
            layout.Controls.Add(_actionComboBox, 1, 0);

            // Key
            var keyLabel = new Label
            {
                Text = "Key:",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Theme.PrimaryText,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                Margin = new Padding(0, 5, 10, 5)
            };
            layout.Controls.Add(keyLabel, 0, 1);

            _keyComboBox = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9f),
                Margin = new Padding(0, 5, 0, 5),
                BackColor = Theme.InputBackground,
                ForeColor = Theme.PrimaryText
            };
            _keyComboBox.SelectedIndexChanged += (s, e) => UpdatePreview();
            layout.Controls.Add(_keyComboBox, 1, 1);

            // Modifiers
            var modifiersLabel = new Label
            {
                Text = "Modifiers:",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Theme.PrimaryText,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.TopRight,
                Margin = new Padding(0, 10, 10, 5)
            };
            layout.Controls.Add(modifiersLabel, 0, 2);

            var modifiersPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Margin = new Padding(0, 10, 0, 5)
            };

            _ctrlCheckBox = new CheckBox
            {
                Text = "Ctrl",
                AutoSize = true,
                Font = new Font("Segoe UI", 9f),
                Margin = new Padding(0, 0, 15, 5),
                ForeColor = Theme.PrimaryText
            };
            _ctrlCheckBox.CheckedChanged += (s, e) => UpdatePreview();

            _altCheckBox = new CheckBox
            {
                Text = "Alt",
                AutoSize = true,
                Font = new Font("Segoe UI", 9f),
                Margin = new Padding(0, 0, 15, 5),
                ForeColor = Theme.PrimaryText
            };
            _altCheckBox.CheckedChanged += (s, e) => UpdatePreview();

            _shiftCheckBox = new CheckBox
            {
                Text = "Shift",
                AutoSize = true,
                Font = new Font("Segoe UI", 9f),
                Margin = new Padding(0, 0, 15, 5),
                ForeColor = Theme.PrimaryText
            };
            _shiftCheckBox.CheckedChanged += (s, e) => UpdatePreview();

            _winCheckBox = new CheckBox
            {
                Text = "Win",
                AutoSize = true,
                Font = new Font("Segoe UI", 9f),
                Margin = new Padding(0, 0, 15, 5),
                ForeColor = Theme.PrimaryText
            };
            _winCheckBox.CheckedChanged += (s, e) => UpdatePreview();

            modifiersPanel.Controls.AddRange(new Control[] { _ctrlCheckBox, _altCheckBox, _shiftCheckBox, _winCheckBox });
            layout.Controls.Add(modifiersPanel, 1, 2);

            // Preview
            var previewLabel = new Label
            {
                Text = "Preview:",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Theme.PrimaryText,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                Margin = new Padding(0, 10, 10, 5)
            };
            layout.Controls.Add(previewLabel, 0, 3);

            _previewLabel = new Label
            {
                Text = "No hotkey configured",
                Font = new Font("Consolas", 9f, FontStyle.Bold),
                ForeColor = Theme.SecondaryText,
                BackColor = Theme.HeaderBackground,
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 10, 0, 5)
            };
            layout.Controls.Add(_previewLabel, 1, 3);

            // Enabled
            var enabledLabel = new Label
            {
                Text = "Enabled:",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Theme.PrimaryText,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                Margin = new Padding(0, 10, 10, 5)
            };
            layout.Controls.Add(enabledLabel, 0, 4);

            _enabledCheckBox = new CheckBox
            {
                Text = "Enable this hotkey",
                Checked = true,
                Font = new Font("Segoe UI", 9f),
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 10, 0, 5),
                ForeColor = Theme.PrimaryText
            };
            layout.Controls.Add(_enabledCheckBox, 1, 4);

            // Button panel
            var buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Theme.FormBackground,
                Padding = new Padding(15)
            };

            var buttonContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            _cancelButton = new Button
            {
                Text = "Cancel",
                Size = new Size(100, 35),
                Font = new Font("Segoe UI", 9f),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.SecondaryButton,
                ForeColor = Color.White,
                DialogResult = DialogResult.Cancel,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            _cancelButton.FlatAppearance.BorderSize = 0;

            _okButton = new Button
            {
                Text = "Save",
                Size = new Size(100, 35),
                Font = new Font("Segoe UI", 9f),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Success,
                ForeColor = Color.White,
                DialogResult = DialogResult.OK,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            _okButton.FlatAppearance.BorderSize = 0;
            _okButton.Click += OkButton_Click;

            // Position buttons
            buttonContainer.Resize += (s, e) =>
            {
                _okButton.Location = new Point(buttonContainer.Width - _okButton.Width,
                    (buttonContainer.Height - _okButton.Height) / 2);
                _cancelButton.Location = new Point(_okButton.Left - _cancelButton.Width - 10,
                    (buttonContainer.Height - _cancelButton.Height) / 2);
            };

            buttonContainer.Controls.Add(_cancelButton);
            buttonContainer.Controls.Add(_okButton);
            buttonPanel.Controls.Add(buttonContainer);

            contentPanel.Controls.Add(layout);
            mainContainer.Controls.Add(contentPanel);
            mainContainer.Controls.Add(buttonPanel);
            Controls.Add(mainContainer);

            AcceptButton = _okButton;
            CancelButton = _cancelButton;
        }


        private void UpdatePreview()
        {
            var parts = new List<string>();

            if (_ctrlCheckBox?.Checked == true) parts.Add("Ctrl");
            if (_altCheckBox?.Checked == true) parts.Add("Alt");
            if (_shiftCheckBox?.Checked == true) parts.Add("Shift");
            if (_winCheckBox?.Checked == true) parts.Add("Win");

            if (_keyComboBox?.SelectedItem != null)
                parts.Add(_keyComboBox.SelectedItem.ToString()!);

            if (_previewLabel != null)
            {
                _previewLabel.Text = parts.Count > 0 ? string.Join(" + ", parts) : "No hotkey configured";
            }
        }

        private void PopulateActionComboBox()
        {
            var actions = Enum.GetValues<HotkeyAction>()
                .Select(action => new { Value = action, Display = Hotkey.GetActionDisplayName(action) })
                .ToArray();

            _actionComboBox.DataSource = actions;
            _actionComboBox.DisplayMember = "Display";
            _actionComboBox.ValueMember = "Value";
        }

        private void PopulateKeyComboBox()
        {
            // Use keys from the entity with friendly display names
            var keys = Hotkey.AvailableKeyCodes
                .Select(code => new { Value = (Keys)code, Display = Hotkey.GetKeyDisplayName(code) })
                .OrderBy(k => k.Display)
                .ToArray();

            _keyComboBox.DataSource = keys;
            _keyComboBox.DisplayMember = "Display";
            _keyComboBox.ValueMember = "Value";
        }

        private void LoadHotkeyData(Hotkey hotkey)
        {
            _actionComboBox.SelectedValue = hotkey.Action;
            _ctrlCheckBox.Checked = hotkey.Modifiers.HasFlag(DomainModifierKeys.Control);
            _altCheckBox.Checked = hotkey.Modifiers.HasFlag(DomainModifierKeys.Alt);
            _shiftCheckBox.Checked = hotkey.Modifiers.HasFlag(DomainModifierKeys.Shift);
            _winCheckBox.Checked = hotkey.Modifiers.HasFlag(DomainModifierKeys.Windows);
            _keyComboBox.SelectedValue = (Keys)hotkey.KeyCode;
            _enabledCheckBox.Checked = hotkey.IsEnabled;
            UpdatePreview();
        }


        private void OkButton_Click(object? sender, EventArgs e)
        {
            if (ValidateInput())
            {
                // If this is a new hotkey (ID is 0), generate a new ID
                if (Hotkey.Id == 0)
                {
                    Hotkey.Id = Environment.TickCount;
                }

                Hotkey.Action = _actionComboBox.SelectedValue is HotkeyAction action
                    ? action
                    : HotkeyAction.PlayPause; // or another default/fallback action
                Hotkey.KeyCode = _keyComboBox.SelectedValue is Keys key ? (int)key : 0;
                Hotkey.IsEnabled = _enabledCheckBox.Checked;

                var modifiers = DomainModifierKeys.None;
                if (_ctrlCheckBox.Checked) modifiers |= DomainModifierKeys.Control;
                if (_altCheckBox.Checked) modifiers |= DomainModifierKeys.Alt;
                if (_shiftCheckBox.Checked) modifiers |= DomainModifierKeys.Shift;
                if (_winCheckBox.Checked) modifiers |= DomainModifierKeys.Windows;
                Hotkey.Modifiers = modifiers;
            }
            else
            {
                DialogResult = DialogResult.None;
            }
        }

        private bool ValidateInput()
        {

            if (_actionComboBox.SelectedValue == null)
            {
                MessageBox.Show("Please select an action.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (_keyComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a key.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }
    }
}

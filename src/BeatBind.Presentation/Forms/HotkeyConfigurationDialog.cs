using System.Drawing;
using System.Windows.Forms;
using BeatBind.Domain.Entities;
using DomainModifierKeys = BeatBind.Domain.Entities.ModifierKeys;

namespace BeatBind.Presentation.Forms
{
    public partial class HotkeyConfigurationDialog : Form
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

        public HotkeyConfigurationDialog(Hotkey? existingHotkey = null)
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
        }

        private void InitializeComponent()
        {
            Text = "Hotkey Configuration";
            Size = new Size(450, 350);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(248, 249, 250);
            Font = new Font("Segoe UI", 9f);

            // Main container
            var mainContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                BackColor = Color.FromArgb(248, 249, 250)
            };

            // Content panel
            var contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(20)
            };

            // Add border to content panel
            contentPanel.Paint += (s, e) =>
            {
                var rect = contentPanel.ClientRectangle;
                rect.Width -= 1;
                rect.Height -= 1;
                e.Graphics.DrawRectangle(new Pen(Color.FromArgb(220, 220, 220)), rect);
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 6,
                BackColor = Color.White,
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
                ForeColor = Color.FromArgb(33, 37, 41),
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
                Margin = new Padding(0, 5, 0, 5)
            };
            _actionComboBox.SelectedIndexChanged += (s, e) => UpdatePreview();
            layout.Controls.Add(_actionComboBox, 1, 0);

            // Key
            var keyLabel = new Label
            {
                Text = "Key:",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 37, 41),
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
                Margin = new Padding(0, 5, 0, 5)
            };
            _keyComboBox.SelectedIndexChanged += (s, e) => UpdatePreview();
            layout.Controls.Add(_keyComboBox, 1, 1);

            // Modifiers
            var modifiersLabel = new Label
            {
                Text = "Modifiers:",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 37, 41),
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
                Margin = new Padding(0, 0, 15, 5)
            };
            _ctrlCheckBox.CheckedChanged += (s, e) => UpdatePreview();

            _altCheckBox = new CheckBox
            {
                Text = "Alt",
                AutoSize = true,
                Font = new Font("Segoe UI", 9f),
                Margin = new Padding(0, 0, 15, 5)
            };
            _altCheckBox.CheckedChanged += (s, e) => UpdatePreview();

            _shiftCheckBox = new CheckBox
            {
                Text = "Shift",
                AutoSize = true,
                Font = new Font("Segoe UI", 9f),
                Margin = new Padding(0, 0, 15, 5)
            };
            _shiftCheckBox.CheckedChanged += (s, e) => UpdatePreview();

            _winCheckBox = new CheckBox
            {
                Text = "Win",
                AutoSize = true,
                Font = new Font("Segoe UI", 9f),
                Margin = new Padding(0, 0, 15, 5)
            };
            _winCheckBox.CheckedChanged += (s, e) => UpdatePreview();

            modifiersPanel.Controls.AddRange(new Control[] { _ctrlCheckBox, _altCheckBox, _shiftCheckBox, _winCheckBox });
            layout.Controls.Add(modifiersPanel, 1, 2);

            // Preview
            var previewLabel = new Label
            {
                Text = "Preview:",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 37, 41),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                Margin = new Padding(0, 10, 10, 5)
            };
            layout.Controls.Add(previewLabel, 0, 3);

            _previewLabel = new Label
            {
                Text = "No hotkey configured",
                Font = new Font("Consolas", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(108, 117, 125),
                BackColor = Color.FromArgb(248, 249, 250),
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
                ForeColor = Color.FromArgb(33, 37, 41),
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
                Margin = new Padding(0, 10, 0, 5)
            };
            layout.Controls.Add(_enabledCheckBox, 1, 4);

            // Button panel
            var buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(248, 249, 250),
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
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                DialogResult = DialogResult.Cancel,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            _cancelButton.FlatAppearance.BorderSize = 0;

            _okButton = new Button
            {
                Text = "Save",
                Size = new Size(100, 35),
                Font = new Font("Segoe UI", 9f),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                DialogResult = DialogResult.OK,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
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
                .Select(action => new { Value = action, Display = GetActionDisplayName(action) })
                .ToArray();

            _actionComboBox.DataSource = actions;
            _actionComboBox.DisplayMember = "Display";
            _actionComboBox.ValueMember = "Value";
        }

        private void PopulateKeyComboBox()
        {
            // Add common keys
            var keys = new[]
            {
                Keys.F1, Keys.F2, Keys.F3, Keys.F4, Keys.F5, Keys.F6,
                Keys.F7, Keys.F8, Keys.F9, Keys.F10, Keys.F11, Keys.F12,
                Keys.A, Keys.B, Keys.C, Keys.D, Keys.E, Keys.F, Keys.G,
                Keys.H, Keys.I, Keys.J, Keys.K, Keys.L, Keys.M, Keys.N,
                Keys.O, Keys.P, Keys.Q, Keys.R, Keys.S, Keys.T, Keys.U,
                Keys.V, Keys.W, Keys.X, Keys.Y, Keys.Z,
                Keys.D0, Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5,
                Keys.D6, Keys.D7, Keys.D8, Keys.D9,
                Keys.Space, Keys.Enter, Keys.Tab, Keys.Escape,
                Keys.Left, Keys.Right, Keys.Up, Keys.Down,
                Keys.Home, Keys.End, Keys.PageUp, Keys.PageDown,
                Keys.Insert, Keys.Delete
            };

            _keyComboBox.DataSource = keys;
        }

        private void LoadHotkeyData(Hotkey hotkey)
        {
            _actionComboBox.SelectedValue = hotkey.Action;
            _ctrlCheckBox.Checked = hotkey.Modifiers.HasFlag(DomainModifierKeys.Control);
            _altCheckBox.Checked = hotkey.Modifiers.HasFlag(DomainModifierKeys.Alt);
            _shiftCheckBox.Checked = hotkey.Modifiers.HasFlag(DomainModifierKeys.Shift);
            _winCheckBox.Checked = hotkey.Modifiers.HasFlag(DomainModifierKeys.Windows);
            _keyComboBox.SelectedItem = (Keys)hotkey.KeyCode;
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
                Hotkey.KeyCode = _keyComboBox.SelectedItem is Keys key ? (int)key : 0;
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

        private static string GetActionDisplayName(HotkeyAction action)
        {
            return action switch
            {
                HotkeyAction.PlayPause => "Play/Pause",
                HotkeyAction.NextTrack => "Next Track",
                HotkeyAction.PreviousTrack => "Previous Track",
                HotkeyAction.VolumeUp => "Volume Up",
                HotkeyAction.VolumeDown => "Volume Down",
                HotkeyAction.Mute => "Mute/Unmute",
                HotkeyAction.SaveTrack => "Save Track",
                HotkeyAction.RemoveTrack => "Remove Track",
                HotkeyAction.ToggleShuffle => "Toggle Shuffle",
                HotkeyAction.ToggleRepeat => "Toggle Repeat",
                HotkeyAction.SeekForward => "Seek Forward",
                HotkeyAction.SeekBackward => "Seek Backward",
                _ => action.ToString()
            };
        }
    }
}

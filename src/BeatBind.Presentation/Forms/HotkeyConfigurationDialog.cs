using System.Drawing;
using System.Windows.Forms;
using BeatBind.Domain.Entities;
using DomainModifierKeys = BeatBind.Domain.Entities.ModifierKeys;

namespace BeatBind.Presentation.Forms
{
    public partial class HotkeyConfigurationDialog : Form
    {
        private ComboBox _actionComboBox = null!;
        private TextBox _descriptionTextBox = null!;
        private CheckBox _ctrlCheckBox = null!;
        private CheckBox _altCheckBox = null!;
        private CheckBox _shiftCheckBox = null!;
        private CheckBox _winCheckBox = null!;
        private ComboBox _keyComboBox = null!;
        private CheckBox _enabledCheckBox = null!;
        private Button _okButton = null!;
        private Button _cancelButton = null!;

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
            Size = new Size(400, 350);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 8,
                Padding = new Padding(15)
            };

            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));

            // Action
            var actionLabel = new Label { Text = "Action:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight };
            _actionComboBox = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            _actionComboBox.SelectedIndexChanged += ActionComboBox_SelectedIndexChanged;

            // Description
            var descLabel = new Label { Text = "Description:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight };
            _descriptionTextBox = new TextBox { Dock = DockStyle.Fill };

            // Modifiers
            var modifiersLabel = new Label { Text = "Modifiers:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight };
            var modifiersPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
            
            _ctrlCheckBox = new CheckBox { Text = "Ctrl", AutoSize = true };
            _altCheckBox = new CheckBox { Text = "Alt", AutoSize = true };
            _shiftCheckBox = new CheckBox { Text = "Shift", AutoSize = true };
            _winCheckBox = new CheckBox { Text = "Win", AutoSize = true };

            modifiersPanel.Controls.AddRange(new Control[] { _ctrlCheckBox, _altCheckBox, _shiftCheckBox, _winCheckBox });

            // Key
            var keyLabel = new Label { Text = "Key:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight };
            _keyComboBox = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };

            // Enabled
            var enabledLabel = new Label { Text = "Enabled:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight };
            _enabledCheckBox = new CheckBox { Dock = DockStyle.Fill, Checked = true };

            // Buttons
            var buttonPanel = new FlowLayoutPanel 
            { 
                Dock = DockStyle.Fill, 
                FlowDirection = FlowDirection.RightToLeft,
                Anchor = AnchorStyles.Right
            };

            _cancelButton = new Button { Text = "Cancel", Size = new Size(75, 25), DialogResult = DialogResult.Cancel };
            _okButton = new Button { Text = "OK", Size = new Size(75, 25), DialogResult = DialogResult.OK };
            _okButton.Click += OkButton_Click;

            buttonPanel.Controls.AddRange(new Control[] { _cancelButton, _okButton });

            // Add to layout
            mainLayout.Controls.Add(actionLabel, 0, 0);
            mainLayout.Controls.Add(_actionComboBox, 1, 0);
            mainLayout.Controls.Add(descLabel, 0, 1);
            mainLayout.Controls.Add(_descriptionTextBox, 1, 1);
            mainLayout.Controls.Add(modifiersLabel, 0, 2);
            mainLayout.Controls.Add(modifiersPanel, 1, 2);
            mainLayout.Controls.Add(keyLabel, 0, 3);
            mainLayout.Controls.Add(_keyComboBox, 1, 3);
            mainLayout.Controls.Add(enabledLabel, 0, 4);
            mainLayout.Controls.Add(_enabledCheckBox, 1, 4);
            mainLayout.SetColumnSpan(buttonPanel, 2);
            mainLayout.Controls.Add(buttonPanel, 0, 6);

            Controls.Add(mainLayout);

            AcceptButton = _okButton;
            CancelButton = _cancelButton;
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
            _descriptionTextBox.Text = hotkey.Description;
            _ctrlCheckBox.Checked = hotkey.Modifiers.HasFlag(DomainModifierKeys.Control);
            _altCheckBox.Checked = hotkey.Modifiers.HasFlag(DomainModifierKeys.Alt);
            _shiftCheckBox.Checked = hotkey.Modifiers.HasFlag(DomainModifierKeys.Shift);
            _winCheckBox.Checked = hotkey.Modifiers.HasFlag(DomainModifierKeys.Windows);
            _keyComboBox.SelectedItem = (Keys)hotkey.KeyCode;
            _enabledCheckBox.Checked = hotkey.IsEnabled;
        }

        private void ActionComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_actionComboBox.SelectedValue is HotkeyAction action)
            {
                _descriptionTextBox.Text = GetActionDisplayName(action);
            }
        }

        private void OkButton_Click(object? sender, EventArgs e)
        {
            if (ValidateInput())
            {
                Hotkey.Action = (HotkeyAction)_actionComboBox.SelectedValue;
                Hotkey.Description = _descriptionTextBox.Text;
                Hotkey.KeyCode = (int)(Keys)_keyComboBox.SelectedItem;
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
            if (string.IsNullOrWhiteSpace(_descriptionTextBox.Text))
            {
                MessageBox.Show("Please enter a description.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

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
                _ => action.ToString()
            };
        }
    }
}

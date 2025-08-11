using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;

namespace BeatBind
{
    public partial class MainForm : Form
    {
        private readonly SpotifyBackend _backend;
        private readonly ConfigurationManager _configManager;
        private readonly GlobalHotkeyManager _hotkeyManager;
        private readonly ILogger<MainForm> _logger;
        
        private NotifyIcon? _notifyIcon;
        private bool _isAuthenticated;

        // UI Controls
        private TextBox _clientIdTextBox = null!;
        private TextBox _clientSecretTextBox = null!;
        private Button _authenticateButton = null!;
        private Label _statusLabel = null!;
        private Panel _hotkeyPanel = null!;
        private FlowLayoutPanel _hotkeyFlowPanel = null!;
        private Button _addHotkeyButton = null!;
        private Button _saveConfigButton = null!;
        private Dictionary<string, HotkeyEntry> _hotkeyEntries = new();

        public MainForm(SpotifyBackend backend)
        {
            _backend = backend;
            _configManager = new ConfigurationManager();
            _hotkeyManager = new GlobalHotkeyManager(this);
            _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<MainForm>();

            InitializeComponent();
            SetupNotifyIcon();
            SetupHotkeys();
            UpdateAuthenticationStatus();
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            // Form settings
            Text = "BeatBind - Spotify Global Hotkeys";
            Size = new Size(500, 600);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = true;
            ShowInTaskbar = true;

            // Main layout
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(10)
            };

            // Spotify Configuration Section
            var spotifyConfigGroup = new GroupBox
            {
                Text = "Spotify Configuration",
                Height = 120,
                Dock = DockStyle.Top
            };

            var configLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3,
                Padding = new Padding(10)
            };

            configLayout.Controls.Add(new Label { Text = "Client ID:", Anchor = AnchorStyles.Left }, 0, 0);
            _clientIdTextBox = new TextBox { Dock = DockStyle.Fill, Text = _configManager.ClientId };
            configLayout.Controls.Add(_clientIdTextBox, 1, 0);

            configLayout.Controls.Add(new Label { Text = "Client Secret:", Anchor = AnchorStyles.Left }, 0, 1);
            _clientSecretTextBox = new TextBox { Dock = DockStyle.Fill, Text = _configManager.ClientSecret, UseSystemPasswordChar = true };
            configLayout.Controls.Add(_clientSecretTextBox, 1, 1);

            _authenticateButton = new Button { Text = "Authenticate with Spotify", Dock = DockStyle.Fill };
            _authenticateButton.Click += OnAuthenticateClick;
            configLayout.Controls.Add(_authenticateButton, 0, 2);
            configLayout.SetColumnSpan(_authenticateButton, 2);

            spotifyConfigGroup.Controls.Add(configLayout);

            // Status Section
            var statusGroup = new GroupBox
            {
                Text = "Status",
                Height = 60,
                Dock = DockStyle.Top
            };

            _statusLabel = new Label
            {
                Text = "Not authenticated",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.Red
            };
            statusGroup.Controls.Add(_statusLabel);

            // Hotkey Configuration Section
            _hotkeyPanel = new Panel
            {
                Height = 350,
                Dock = DockStyle.Top
            };

            var hotkeyGroupBox = new GroupBox
            {
                Text = "Hotkey Configuration",
                Dock = DockStyle.Fill
            };

            var hotkeyLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(10)
            };
            hotkeyLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            hotkeyLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            hotkeyLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // Add hotkey button
            _addHotkeyButton = new Button
            {
                Text = "Add Hotkey",
                Height = 30,
                Dock = DockStyle.Fill
            };
            _addHotkeyButton.Click += OnAddHotkeyClick;

            // Scrollable hotkey list
            var scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BorderStyle = BorderStyle.FixedSingle
            };

            _hotkeyFlowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };

            scrollPanel.Controls.Add(_hotkeyFlowPanel);

            hotkeyLayout.Controls.Add(_addHotkeyButton, 0, 0);
            hotkeyLayout.Controls.Add(scrollPanel, 0, 1);

            hotkeyGroupBox.Controls.Add(hotkeyLayout);
            _hotkeyPanel.Controls.Add(hotkeyGroupBox);

            CreateHotkeyEntries();

            // Action buttons
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                FlowDirection = FlowDirection.RightToLeft
            };

            _saveConfigButton = new Button { Text = "Save Configuration", AutoSize = true };
            _saveConfigButton.Click += OnSaveConfigClick;
            buttonPanel.Controls.Add(_saveConfigButton);

            var exitButton = new Button { Text = "Exit", AutoSize = true };
            exitButton.Click += (s, e) => Close();
            buttonPanel.Controls.Add(exitButton);

            // Add all sections to main panel
            mainPanel.Controls.Add(spotifyConfigGroup, 0, 0);
            mainPanel.Controls.Add(statusGroup, 0, 1);
            mainPanel.Controls.Add(_hotkeyPanel, 0, 2);
            mainPanel.Controls.Add(buttonPanel, 0, 3);

            Controls.Add(mainPanel);

            // Form events
            FormClosing += OnFormClosing;
            Resize += OnFormResize;

            ResumeLayout();
        }

        private void CreateHotkeyEntries()
        {
            // Create entries for all default hotkeys
            var hotkeys = _configManager.Hotkeys;
            var hotkeyProperties = typeof(HotkeyConfiguration).GetProperties();

            foreach (var property in hotkeyProperties)
            {
                var action = SplitCamelCase(property.Name);
                var currentValue = property.GetValue(hotkeys)?.ToString() ?? "";
                
                AddHotkeyEntry(action, currentValue, property.Name);
            }
        }

        private void AddHotkeyEntry(string action, string hotkeyValue, string propertyName)
        {
            var entry = new HotkeyEntry(action, hotkeyValue, propertyName);
            entry.DeleteRequested += (s, e) => RemoveHotkeyEntry(entry);
            
            _hotkeyEntries[propertyName] = entry;
            _hotkeyFlowPanel.Controls.Add(entry);
        }

        private void RemoveHotkeyEntry(HotkeyEntry entry)
        {
            _hotkeyEntries.Remove(entry.PropertyName);
            _hotkeyFlowPanel.Controls.Remove(entry);
            entry.Dispose();
        }

        private void OnAddHotkeyClick(object? sender, EventArgs e)
        {
            using var dialog = new AddHotkeyDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var propertyName = dialog.SelectedAction.Replace(" ", "");
                if (!_hotkeyEntries.ContainsKey(propertyName))
                {
                    AddHotkeyEntry(dialog.SelectedAction, "", propertyName);
                }
            }
        }

        private void SetupNotifyIcon()
        {
            _notifyIcon = new NotifyIcon
            {
                Text = "BeatBind - Spotify Global Hotkeys",
                Visible = true
            };

            // Create context menu
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Show", null, (s, e) => { Show(); WindowState = FormWindowState.Normal; });
            contextMenu.Items.Add("Exit", null, (s, e) => Close());

            _notifyIcon.ContextMenuStrip = contextMenu;
            _notifyIcon.DoubleClick += (s, e) => { Show(); WindowState = FormWindowState.Normal; };

            // Try to load icon (you'll need to add an icon file)
            try
            {
                // You would load your icon here
                // _notifyIcon.Icon = new Icon("Resources\\icon.ico");
                _notifyIcon.Icon = SystemIcons.Application; // Fallback
            }
            catch
            {
                _notifyIcon.Icon = SystemIcons.Application;
            }
        }

        private void SetupHotkeys()
        {
            if (!_configManager.HasValidSpotifyCredentials())
                return;

            var hotkeys = _configManager.Hotkeys;

            _hotkeyManager.RegisterHotkey(hotkeys.PlayPause, async () => await _backend.PlayPauseAsync());
            _hotkeyManager.RegisterHotkey(hotkeys.NextTrack, async () => await _backend.NextTrackAsync());
            _hotkeyManager.RegisterHotkey(hotkeys.PreviousTrack, async () => await _backend.PreviousTrackAsync());
            _hotkeyManager.RegisterHotkey(hotkeys.VolumeUp, async () => await _backend.AdjustVolumeAsync(_configManager.Config.VolumeStep));
            _hotkeyManager.RegisterHotkey(hotkeys.VolumeDown, async () => await _backend.AdjustVolumeAsync(-_configManager.Config.VolumeStep));
            _hotkeyManager.RegisterHotkey(hotkeys.Mute, async () => await _backend.MuteAsync());
            _hotkeyManager.RegisterHotkey(hotkeys.SeekForward, async () => await _backend.SeekAsync(_configManager.Config.SeekStep));
            _hotkeyManager.RegisterHotkey(hotkeys.SeekBackward, async () => await _backend.SeekAsync(-_configManager.Config.SeekStep));
            _hotkeyManager.RegisterHotkey(hotkeys.SaveTrack, async () => await _backend.SaveCurrentTrackAsync());
            _hotkeyManager.RegisterHotkey(hotkeys.RemoveTrack, async () => await _backend.RemoveCurrentTrackAsync());

            _logger.LogInformation("Global hotkeys registered");
        }

        private async void OnAuthenticateClick(object? sender, EventArgs e)
        {
            try
            {
                _configManager.ClientId = _clientIdTextBox.Text;
                _configManager.ClientSecret = _clientSecretTextBox.Text;

                _authenticateButton.Enabled = false;
                _authenticateButton.Text = "Authenticating...";

                var success = await _backend.AuthenticateAsync();
                _isAuthenticated = success;

                UpdateAuthenticationStatus();

                if (success)
                {
                    SetupHotkeys();
                    MessageBox.Show("Authentication successful!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Authentication failed. Please check your credentials.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Authentication error");
                MessageBox.Show($"Authentication error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _authenticateButton.Enabled = true;
                _authenticateButton.Text = "Authenticate with Spotify";
            }
        }

        private void OnSaveConfigClick(object? sender, EventArgs e)
        {
            try
            {
                // Save hotkey configurations
                foreach (var entry in _hotkeyEntries.Values)
                {
                    var property = typeof(HotkeyConfiguration).GetProperty(entry.PropertyName);
                    property?.SetValue(_configManager.Hotkeys, entry.HotkeyValue);
                }

                _configManager.SaveConfiguration();
                
                // Re-register hotkeys with new configuration
                _hotkeyManager.UnregisterAllHotkeys();
                SetupHotkeys();

                MessageBox.Show("Configuration saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving configuration");
                MessageBox.Show($"Error saving configuration: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateAuthenticationStatus()
        {
            if (_isAuthenticated && _backend.IsTokenValid)
            {
                _statusLabel.Text = "Authenticated and connected to Spotify";
                _statusLabel.ForeColor = Color.Green;
            }
            else if (_configManager.HasValidSpotifyCredentials())
            {
                _statusLabel.Text = "Credentials configured - Click authenticate to connect";
                _statusLabel.ForeColor = Color.Orange;
            }
            else
            {
                _statusLabel.Text = "Not authenticated - Enter Spotify credentials";
                _statusLabel.ForeColor = Color.Red;
            }
        }

        private void OnFormClosing(object? sender, FormClosingEventArgs e)
        {
            if (_configManager.Config.MinimizeToTray && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                _notifyIcon?.ShowBalloonTip(2000, "BeatBind", "Application minimized to tray", ToolTipIcon.Info);
            }
            else
            {
                _hotkeyManager?.Dispose();
                _notifyIcon?.Dispose();
            }
        }

        private void OnFormResize(object? sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized && _configManager.Config.MinimizeToTray)
            {
                Hide();
            }
        }

        private static string SplitCamelCase(string input)
        {
            return System.Text.RegularExpressions.Regex.Replace(input, "([A-Z])", " $1").Trim();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _hotkeyManager?.Dispose();
                _notifyIcon?.Dispose();
                _backend?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    public class HotkeyEntry : UserControl
    {
        private readonly Label _actionLabel;
        private readonly HotkeyTextBox _hotkeyTextBox;
        private readonly Button _deleteButton;

        public string PropertyName { get; }
        public string HotkeyValue => _hotkeyTextBox.Text;
        public event EventHandler? DeleteRequested;

        public HotkeyEntry(string action, string hotkeyValue, string propertyName)
        {
            PropertyName = propertyName;
            Height = 35;
            Width = 450;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(5)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            _actionLabel = new Label
            {
                Text = action + ":",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _hotkeyTextBox = new HotkeyTextBox
            {
                Dock = DockStyle.Fill,
                Text = hotkeyValue
            };

            _deleteButton = new Button
            {
                Text = "×",
                Width = 25,
                Height = 23,
                Font = new Font(Font.FontFamily, 10, FontStyle.Bold),
                ForeColor = Color.Red,
                Anchor = AnchorStyles.None
            };
            _deleteButton.Click += (s, e) => DeleteRequested?.Invoke(this, EventArgs.Empty);

            layout.Controls.Add(_actionLabel, 0, 0);
            layout.Controls.Add(_hotkeyTextBox, 1, 0);
            layout.Controls.Add(_deleteButton, 2, 0);

            Controls.Add(layout);
        }
    }

    public class HotkeyTextBox : TextBox
    {
        private readonly List<Keys> _pressedKeys = new();
        private readonly HashSet<Keys> _modifierKeys = new();

        public HotkeyTextBox()
        {
            ReadOnly = true;
            KeyDown += OnKeyDown;
            KeyUp += OnKeyUp;
            Enter += OnEnter;
            Leave += OnLeave;
        }

        private void OnEnter(object? sender, EventArgs e)
        {
            Text = "Press keys...";
            BackColor = Color.LightYellow;
            _pressedKeys.Clear();
            _modifierKeys.Clear();
        }

        private void OnLeave(object? sender, EventArgs e)
        {
            BackColor = SystemColors.Window;
            if (Text == "Press keys...")
            {
                Text = "";
            }
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;

            var key = e.KeyCode;

            // Handle modifier keys
            if (key == Keys.ControlKey || key == Keys.LControlKey || key == Keys.RControlKey)
            {
                _modifierKeys.Add(Keys.Control);
            }
            else if (key == Keys.Menu || key == Keys.LMenu || key == Keys.RMenu)
            {
                _modifierKeys.Add(Keys.Alt);
            }
            else if (key == Keys.ShiftKey || key == Keys.LShiftKey || key == Keys.RShiftKey)
            {
                _modifierKeys.Add(Keys.Shift);
            }
            else if (key == Keys.LWin || key == Keys.RWin)
            {
                _modifierKeys.Add(Keys.LWin);
            }
            else
            {
                // Regular key pressed
                if (!_pressedKeys.Contains(key))
                {
                    _pressedKeys.Add(key);
                }
            }

            UpdateDisplay();
        }

        private void OnKeyUp(object? sender, KeyEventArgs e)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;

            // If we have at least one modifier and one regular key, finalize the combination
            if (_modifierKeys.Count > 0 && _pressedKeys.Count > 0)
            {
                FinalizeCombination();
            }
        }

        private void UpdateDisplay()
        {
            var parts = new List<string>();

            if (_modifierKeys.Contains(Keys.Control)) parts.Add("Ctrl");
            if (_modifierKeys.Contains(Keys.Alt)) parts.Add("Alt");
            if (_modifierKeys.Contains(Keys.Shift)) parts.Add("Shift");
            if (_modifierKeys.Contains(Keys.LWin)) parts.Add("Win");

            foreach (var key in _pressedKeys)
            {
                parts.Add(GetKeyDisplayName(key));
            }

            Text = string.Join("+", parts);
        }

        private void FinalizeCombination()
        {
            UpdateDisplay();
            _pressedKeys.Clear();
            _modifierKeys.Clear();
        }

        private string GetKeyDisplayName(Keys key)
        {
            return key switch
            {
                Keys.Space => "Space",
                Keys.Left => "Left",
                Keys.Right => "Right",
                Keys.Up => "Up",
                Keys.Down => "Down",
                Keys.Enter => "Enter",
                Keys.Escape => "Esc",
                Keys.Tab => "Tab",
                Keys.Back => "Backspace",
                Keys.Delete => "Delete",
                Keys.Home => "Home",
                Keys.End => "End",
                Keys.PageUp => "PageUp",
                Keys.PageDown => "PageDown",
                _ => key.ToString()
            };
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Capture all key combinations
            return true;
        }
    }

    public partial class AddHotkeyDialog : Form
    {
        private ComboBox _actionComboBox = null!;
        private Button _okButton = null!;
        private Button _cancelButton = null!;

        public string SelectedAction => _actionComboBox.SelectedItem?.ToString() ?? "";

        public AddHotkeyDialog()
        {
            InitializeComponent();
            PopulateActions();
        }

        private void InitializeComponent()
        {
            Text = "Add Hotkey";
            Size = new Size(300, 120);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(10)
            };

            var actionLabel = new Label
            {
                Text = "Action:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _actionComboBox = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft
            };

            _cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                AutoSize = true
            };

            _okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                AutoSize = true
            };
            _okButton.Click += (s, e) =>
            {
                if (_actionComboBox.SelectedItem == null)
                {
                    MessageBox.Show("Please select an action.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                DialogResult = DialogResult.OK;
                Close();
            };

            buttonPanel.Controls.Add(_cancelButton);
            buttonPanel.Controls.Add(_okButton);

            layout.Controls.Add(actionLabel, 0, 0);
            layout.Controls.Add(_actionComboBox, 1, 0);
            layout.Controls.Add(buttonPanel, 0, 1);
            layout.SetColumnSpan(buttonPanel, 2);

            Controls.Add(layout);

            AcceptButton = _okButton;
            CancelButton = _cancelButton;
        }

        private void PopulateActions()
        {
            var actions = new[]
            {
                "Play Pause",
                "Next Track",
                "Previous Track",
                "Volume Up",
                "Volume Down",
                "Mute",
                "Seek Forward",
                "Seek Backward",
                "Save Track",
                "Remove Track"
            };

            _actionComboBox.Items.AddRange(actions);
        }
    }
}

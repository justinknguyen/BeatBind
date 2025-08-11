using System.Drawing;
using System.Windows.Forms;
using BeatBind.Application.Services;
using BeatBind.Application.UseCases;
using BeatBind.Domain.Entities;
using BeatBind.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace BeatBind.Presentation.Forms
{
    public partial class MainForm : Form
    {
        private readonly MusicControlService _musicControlService;
        private HotkeyManagementService _hotkeyManagementService;
        private readonly AuthenticateUserUseCase _authenticateUserUseCase;
        private readonly SaveConfigurationUseCase _saveConfigurationUseCase;
        private readonly IConfigurationService _configurationService;
        private readonly ILogger<MainForm> _logger;
        
        private NotifyIcon? _notifyIcon;
        private bool _isAuthenticated;

        // UI Controls
        private TextBox _clientIdTextBox = null!;
        private TextBox _clientSecretTextBox = null!;
        private Button _authenticateButton = null!;
        private Label _statusLabel = null!;
        private Panel _hotkeyPanel = null!;
        private Label _lastHotkeyLabel = null!;
        private FlowLayoutPanel _hotkeyFlowPanel = null!;
        private Button _addHotkeyButton = null!;
        private Button _saveConfigButton = null!;
        private Dictionary<string, HotkeyEntry> _hotkeyEntries = new();

        public MainForm(
            MusicControlService musicControlService,
            AuthenticateUserUseCase authenticateUserUseCase,
            SaveConfigurationUseCase saveConfigurationUseCase,
            IConfigurationService configurationService,
            ILogger<MainForm> logger)
        {
            _musicControlService = musicControlService;
            _authenticateUserUseCase = authenticateUserUseCase;
            _saveConfigurationUseCase = saveConfigurationUseCase;
            _configurationService = configurationService;
            _logger = logger;

            InitializeComponent();
            SetupNotifyIcon();
            LoadConfiguration();
            UpdateAuthenticationStatus();
        }

        public void SetHotkeyManagementService(HotkeyManagementService hotkeyManagementService)
        {
            _hotkeyManagementService = hotkeyManagementService;
            
            // Initialize hotkeys from configuration once the service is set
            _hotkeyManagementService.InitializeHotkeys();
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            // Form settings
            Text = "BeatBind - Spotify Global Hotkeys";
            Size = new Size(550, 700);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;

            // Main layout
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6,
                Padding = new Padding(10)
            };

            // Client credentials section
            var credentialsGroup = CreateCredentialsSection();
            mainPanel.Controls.Add(credentialsGroup);
            mainPanel.SetRow(credentialsGroup, 0);

            // Authentication section
            var authGroup = CreateAuthenticationSection();
            mainPanel.Controls.Add(authGroup);
            mainPanel.SetRow(authGroup, 1);

            // Status section
            var statusGroup = CreateStatusSection();
            mainPanel.Controls.Add(statusGroup);
            mainPanel.SetRow(statusGroup, 2);

            // Last hotkey section
            var lastHotkeyGroup = CreateLastHotkeySection();
            mainPanel.Controls.Add(lastHotkeyGroup);
            mainPanel.SetRow(lastHotkeyGroup, 3);

            // Hotkey configuration section
            _hotkeyPanel = CreateHotkeyConfigurationSection();
            mainPanel.Controls.Add(_hotkeyPanel);
            mainPanel.SetRow(_hotkeyPanel, 4);

            // Save configuration section
            var saveGroup = CreateSaveConfigurationSection();
            mainPanel.Controls.Add(saveGroup);
            mainPanel.SetRow(saveGroup, 5);

            Controls.Add(mainPanel);

            ResumeLayout(false);
        }

        private GroupBox CreateCredentialsSection()
        {
            var credentialsGroup = new GroupBox
            {
                Text = "Spotify Client Credentials",
                Height = 120,
                Dock = DockStyle.Top
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(10)
            };

            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));

            var clientIdLabel = new Label { Text = "Client ID:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight };
            _clientIdTextBox = new TextBox { Dock = DockStyle.Fill };

            var clientSecretLabel = new Label { Text = "Client Secret:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight };
            _clientSecretTextBox = new TextBox { Dock = DockStyle.Fill, UseSystemPasswordChar = true };

            layout.Controls.Add(clientIdLabel, 0, 0);
            layout.Controls.Add(_clientIdTextBox, 1, 0);
            layout.Controls.Add(clientSecretLabel, 0, 1);
            layout.Controls.Add(_clientSecretTextBox, 1, 1);

            credentialsGroup.Controls.Add(layout);
            return credentialsGroup;
        }

        private GroupBox CreateAuthenticationSection()
        {
            var authGroup = new GroupBox
            {
                Text = "Authentication",
                Height = 70,
                Dock = DockStyle.Top
            };

            _authenticateButton = new Button
            {
                Text = "Authenticate with Spotify",
                Size = new Size(200, 30),
                Dock = DockStyle.Fill
            };
            _authenticateButton.Click += AuthenticateButton_Click;

            authGroup.Controls.Add(_authenticateButton);
            return authGroup;
        }

        private GroupBox CreateStatusSection()
        {
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

            return statusGroup;
        }

        private GroupBox CreateLastHotkeySection()
        {
            var lastHotkeyGroup = new GroupBox
            {
                Text = "Last Hotkey Triggered",
                Height = 50,
                Dock = DockStyle.Top
            };

            _lastHotkeyLabel = new Label
            {
                Text = "(none)",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Bold),
                ForeColor = Color.Blue
            };
            lastHotkeyGroup.Controls.Add(_lastHotkeyLabel);

            return lastHotkeyGroup;
        }

        private Panel CreateHotkeyConfigurationSection()
        {
            var hotkeyPanel = new Panel
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
            hotkeyLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            hotkeyLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _addHotkeyButton = new Button
            {
                Text = "Add New Hotkey",
                Height = 30,
                Dock = DockStyle.Fill
            };
            _addHotkeyButton.Click += AddHotkeyButton_Click;

            _hotkeyFlowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true
            };

            hotkeyLayout.Controls.Add(_addHotkeyButton, 0, 0);
            hotkeyLayout.Controls.Add(_hotkeyFlowPanel, 0, 1);

            hotkeyGroupBox.Controls.Add(hotkeyLayout);
            hotkeyPanel.Controls.Add(hotkeyGroupBox);

            return hotkeyPanel;
        }

        private GroupBox CreateSaveConfigurationSection()
        {
            var saveGroup = new GroupBox
            {
                Text = "Configuration",
                Height = 70,
                Dock = DockStyle.Top
            };

            _saveConfigButton = new Button
            {
                Text = "Save Configuration",
                Size = new Size(150, 30),
                Dock = DockStyle.Fill
            };
            _saveConfigButton.Click += SaveConfigButton_Click;

            saveGroup.Controls.Add(_saveConfigButton);
            return saveGroup;
        }

        private void SetupNotifyIcon()
        {
            _notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = "BeatBind - Spotify Global Hotkeys",
                Visible = true
            };

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Show", null, (s, e) => { Show(); WindowState = FormWindowState.Normal; });
            contextMenu.Items.Add("Exit", null, (s, e) => Close());

            _notifyIcon.ContextMenuStrip = contextMenu;
            _notifyIcon.DoubleClick += (s, e) => { Show(); WindowState = FormWindowState.Normal; };
        }

        private void LoadConfiguration()
        {
            try
            {
                var config = _configurationService.GetConfiguration();
                _clientIdTextBox.Text = config.ClientId;
                _clientSecretTextBox.Text = config.ClientSecret;

                LoadHotkeysFromConfiguration(config.Hotkeys);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load configuration");
                MessageBox.Show("Failed to load configuration. Using defaults.", "Configuration Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void LoadHotkeysFromConfiguration(List<Hotkey> hotkeys)
        {
            _hotkeyFlowPanel.Controls.Clear();
            _hotkeyEntries.Clear();

            foreach (var hotkey in hotkeys)
            {
                AddHotkeyEntryToUI(hotkey);
            }
        }

        private async void AuthenticateButton_Click(object? sender, EventArgs e)
        {
            try
            {
                // Save credentials first
                if (!string.IsNullOrEmpty(_clientIdTextBox.Text) && !string.IsNullOrEmpty(_clientSecretTextBox.Text))
                {
                    _saveConfigurationUseCase.Execute(_clientIdTextBox.Text, _clientSecretTextBox.Text);
                }

                _authenticateButton.Enabled = false;
                _authenticateButton.Text = "Authenticating...";

                var success = await _authenticateUserUseCase.ExecuteAsync();
                
                _isAuthenticated = success;
                UpdateAuthenticationStatus();

                if (success)
                {
                    MessageBox.Show("Authentication successful!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Authentication failed. Please check your credentials and try again.", "Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private void SaveConfigButton_Click(object? sender, EventArgs e)
        {
            try
            {
                var config = _configurationService.GetConfiguration();
                config.ClientId = _clientIdTextBox.Text;
                config.ClientSecret = _clientSecretTextBox.Text;
                config.Hotkeys = GetHotkeysFromUI();

                _saveConfigurationUseCase.Execute(config);
                
                MessageBox.Show("Configuration saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save configuration");
                MessageBox.Show($"Failed to save configuration: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AddHotkeyButton_Click(object? sender, EventArgs e)
        {
            var hotkeyDialog = new HotkeyConfigurationDialog();
            if (hotkeyDialog.ShowDialog() == DialogResult.OK)
            {
                var hotkey = hotkeyDialog.Hotkey;
                AddHotkeyEntryToUI(hotkey);
                _hotkeyManagementService?.AddHotkey(hotkey);
            }
        }

        private void AddHotkeyEntryToUI(Hotkey hotkey)
        {
            var entry = new HotkeyEntry(hotkey);
            entry.EditRequested += (s, e) => EditHotkey(hotkey);
            entry.DeleteRequested += (s, e) => DeleteHotkey(hotkey);

            _hotkeyEntries[hotkey.Id.ToString()] = entry;
            _hotkeyFlowPanel.Controls.Add(entry);
        }

        private void EditHotkey(Hotkey hotkey)
        {
            var hotkeyDialog = new HotkeyConfigurationDialog(hotkey);
            if (hotkeyDialog.ShowDialog() == DialogResult.OK)
            {
                var updatedHotkey = hotkeyDialog.Hotkey;
                _hotkeyManagementService?.UpdateHotkey(updatedHotkey);
                
                // Update UI
                if (_hotkeyEntries.TryGetValue(hotkey.Id.ToString(), out var entry))
                {
                    entry.UpdateHotkey(updatedHotkey);
                }
            }
        }

        private void DeleteHotkey(Hotkey hotkey)
        {
            var result = MessageBox.Show($"Are you sure you want to delete the hotkey '{hotkey.Description}'?", 
                "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            
            if (result == DialogResult.Yes)
            {
                _hotkeyManagementService?.RemoveHotkey(hotkey.Id);
                
                if (_hotkeyEntries.TryGetValue(hotkey.Id.ToString(), out var entry))
                {
                    _hotkeyFlowPanel.Controls.Remove(entry);
                    _hotkeyEntries.Remove(hotkey.Id.ToString());
                    entry.Dispose();
                }
            }
        }

        private List<Hotkey> GetHotkeysFromUI()
        {
            return _hotkeyEntries.Values.Select(entry => entry.Hotkey).ToList();
        }

        private void UpdateAuthenticationStatus()
        {
            if (_isAuthenticated)
            {
                _statusLabel.Text = "Authenticated ✓";
                _statusLabel.ForeColor = Color.Green;
            }
            else
            {
                _statusLabel.Text = "Not authenticated";
                _statusLabel.ForeColor = Color.Red;
            }
        }

        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(value);
            if (!value && _notifyIcon != null)
            {
                _notifyIcon.Visible = true;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                return;
            }

            _hotkeyManagementService?.Dispose();
            _notifyIcon?.Dispose();
            base.OnFormClosing(e);
        }
    }
}

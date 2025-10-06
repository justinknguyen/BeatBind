using System.Drawing;
using System.Linq;
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
        private TabControl _mainTabControl = null!;
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
        
        // Settings controls
        private CheckBox _startupCheckBox = null!;
        private CheckBox _minimizeCheckBox = null!;
        private CheckBox _rewindCheckBox = null!;
        private NumericUpDown _volumeStepsNumeric = null!;
        private NumericUpDown _seekMillisecondsNumeric = null!;

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
            Size = new Size(650, 550);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(600, 500);
            BackColor = Color.FromArgb(248, 249, 250);

            // Create modern tab control
            _mainTabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(10),
                Font = new Font("Segoe UI", 10f),
                Appearance = TabAppearance.Normal,
                SizeMode = TabSizeMode.Fixed,
                ItemSize = new Size(120, 35)
            };

            // Create tabs
            CreateAuthenticationTab();
            CreateHotkeysTab();
            CreateSettingsTab();

            // Create main layout for form
            var formLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.FromArgb(248, 249, 250),
                Padding = new Padding(15)
            };

            // Set row styles - tabs take most space, button takes fixed space
            formLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // Tabs
            formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60f)); // Save button

            // Tab control container
            var tabContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 249, 250)
            };
            tabContainer.Controls.Add(_mainTabControl);

            // Save button container
            var saveButtonContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 249, 250),
                Padding = new Padding(0, 10, 0, 0)
            };

            _saveConfigButton = new Button
            {
                Text = "💾 Save Configuration",
                Size = new Size(200, 40),
                Font = new Font("Segoe UI", 10f),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.None
            };
            _saveConfigButton.FlatAppearance.BorderSize = 0;
            _saveConfigButton.Click += SaveConfigButton_Click;

            // Center the button
            saveButtonContainer.Resize += (s, e) => {
                _saveConfigButton.Location = new Point(
                    (saveButtonContainer.Width - _saveConfigButton.Width) / 2,
                    (saveButtonContainer.Height - _saveConfigButton.Height) / 2
                );
            };

            saveButtonContainer.Controls.Add(_saveConfigButton);

            formLayout.Controls.Add(tabContainer, 0, 0);
            formLayout.Controls.Add(saveButtonContainer, 0, 1);
            
            Controls.Add(formLayout);

            ResumeLayout(false);
        }

        private void CreateAuthenticationTab()
        {
            var authTab = new TabPage("🔐 Authentication")
            {
                BackColor = Color.White,
                Padding = new Padding(15)
            };

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.White
            };

            // Set row styles to fit the available space
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 60f)); // Credentials
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 40f)); // Authentication & Status

            // Spotify Credentials Card
            var credentialsCard = CreateCompactCard("Spotify API Credentials", CreateCredentialsContentWithoutSaveButton());
            mainLayout.Controls.Add(credentialsCard, 0, 0);

            // Combined Authentication & Status Card
            var authStatusCard = CreateCompactCard("Authentication & Status", CreateCombinedAuthStatusContent());
            mainLayout.Controls.Add(authStatusCard, 0, 1);

            authTab.Controls.Add(mainLayout);
            _mainTabControl.TabPages.Add(authTab);
        }

        private void CreateHotkeysTab()
        {
            var hotkeysTab = new TabPage("⌨️ Hotkeys")
            {
                BackColor = Color.White,
                Padding = new Padding(20)
            };

            var scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White
            };

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 1,
                RowCount = 2,
                AutoSize = true,
                BackColor = Color.White
            };

            // Last Hotkey Status Card
            var lastHotkeyCard = CreateModernCard("Last Triggered Hotkey", CreateLastHotkeyContent());
            mainLayout.Controls.Add(lastHotkeyCard);

            // Hotkey Management Card
            var hotkeyCard = CreateModernCard("Hotkey Management", CreateHotkeyManagementContent());
            mainLayout.Controls.Add(hotkeyCard);

            scrollPanel.Controls.Add(mainLayout);
            hotkeysTab.Controls.Add(scrollPanel);
            _mainTabControl.TabPages.Add(hotkeysTab);
        }

        private void CreateSettingsTab()
        {
            var settingsTab = new TabPage("⚙️ Settings")
            {
                BackColor = Color.White,
                Padding = new Padding(15)
            };

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.White
            };

            // Set row styles to fit the available space
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 60f)); // Application Settings
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 30f)); // About

            // Application Settings Card
            var appSettingsCard = CreateCompactCard("Application Settings", CreateCompactAppSettingsContent());
            mainLayout.Controls.Add(appSettingsCard, 0, 0);

            // About Card
            var aboutCard = CreateCompactCard("About", CreateAboutContent());
            mainLayout.Controls.Add(aboutCard, 0, 1);

            settingsTab.Controls.Add(mainLayout);
            _mainTabControl.TabPages.Add(settingsTab);
        }

        private Panel CreateModernCard(string title, Control content)
        {
            var card = new Panel
            {
                Dock = DockStyle.Top,
                Height = content.Height + 60,
                Margin = new Padding(0, 0, 0, 15),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };

            // Add shadow effect (simplified)
            card.Paint += (s, e) =>
            {
                var rect = card.ClientRectangle;
                rect.Width -= 1;
                rect.Height -= 1;
                e.Graphics.DrawRectangle(new Pen(Color.FromArgb(220, 220, 220)), rect);
            };

            var titleLabel = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 37, 41),
                Dock = DockStyle.Top,
                Height = 35,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(15, 10, 15, 0),
                BackColor = Color.FromArgb(248, 249, 250)
            };

            var contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(15),
                BackColor = Color.White
            };
            contentPanel.Controls.Add(content);

            card.Controls.Add(contentPanel);
            card.Controls.Add(titleLabel);

            return card;
        }

        private Panel CreateCompactCard(string title, Control content)
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 8),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };

            // Add shadow effect (simplified)
            card.Paint += (s, e) =>
            {
                var rect = card.ClientRectangle;
                rect.Width -= 1;
                rect.Height -= 1;
                e.Graphics.DrawRectangle(new Pen(Color.FromArgb(220, 220, 220)), rect);
            };

            var titleLabel = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 37, 41),
                Dock = DockStyle.Top,
                Height = 25,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 5, 10, 0),
                BackColor = Color.FromArgb(248, 249, 250)
            };

            var contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                BackColor = Color.White
            };
            contentPanel.Controls.Add(content);

            card.Controls.Add(contentPanel);
            card.Controls.Add(titleLabel);

            return card;
        }

        private Control CreateCredentialsContentWithoutSaveButton()
        {
            var panel = new Panel { Dock = DockStyle.Fill };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(15)
            };

            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Client ID Label
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Client ID TextBox
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Client Secret Label
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Client Secret TextBox

            var clientIdLabel = new Label
            {
                Text = "Client ID",
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(73, 80, 87),
                Margin = new Padding(0, 0, 0, 5),
                AutoSize = true
            };

            _clientIdTextBox = new TextBox
            {
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 10f),
                BorderStyle = BorderStyle.FixedSingle,
                Height = 30,
                Margin = new Padding(0, 0, 0, 15)
            };

            var clientSecretLabel = new Label
            {
                Text = "Client Secret",
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(73, 80, 87),
                Margin = new Padding(0, 0, 0, 5),
                AutoSize = true
            };

            _clientSecretTextBox = new TextBox
            {
                Dock = DockStyle.Top,
                UseSystemPasswordChar = true,
                Font = new Font("Segoe UI", 10f),
                BorderStyle = BorderStyle.FixedSingle,
                Height = 30,
                Margin = new Padding(0, 0, 0, 0)
            };

            layout.Controls.Add(clientIdLabel, 0, 0);
            layout.Controls.Add(_clientIdTextBox, 0, 1);
            layout.Controls.Add(clientSecretLabel, 0, 2);
            layout.Controls.Add(_clientSecretTextBox, 0, 3);

            panel.Controls.Add(layout);
            return panel;
        }


        private Control CreateAuthenticationContent()
        {
            var panel = new Panel { Dock = DockStyle.Fill };

            _authenticateButton = new Button
            {
                Text = "🔗 Authenticate with Spotify",
                Size = new Size(200, 35),
                Font = new Font("Segoe UI", 9f),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 123, 255),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Dock = DockStyle.Left
            };
            _authenticateButton.FlatAppearance.BorderSize = 0;
            _authenticateButton.Click += AuthenticateButton_Click;

            panel.Controls.Add(_authenticateButton);
            return panel;
        }

        private Control CreateStatusContent()
        {
            var panel = new Panel { Dock = DockStyle.Fill };

            _statusLabel = new Label
            {
                Text = "Not authenticated",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(220, 53, 69),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };
            
            panel.Controls.Add(_statusLabel);
            return panel;
        }

        private Control CreateCombinedAuthStatusContent()
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15) };

            var statusContainer = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(248, 249, 250),
                Padding = new Padding(15)
            };

            _statusLabel = new Label
            {
                Text = "Not authenticated",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(220, 53, 69),
                Font = new Font("Segoe UI", 11f, FontStyle.Bold)
            };

            statusContainer.Controls.Add(_statusLabel);

            _authenticateButton = new Button
            {
                Text = "🔗 Authenticate with Spotify",
                Height = 40,
                Font = new Font("Segoe UI", 10f),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 123, 255),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 10, 0, 0)
            };
            _authenticateButton.FlatAppearance.BorderSize = 0;
            _authenticateButton.Click += AuthenticateButton_Click;

            panel.Controls.Add(_authenticateButton);
            panel.Controls.Add(statusContainer);

            return panel;
        }

        private Control CreateLastHotkeyContent()
        {
            var panel = new Panel { Height = 40, Dock = DockStyle.Top };

            _lastHotkeyLabel = new Label
            {
                Text = "No hotkey triggered yet",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10f),
                ForeColor = Color.FromArgb(108, 117, 125)
            };

            panel.Controls.Add(_lastHotkeyLabel);
            return panel;
        }

        private Control CreateHotkeyManagementContent()
        {
            var panel = new Panel { Height = 400, Dock = DockStyle.Top };

            _addHotkeyButton = new Button
            {
                Text = "➕ Add New Hotkey",
                Size = new Size(150, 35),
                Font = new Font("Segoe UI", 9f),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 123, 255),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Location = new Point(0, 8),
                Dock = DockStyle.Top,
                Margin = new Padding(0, 0, 0, 10)
            };
            _addHotkeyButton.FlatAppearance.BorderSize = 0;
            _addHotkeyButton.Click += AddHotkeyButton_Click;

            _hotkeyFlowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.White,
                Padding = new Padding(0, 5, 5, 5)
            };

            // Resize hotkey entries when the panel resizes
            _hotkeyFlowPanel.ClientSizeChanged += (s, e) =>
            {
                _hotkeyFlowPanel.SuspendLayout();
                var scrollbarWidth = _hotkeyFlowPanel.VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0;
                var availableWidth = _hotkeyFlowPanel.ClientSize.Width - _hotkeyFlowPanel.Padding.Horizontal - scrollbarWidth - 5;

                foreach (Control control in _hotkeyFlowPanel.Controls)
                {
                    if (control is HotkeyEntry)
                    {
                        control.Width = Math.Max(400, availableWidth);
                    }
                }
                _hotkeyFlowPanel.ResumeLayout(true);
            };

            panel.Controls.Add(_hotkeyFlowPanel);
            panel.Controls.Add(_addHotkeyButton);

            _hotkeyPanel = panel;
            return panel;
        }

        private Control CreateAppSettingsContent()
        {
            var panel = new Panel { Height = 280, Dock = DockStyle.Top };

            var scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                RowCount = 6,
                AutoSize = true,
                Padding = new Padding(0)
            };

            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));

            // General Settings
            var generalLabel = new Label
            {
                Text = "General Settings",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 37, 41),
                Dock = DockStyle.Top,
                Height = 25,
                Margin = new Padding(0, 10, 0, 5)
            };
            layout.Controls.Add(generalLabel, 0, 0);
            layout.SetColumnSpan(generalLabel, 2);

            _startupCheckBox = new CheckBox
            {
                Text = "Start with Windows",
                Font = new Font("Segoe UI", 9f),
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 5, 0, 5)
            };
            layout.Controls.Add(_startupCheckBox, 0, 1);

            _minimizeCheckBox = new CheckBox
            {
                Text = "Minimize to system tray",
                Font = new Font("Segoe UI", 9f),
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 5, 0, 5)
            };
            layout.Controls.Add(_minimizeCheckBox, 1, 1);

            // Audio Control Settings
            var audioLabel = new Label
            {
                Text = "Audio Control Settings",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 37, 41),
                Dock = DockStyle.Top,
                Height = 25,
                Margin = new Padding(0, 15, 0, 5)
            };
            layout.Controls.Add(audioLabel, 0, 2);
            layout.SetColumnSpan(audioLabel, 2);

            _rewindCheckBox = new CheckBox
            {
                Text = "Previous Track: rewind to start",
                Font = new Font("Segoe UI", 9f),
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 5, 0, 5),
                Checked = true
            };
            layout.Controls.Add(_rewindCheckBox, 0, 3);
            layout.SetColumnSpan(_rewindCheckBox, 2);

            // Volume Steps
            var volumeLabel = new Label
            {
                Text = "Volume Steps:",
                Font = new Font("Segoe UI", 9f),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 5, 0, 5)
            };
            layout.Controls.Add(volumeLabel, 0, 4);

            _volumeStepsNumeric = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 50,
                Value = 10,
                Font = new Font("Segoe UI", 9f),
                Dock = DockStyle.Left,
                Width = 80,
                Margin = new Padding(0, 5, 0, 5)
            };
            layout.Controls.Add(_volumeStepsNumeric, 1, 4);

            // Seek Milliseconds
            var seekLabel = new Label
            {
                Text = "Seek (ms):",
                Font = new Font("Segoe UI", 9f),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 5, 0, 5)
            };
            layout.Controls.Add(seekLabel, 0, 5);

            _seekMillisecondsNumeric = new NumericUpDown
            {
                Minimum = 1000,
                Maximum = 60000,
                Value = 10000,
                Increment = 1000,
                Font = new Font("Segoe UI", 9f),
                Dock = DockStyle.Left,
                Width = 80,
                Margin = new Padding(0, 5, 0, 5)
            };
            layout.Controls.Add(_seekMillisecondsNumeric, 1, 5);

            scrollPanel.Controls.Add(layout);
            panel.Controls.Add(scrollPanel);
            return panel;
        }

        private Control CreateAboutContent()
        {
            var panel = new Panel { Dock = DockStyle.Fill };

            var aboutLabel = new Label
            {
                Text = "BeatBind v1.0\nGlobal hotkeys for Spotify\n\nDeveloped with ❤️",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(108, 117, 125),
                TextAlign = ContentAlignment.TopLeft
            };

            panel.Controls.Add(aboutLabel);
            return panel;
        }

        private Control CreateCompactAppSettingsContent()
        {
            var panel = new Panel { Dock = DockStyle.Fill };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 5,
                Padding = new Padding(5),
                BackColor = Color.White
            };

            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // General Settings header
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Checkboxes
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Audio Settings header  
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Audio checkboxes
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Numeric controls

            // General Settings
            var generalLabel = new Label
            {
                Text = "General Settings",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 37, 41),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 3)
            };
            layout.Controls.Add(generalLabel, 0, 0);
            layout.SetColumnSpan(generalLabel, 2);

            var checkboxPanel1 = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Dock = DockStyle.Fill,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 8)
            };

            _startupCheckBox = new CheckBox
            {
                Text = "Start with Windows",
                Font = new Font("Segoe UI", 8f),
                AutoSize = true,
                Margin = new Padding(0, 0, 20, 0)
            };

            _minimizeCheckBox = new CheckBox
            {
                Text = "Minimize to tray",
                Font = new Font("Segoe UI", 8f),
                AutoSize = true
            };

            checkboxPanel1.Controls.Add(_startupCheckBox);
            checkboxPanel1.Controls.Add(_minimizeCheckBox);
            layout.Controls.Add(checkboxPanel1, 0, 1);
            layout.SetColumnSpan(checkboxPanel1, 2);

            // Audio Control Settings
            var audioLabel = new Label
            {
                Text = "Audio Control Settings",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 37, 41),
                AutoSize = true,
                Margin = new Padding(0, 5, 0, 3)
            };
            layout.Controls.Add(audioLabel, 0, 2);
            layout.SetColumnSpan(audioLabel, 2);

            _rewindCheckBox = new CheckBox
            {
                Text = "Previous Track: rewind to start",
                Font = new Font("Segoe UI", 8f),
                Checked = true,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 8)
            };
            layout.Controls.Add(_rewindCheckBox, 0, 3);
            layout.SetColumnSpan(_rewindCheckBox, 2);

            // Volume and Seek controls in a compact layout
            var controlsPanel = new TableLayoutPanel
            {
                ColumnCount = 4,
                RowCount = 2,
                Dock = DockStyle.Fill,
                AutoSize = true
            };
            controlsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            controlsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            controlsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            controlsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var volumeLabel = new Label
            {
                Text = "Volume Steps:",
                Font = new Font("Segoe UI", 8f),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = true,
                Margin = new Padding(0, 3, 5, 3)
            };

            _volumeStepsNumeric = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 50,
                Value = 10,
                Font = new Font("Segoe UI", 8f),
                Width = 60,
                Margin = new Padding(0, 3, 15, 3)
            };

            var seekLabel = new Label
            {
                Text = "Seek (ms):",
                Font = new Font("Segoe UI", 8f),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = true,
                Margin = new Padding(0, 3, 5, 3)
            };

            _seekMillisecondsNumeric = new NumericUpDown
            {
                Minimum = 1000,
                Maximum = 60000,
                Value = 10000,
                Increment = 1000,
                Font = new Font("Segoe UI", 8f),
                Width = 80,
                Margin = new Padding(0, 3, 0, 3)
            };

            controlsPanel.Controls.Add(volumeLabel, 0, 0);
            controlsPanel.Controls.Add(_volumeStepsNumeric, 1, 0);
            controlsPanel.Controls.Add(seekLabel, 2, 0);
            controlsPanel.Controls.Add(_seekMillisecondsNumeric, 3, 0);

            layout.Controls.Add(controlsPanel, 0, 4);
            layout.SetColumnSpan(controlsPanel, 2);

            panel.Controls.Add(layout);
            return panel;
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
                Size = new Size(50, 30),
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

                // Load application settings
                _startupCheckBox.Checked = config.StartMinimized;
                _minimizeCheckBox.Checked = config.MinimizeToTray;
                _rewindCheckBox.Checked = config.PreviousTrackRewindToStart;
                _volumeStepsNumeric.Value = config.VolumeSteps;
                _seekMillisecondsNumeric.Value = config.SeekMilliseconds;

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
            if (_hotkeyFlowPanel == null)
            {
                _logger.LogWarning("_hotkeyFlowPanel is null in LoadHotkeysFromConfiguration");
                return;
            }

            _hotkeyFlowPanel.SuspendLayout();
            _hotkeyFlowPanel.Controls.Clear();
            _hotkeyEntries.Clear();

            _logger.LogInformation($"Loading {hotkeys.Count} hotkeys from configuration");

            foreach (var hotkey in hotkeys)
            {
                AddHotkeyEntryToUI(hotkey);
            }

            _hotkeyFlowPanel.ResumeLayout(true);

            _logger.LogInformation($"Total controls in _hotkeyFlowPanel: {_hotkeyFlowPanel.Controls.Count}");
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
                
                // Save application settings
                config.StartMinimized = _startupCheckBox.Checked;
                config.MinimizeToTray = _minimizeCheckBox.Checked;
                config.PreviousTrackRewindToStart = _rewindCheckBox.Checked;
                config.VolumeSteps = (int)_volumeStepsNumeric.Value;
                config.SeekMilliseconds = (int)_seekMillisecondsNumeric.Value;

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

                // Check if a hotkey with the same action already exists
                var existingHotkey = GetHotkeysFromUI().FirstOrDefault(h => h.Action == hotkey.Action);
                if (existingHotkey != null)
                {
                    MessageBox.Show($"A hotkey for '{hotkey.Action}' already exists. Please edit or delete the existing hotkey first.",
                        "Duplicate Action", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

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

            // Ensure visibility
            entry.Visible = true;

            _hotkeyFlowPanel.Controls.Add(entry);

            // Debug: Log the addition
            _logger.LogInformation($"Added hotkey to UI: {hotkey.Action} with ID {hotkey.Id}. Size: {entry.Width}x{entry.Height}. Visible: {entry.Visible}. Parent: {entry.Parent != null}. Total controls: {_hotkeyFlowPanel.Controls.Count}");
            _logger.LogInformation($"FlowPanel size: {_hotkeyFlowPanel.Width}x{_hotkeyFlowPanel.Height}. Visible: {_hotkeyFlowPanel.Visible}");
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
            var result = MessageBox.Show($"Are you sure you want to delete the hotkey '{hotkey.Action}'?", 
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
            // Check if we have stored authentication
            bool hasStoredAuth = CheckStoredAuthentication();
            _isAuthenticated = hasStoredAuth;

            if (_isAuthenticated)
            {
                _statusLabel.Text = "Authenticated ✓";
                _statusLabel.ForeColor = Color.FromArgb(40, 167, 69);
                _authenticateButton.Text = "🔗 Re-authenticate";
            }
            else
            {
                _statusLabel.Text = "Not authenticated";
                _statusLabel.ForeColor = Color.FromArgb(220, 53, 69);
                _authenticateButton.Text = "🔗 Authenticate with Spotify";
            }
        }

        private bool CheckStoredAuthentication()
        {
            try
            {
                // Check if the music control service (which uses SpotifyService) has valid authentication
                var config = _configurationService.GetConfiguration();
                
                if (!string.IsNullOrEmpty(config.AccessToken) && !string.IsNullOrEmpty(config.RefreshToken))
                {
                    // Check if token is still valid or can be refreshed
                    if (config.TokenExpiresAt > DateTime.UtcNow.AddMinutes(5)) // 5 minute buffer
                    {
                        _logger.LogInformation("Found valid stored authentication");
                        return true;
                    }
                    else if (!string.IsNullOrEmpty(config.RefreshToken))
                    {
                        _logger.LogInformation("Found stored authentication with refresh token available");
                        return true; // Will be refreshed automatically by SpotifyService
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking stored authentication");
                return false;
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

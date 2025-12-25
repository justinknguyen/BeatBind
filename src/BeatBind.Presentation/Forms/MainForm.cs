using System.ComponentModel;
using BeatBind.Application.Services;
using BeatBind.Application.Commands;
using BeatBind.Core.Entities;
using BeatBind.Core.Interfaces;
using BeatBind.Presentation.Themes;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MaterialSkin;
using MaterialSkin.Controls;

namespace BeatBind.Presentation.Forms
{
    public partial class MainForm : MaterialForm
    {
        private readonly MaterialSkinManager _materialSkinManager;
        private readonly MusicControlService _musicControlService;
        private HotkeyManagementService _hotkeyManagementService = null!;
        private readonly IMediator _mediator;
        private readonly IConfigurationService _configurationService;
        private readonly IGithubReleaseService _githubReleaseService;
        private readonly ILogger<MainForm> _logger;
        private const string CURRENT_VERSION = "2.0.0";
        
        private NotifyIcon? _notifyIcon;
        private bool _isAuthenticated;
        private Panel? _updateNotificationPanel;

        // UI Controls
        private MaterialTabControl _mainTabControl = null!;
        private MaterialTextBox _clientIdTextBox = null!;
        private MaterialTextBox _clientSecretTextBox = null!;
        private MaterialButton _authenticateButton = null!;
        private MaterialLabel _statusLabel = null!;
        private Panel _hotkeyPanel = null!;
        private MaterialLabel _lastHotkeyLabel = null!;
        private FlowLayoutPanel _hotkeyFlowPanel = null!;
        private MaterialButton _addHotkeyButton = null!;
        private MaterialButton _saveConfigButton = null!;
        private Dictionary<string, HotkeyEntry> _hotkeyEntries = new();
        
        // Settings controls
        private MaterialCheckbox _startupCheckBox = null!;
        private MaterialCheckbox _minimizeCheckBox = null!;
        private MaterialCheckbox _rewindCheckBox = null!;
        private NumericUpDown _volumeStepsNumeric = null!;
        private NumericUpDown _seekMillisecondsNumeric = null!;

        // Parameterless constructor for WinForms designer support
        public MainForm()
        {
            _materialSkinManager = MaterialSkinManager.Instance;
            _musicControlService = null!;
            _mediator = null!;
            _configurationService = null!;
            _githubReleaseService = null!;
            _logger = NullLogger<MainForm>.Instance;

            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            {
                InitializeComponent();
            }
            else
            {
                throw new InvalidOperationException("This constructor is for the WinForms designer only. Use DI constructor.");
            }
        }

        public MainForm(
            MusicControlService musicControlService,
            IMediator mediator,
            IConfigurationService configurationService,
            IGithubReleaseService githubReleaseService,
            ILogger<MainForm> logger)
        {
            _musicControlService = musicControlService;
            _mediator = mediator;
            _configurationService = configurationService;
            _githubReleaseService = githubReleaseService;
            _logger = logger;

            // Initialize MaterialSkinManager
            _materialSkinManager = MaterialSkinManager.Instance;
            _materialSkinManager.AddFormToManage(this);
            _materialSkinManager.Theme = MaterialSkinManager.Themes.DARK;
            _materialSkinManager.ColorScheme = new ColorScheme(
                Primary.Green700, Primary.Green900,
                Primary.Green500, Accent.LightGreen200,
                TextShade.WHITE
            );

            InitializeComponent();
            SetupNotifyIcon();
            LoadConfiguration();
            UpdateAuthenticationStatus();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            // Force a theme refresh after the form is fully loaded
            ApplyTheme();
            // Check for updates
            _ = CheckForUpdatesAsync();
        }

        public void SetHotkeyManagementService(HotkeyManagementService hotkeyManagementService)
        {
            _hotkeyManagementService = hotkeyManagementService;

            // Subscribe to hotkey triggered events
            _hotkeyManagementService.HotkeyTriggered += OnHotkeyTriggered;

            // Initialize hotkeys from configuration once the service is set
            _hotkeyManagementService.InitializeHotkeys();
        }

        private void OnHotkeyTriggered(object? sender, Hotkey hotkey)
        {
            // Update UI on the UI thread
            if (InvokeRequired)
            {
                Invoke(() => OnHotkeyTriggered(sender, hotkey));
                return;
            }

            _lastHotkeyLabel.Text = $"{hotkey.Action}";
            _lastHotkeyLabel.ForeColor = Theme.Info;
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            // Form settings
            Text = "BeatBind - Spotify Global Hotkeys";
            Size = new Size(700, 650);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(650, 600);

            // Create MaterialTabControl FIRST
            _mainTabControl = new MaterialTabControl
            {
                Dock = DockStyle.Fill,
                Depth = 0,
                MouseState = MaterialSkin.MouseState.HOVER
            };

            // Now create tabs (they need _mainTabControl to exist)
            CreateHotkeysTab();
            CreateAuthenticationTab();
            CreateSettingsTab();

            // Create MaterialTabSelector for the tab headers
            var tabSelector = new MaterialTabSelector
            {
                BaseTabControl = _mainTabControl,
                Depth = 0,
                Dock = DockStyle.Top,
                Height = 48
            };

            // Create main layout for form
            var formLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(0)
            };

            // Set row styles
            formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48f)); // Tab selector
            formLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // Tabs
            formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70f)); // Save button

            // Tab control container
            var tabContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(15)
            };
            tabContainer.Controls.Add(_mainTabControl);

            // Save button container
            var saveButtonContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 10, 0, 10)
            };

            _saveConfigButton = new MaterialButton
            {
                Text = "SAVE CONFIGURATION",
                Size = new Size(220, 45),
                Type = MaterialButton.MaterialButtonType.Contained,
                Depth = 0,
                Anchor = AnchorStyles.None,
                UseAccentColor = false,
                AutoSize = false,
                Cursor = Cursors.Hand
            };
            _saveConfigButton.Click += SaveConfigButton_Click;

            // Center the button
            saveButtonContainer.Resize += (s, e) => {
                _saveConfigButton.Location = new Point(
                    (saveButtonContainer.Width - _saveConfigButton.Width) / 2,
                    (saveButtonContainer.Height - _saveConfigButton.Height) / 2
                );
            };

            saveButtonContainer.Controls.Add(_saveConfigButton);

            formLayout.Controls.Add(tabSelector, 0, 0);
            formLayout.Controls.Add(tabContainer, 0, 1);
            formLayout.Controls.Add(saveButtonContainer, 0, 2);
            
            Controls.Add(formLayout);

            ResumeLayout(false);
        }

        private void CreateAuthenticationTab()
        {
            var authTab = new TabPage("🔐 Authentication")
            {
                BackColor = Theme.CardBackground,
                Padding = new Padding(15)
            };

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Theme.CardBackground
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
                BackColor = Theme.CardBackground,
                Padding = new Padding(20)
            };

            var scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Theme.CardBackground
            };

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 1,
                RowCount = 2,
                AutoSize = true,
                BackColor = Theme.CardBackground
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
                BackColor = Theme.CardBackground,
                Padding = new Padding(15)
            };

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Theme.CardBackground
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
                BackColor = Theme.CardBackground,
                BorderStyle = BorderStyle.None
            };

            // Add shadow effect (simplified)
            card.Paint += (s, e) =>
            {
                var rect = card.ClientRectangle;
                rect.Width -= 1;
                rect.Height -= 1;
                e.Graphics.DrawRectangle(new Pen(Theme.Border), rect);
            };

            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Theme.HeaderBackground,
                Tag = "headerPanel",
                Padding = new Padding(15, 0, 15, 0)
            };

            var titleLabel = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Theme.PrimaryText,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent,
                Tag = "headerLabel"
            };
            headerPanel.Controls.Add(titleLabel);

            var contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(15, 15, 15, 15),
                BackColor = Theme.CardBackground
            };
            contentPanel.Controls.Add(content);

            // WinForms Docking Order:
            // Controls are docked in reverse Z-order. The control at the bottom of the Z-order (highest index) is docked first.
            // We want Header (Top) to be docked first, so it takes the top slice.
            // We want Content (Fill) to be docked last, so it fills the remaining space.
            
            // Add controls to the collection
            card.Controls.Add(contentPanel);
            card.Controls.Add(headerPanel);

            // Ensure correct Z-order for docking
            // Header needs to be at the bottom of Z-order (docked first) -> SendToBack()
            // Content needs to be at the top of Z-order (docked last) -> BringToFront()
            headerPanel.SendToBack();
            contentPanel.BringToFront();

            return card;
        }

        private Panel CreateCompactCard(string title, Control content)
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 8),
                BackColor = Theme.CardBackground,
                BorderStyle = BorderStyle.None
            };

            // Add shadow effect (simplified)
            card.Paint += (s, e) =>
            {
                var rect = card.ClientRectangle;
                rect.Width -= 1;
                rect.Height -= 1;
                e.Graphics.DrawRectangle(new Pen(Theme.Border), rect);
            };

            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 30,
                BackColor = Theme.HeaderBackground,
                Tag = "headerPanel",
                Padding = new Padding(10, 0, 10, 0)
            };

            var titleLabel = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Theme.PrimaryText,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent,
                Tag = "headerLabel"
            };
            headerPanel.Controls.Add(titleLabel);

            var contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                BackColor = Theme.CardBackground
            };
            contentPanel.Controls.Add(content);

            card.Controls.Add(contentPanel);
            card.Controls.Add(headerPanel);

            // Ensure correct Z-order for docking
            headerPanel.SendToBack();
            contentPanel.BringToFront();

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
                ForeColor = Theme.LabelText,
                Margin = new Padding(0, 0, 0, 5),
                AutoSize = true
            };

            _clientIdTextBox = new MaterialTextBox
            {
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 10f),
                Height = 48,
                Margin = new Padding(0, 0, 0, 15),
                Hint = "Enter your Spotify Client ID"
            };

            var clientSecretLabel = new Label
            {
                Text = "Client Secret",
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Theme.LabelText,
                Margin = new Padding(0, 0, 0, 5),
                AutoSize = true
            };

            _clientSecretTextBox = new MaterialTextBox
            {
                Dock = DockStyle.Top,
                Password = true,
                Font = new Font("Segoe UI", 10f),
                Height = 48,
                Margin = new Padding(0, 0, 0, 0),
                Hint = "Enter your Spotify Client Secret"
            };

            layout.Controls.Add(clientIdLabel, 0, 0);
            layout.Controls.Add(_clientIdTextBox, 0, 1);
            layout.Controls.Add(clientSecretLabel, 0, 2);
            layout.Controls.Add(_clientSecretTextBox, 0, 3);

            panel.Controls.Add(layout);
            return panel;
        }

        private Control CreateCombinedAuthStatusContent()
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15, 15, 15, 20) };

            var statusContainer = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Theme.HeaderBackground,
                Padding = new Padding(5)
            };

            _statusLabel = new MaterialLabel
            {
                Text = "Not authenticated",
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                HighEmphasis = true
            };

            statusContainer.Controls.Add(_statusLabel);

            _authenticateButton = new MaterialButton
            {
                Text = "AUTHENTICATE WITH SPOTIFY",
                Height = 45,
                Type = MaterialButton.MaterialButtonType.Contained,
                Depth = 0,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 10, 0, 10),
                UseAccentColor = false,
                AutoSize = false,
                Cursor = Cursors.Hand
            };
            _authenticateButton.Click += AuthenticateButton_Click;

            panel.Controls.Add(_authenticateButton);
            panel.Controls.Add(statusContainer);

            return panel;
        }

        private Control CreateLastHotkeyContent()
        {
            var panel = new Panel { Height = 40, Dock = DockStyle.Top };

            _lastHotkeyLabel = new MaterialLabel
            {
                Text = "No hotkey triggered yet",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10f),
                HighEmphasis = false
            };

            panel.Controls.Add(_lastHotkeyLabel);
            return panel;
        }

        private Control CreateHotkeyManagementContent()
        {
            var panel = new Panel { Height = 400, Dock = DockStyle.Top };

            _addHotkeyButton = new MaterialButton
            {
                Text = "ADD NEW HOTKEY",
                Size = new Size(180, 40),
                Type = MaterialButton.MaterialButtonType.Contained,
                Depth = 0,
                Location = new Point(0, 8),
                Dock = DockStyle.Top,
                Margin = new Padding(0, 0, 0, 10),
                UseAccentColor = false,
                AutoSize = false,
                Cursor = Cursors.Hand
            };
            _addHotkeyButton.Click += AddHotkeyButton_Click;

            _hotkeyFlowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Theme.CardBackground,
                Padding = new Padding(0, 5, 5, 5)
            };
            _hotkeyFlowPanel.HorizontalScroll.Enabled = false;
            _hotkeyFlowPanel.HorizontalScroll.Visible = false;

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

        private Control CreateAboutContent()
        {
            var panel = new Panel { Dock = DockStyle.Fill };

            var aboutLabel = new LinkLabel
            {
                Text = "BeatBind v2.0\nGlobal hotkeys for Spotify\n\nhttps://github.com/justinknguyen/BeatBind",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9f),
                LinkColor = Color.LightBlue,
                ActiveLinkColor = Color.DeepSkyBlue,
                VisitedLinkColor = Color.CornflowerBlue,
                ForeColor = Theme.SecondaryText,
                TextAlign = ContentAlignment.TopLeft
            };

            // Define the clickable link region
            int linkStart = aboutLabel.Text.IndexOf("https://");
            int linkLength = aboutLabel.Text.Length - linkStart;
            aboutLabel.Links.Add(linkStart, linkLength, "https://github.com/justinknguyen/BeatBind");

            // Handle link click
            aboutLabel.LinkClicked += (s, e) =>
            {
                if (e.Link?.LinkData is string url)
                {
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = url,
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to open link: {ex.Message}", "Error", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
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
                BackColor = Theme.CardBackground
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
                ForeColor = Theme.PrimaryText,
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

            _startupCheckBox = new MaterialCheckbox
            {
                Text = "Start with Windows",
                AutoSize = true,
                Depth = 0,
                Margin = new Padding(0, 0, 20, 0)
            };

            _minimizeCheckBox = new MaterialCheckbox
            {
                Text = "Minimize to tray",
                AutoSize = true,
                Depth = 0,
                Margin = new Padding(0, 0, 20, 0)
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
                ForeColor = Theme.PrimaryText,
                AutoSize = true,
                Margin = new Padding(0, 5, 0, 3)
            };
            layout.Controls.Add(audioLabel, 0, 2);
            layout.SetColumnSpan(audioLabel, 2);

            _rewindCheckBox = new MaterialCheckbox
            {
                Text = "Previous Track: rewind to start",
                Checked = true,
                AutoSize = true,
                Depth = 0,
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
                Margin = new Padding(0, 3, 5, 3),
                ForeColor = Theme.PrimaryText
            };

            _volumeStepsNumeric = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 50,
                Value = 10,
                Font = new Font("Segoe UI", 8f),
                Width = 60,
                Margin = new Padding(0, 3, 15, 3),
                BackColor = Theme.InputBackground,
                ForeColor = Theme.PrimaryText
            };

            var seekLabel = new Label
            {
                Text = "Seek (ms):",
                Font = new Font("Segoe UI", 8f),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = true,
                Margin = new Padding(0, 3, 5, 3),
                ForeColor = Theme.PrimaryText
            };

            _seekMillisecondsNumeric = new NumericUpDown
            {
                Minimum = 1000,
                Maximum = 60000,
                Value = 10000,
                Increment = 1000,
                Font = new Font("Segoe UI", 8f),
                Width = 80,
                Margin = new Padding(0, 3, 0, 3),
                BackColor = Theme.InputBackground,
                ForeColor = Theme.PrimaryText
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

                // Apply current theme
                ApplyTheme();

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
            _authenticateButton.Enabled = false;
            _authenticateButton.Text = "Authenticating...";

            try
            {
                // Save credentials first if provided
                if (!string.IsNullOrEmpty(_clientIdTextBox.Text) && !string.IsNullOrEmpty(_clientSecretTextBox.Text))
                {
                    await _mediator.Send(new UpdateClientCredentialsCommand(_clientIdTextBox.Text, _clientSecretTextBox.Text));
                }

                var result = await _mediator.Send(new AuthenticateUserCommand());
                
                _isAuthenticated = result.IsSuccess;
                UpdateAuthenticationStatus();

                var message = result.IsSuccess
                    ? "Authentication successful!"
                    : $"Authentication failed. {result.Error}";
                
                var icon = result.IsSuccess ? MessageBoxIcon.Information : MessageBoxIcon.Error;
                var title = result.IsSuccess ? "Success" : "Error";
                
                MessageBox.Show(message, title, MessageBoxButtons.OK, icon);
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

        private async void SaveConfigButton_Click(object? sender, EventArgs e)
        {
            try
            {
                var config = _configurationService.GetConfiguration();
                config.ClientId = _clientIdTextBox.Text;
                config.ClientSecret = _clientSecretTextBox.Text;
                config.Hotkeys = GetHotkeysFromUI();
                config.StartMinimized = _startupCheckBox.Checked;
                config.MinimizeToTray = _minimizeCheckBox.Checked;
                config.PreviousTrackRewindToStart = _rewindCheckBox.Checked;
                config.VolumeSteps = (int)_volumeStepsNumeric.Value;
                config.SeekMilliseconds = (int)_seekMillisecondsNumeric.Value;

                var result = await _mediator.Send(new SaveConfigurationCommand(config));

                var message = result.IsSuccess
                    ? "Configuration saved successfully!"
                    : $"Failed to save configuration: {result.Error}";
                
                var icon = result.IsSuccess ? MessageBoxIcon.Information : MessageBoxIcon.Error;
                var title = result.IsSuccess ? "Success" : "Error";
                
                MessageBox.Show(message, title, MessageBoxButtons.OK, icon);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save configuration");
                MessageBox.Show($"Failed to save configuration: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplyTheme()
        {
            // Form background
            BackColor = Theme.FormBackground;

            // Tab control
            _mainTabControl.BackColor = Theme.FormBackground;
            foreach (TabPage tab in _mainTabControl.TabPages)
            {
                tab.BackColor = Theme.CardBackground;
            }

            // Ensure headers use the correct color
            UpdateControlColors(this);
        }

        private void UpdateControlColors(Control control)
        {
            foreach (Control child in control.Controls)
            {
                if (child is Panel panel)
                {
                    if (panel.Tag?.ToString() == "headerPanel")
                    {
                        panel.BackColor = Theme.HeaderBackground;
                    }
                    // Ensure panels that are not the main background are card background
                    // This mimics the old behavior of fixing up "light" panels
                    else if (panel.BackColor.R > 200 && panel.BackColor.G > 200 && panel.BackColor.B > 200)
                    {
                        panel.BackColor = Theme.CardBackground;
                    }
                }
                else if (child is Label label)
                {
                    if (label.Tag?.ToString() == "headerLabel")
                    {
                        label.ForeColor = Theme.PrimaryText;
                        label.BackColor = Color.Transparent;
                    }
                    else if (label.Tag?.ToString() == "header")
                    {
                        label.BackColor = Theme.HeaderBackground;
                        label.ForeColor = Theme.PrimaryText;
                    }
                    else
                    {
                        // Ensure other labels are visible against the dark background
                        // Only update if they are not explicitly set to a specific color (like success/error)
                        // This is a heuristic to match the old behavior but safer
                        if (label.ForeColor.R < 100 && label.ForeColor.G < 100 && label.ForeColor.B < 100)
                        {
                            label.ForeColor = Theme.PrimaryText;
                        }
                    }
                }

                if (child.HasChildren)
                {
                    UpdateControlColors(child);
                }
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
                _statusLabel.ForeColor = Theme.Success;
                _authenticateButton.Text = "🔗 Re-authenticate";
            }
            else
            {
                _statusLabel.Text = "Not authenticated";
                _statusLabel.ForeColor = Theme.Error;
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

            if (_hotkeyManagementService != null)
            {
                _hotkeyManagementService.HotkeyTriggered -= OnHotkeyTriggered;
                _hotkeyManagementService.Dispose();
            }

            _notifyIcon?.Dispose();
            base.OnFormClosing(e);
        }

        private async Task CheckForUpdatesAsync()
        {
            try
            {
                var latestRelease = await _githubReleaseService.GetLatestReleaseAsync();
                
                if (latestRelease == null)
                {
                    _logger.LogWarning("Could not fetch latest release information");
                    return;
                }

                if (_githubReleaseService.IsNewerVersion(CURRENT_VERSION, latestRelease.Version))
                {
                    ShowUpdateNotification(latestRelease);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for updates");
            }
        }

        private void ShowUpdateNotification(GithubRelease release)
        {
            if (InvokeRequired)
            {
                Invoke(() => ShowUpdateNotification(release));
                return;
            }

            // Create update notification panel
            _updateNotificationPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.FromArgb(76, 175, 80),
                Padding = new Padding(15, 10, 15, 10)
            };

            var messageLabel = new Label
            {
                Text = $"🎉 New version available: v{release.Version}",
                AutoSize = false,
                Dock = DockStyle.Left,
                Width = 300,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var downloadButton = new Button
            {
                Text = "Download",
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(76, 175, 80),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Dock = DockStyle.Left,
                Width = 100,
                Height = 30
            };
            downloadButton.FlatAppearance.BorderSize = 0;
            downloadButton.Click += (s, e) => {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = release.Url,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to open release URL");
                    MessageBox.Show("Failed to open the download page. Please visit the GitHub releases page manually.",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            var closeButton = new Button
            {
                Text = "✕",
                AutoSize = false,
                Width = 30,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Dock = DockStyle.Right
            };
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.Click += (s, e) => {
                Controls.Remove(_updateNotificationPanel);
                _updateNotificationPanel?.Dispose();
                _updateNotificationPanel = null;
            };

            _updateNotificationPanel.Controls.Add(downloadButton);
            _updateNotificationPanel.Controls.Add(messageLabel);
            _updateNotificationPanel.Controls.Add(closeButton);

            Controls.Add(_updateNotificationPanel);
            _updateNotificationPanel.BringToFront();
        }
    }
}

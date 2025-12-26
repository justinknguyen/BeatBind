using System.ComponentModel;
using BeatBind.Application.Services;
using BeatBind.Core.Entities;
using BeatBind.Core.Interfaces;
using BeatBind.Presentation.Themes;
using BeatBind.Presentation.Panels;
using BeatBind.Presentation.Components;
using BeatBind.Presentation.Helpers;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MaterialSkin;
using MaterialSkin.Controls;

namespace BeatBind.Presentation
{
    public partial class MainForm : MaterialForm
    {
        private readonly MaterialSkinManager _materialSkinManager;
        private readonly MusicControlApplicationService _musicControlService;
        private readonly AuthenticationApplicationService _authenticationService;
        private HotkeyApplicationService _hotkeyApplicationService = null!;
        private readonly IMediator _mediator;
        private readonly IConfigurationService _configurationService;
        private readonly IGithubReleaseService _githubReleaseService;
        private readonly ILogger<MainForm> _logger;
        private const string CURRENT_VERSION = "2.0.0";
        
        private NotifyIcon? _notifyIcon;
        private Panel? _updateNotificationPanel;
        private bool _isExiting;

        // UI Controls
        private MaterialTabControl _mainTabControl = null!;
        private MaterialButton _saveConfigButton = null!;
        
        // Panel controls
        private AuthenticationPanel _authenticationPanel = null!;
        private HotkeysPanel _hotkeysPanel = null!;
        private SettingsPanel _settingsPanel = null!;

        // Parameterless constructor for WinForms designer support
        public MainForm()
        {
            _materialSkinManager = MaterialSkinManager.Instance;
            _musicControlService = null!;
            _authenticationService = null!;
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
            MusicControlApplicationService musicControlService,
            AuthenticationApplicationService authenticationService,
            IMediator mediator,
            IConfigurationService configurationService,
            IGithubReleaseService githubReleaseService,
            ILogger<MainForm> logger)
        {
            _musicControlService = musicControlService;
            _authenticationService = authenticationService;
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
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            // Force a theme refresh after the form is fully loaded
            ApplyTheme();
            // Check for updates
            _ = CheckForUpdatesAsync();
        }

        public void SetHotkeyApplicationService(HotkeyApplicationService hotkeyApplicationService)
        {
            _hotkeyApplicationService = hotkeyApplicationService;

            // Subscribe to hotkey triggered events
            _hotkeyApplicationService.HotkeyTriggered += OnHotkeyTriggered;

            // Initialize hotkeys from configuration once the service is set
            _hotkeyApplicationService.InitializeHotkeys();
        }

        private void OnHotkeyTriggered(object? sender, Hotkey hotkey)
        {
            _hotkeysPanel?.UpdateLastHotkeyLabel($"{hotkey.Action}");
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

            // Create MaterialTabControl
            _mainTabControl = new MaterialTabControl
            {
                Dock = DockStyle.Fill,
                Depth = 0,
                MouseState = MaterialSkin.MouseState.HOVER
            };

            // Create panels
            _authenticationPanel = new AuthenticationPanel(_authenticationService, _configurationService, NullLogger<AuthenticationPanel>.Instance);
            _hotkeysPanel = new HotkeysPanel(null, NullLogger<HotkeysPanel>.Instance);
            _settingsPanel = new SettingsPanel(_configurationService, NullLogger<SettingsPanel>.Instance);

            // Wire up panel events
            _hotkeysPanel.HotkeyEditRequested += HotkeysPanel_HotkeyEditRequested;
            _hotkeysPanel.HotkeyDeleteRequested += HotkeysPanel_HotkeyDeleteRequested;
            _hotkeysPanel.HotkeyAdded += HotkeysPanel_HotkeyAdded;

            // Create tabs
            var hotkeysTab = new TabPage("⌨️ Hotkeys")
            {
                BackColor = Theme.CardBackground
            };
            hotkeysTab.Controls.Add(_hotkeysPanel);
            
            var authTab = new TabPage("🔐 Authentication")
            {
                BackColor = Theme.CardBackground
            };
            authTab.Controls.Add(_authenticationPanel);
            
            var settingsTab = new TabPage("⚙️ Settings")
            {
                BackColor = Theme.CardBackground
            };
            settingsTab.Controls.Add(_settingsPanel);

            _mainTabControl.TabPages.Add(hotkeysTab);
            _mainTabControl.TabPages.Add(authTab);
            _mainTabControl.TabPages.Add(settingsTab);

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

            // Center the button using ThemeHelper
            saveButtonContainer.Resize += (s, e) => 
            {
                ThemeHelper.CenterControl(_saveConfigButton, saveButtonContainer);
            };

            saveButtonContainer.Controls.Add(_saveConfigButton);

            formLayout.Controls.Add(tabSelector, 0, 0);
            formLayout.Controls.Add(tabContainer, 0, 1);
            formLayout.Controls.Add(saveButtonContainer, 0, 2);
            
            Controls.Add(formLayout);

            ResumeLayout(false);
        }

        private void HotkeysPanel_HotkeyEditRequested(object? sender, Hotkey hotkey)
        {
            var hotkeyDialog = new HotkeyEditorDialog(hotkey);
            if (hotkeyDialog.ShowDialog() == DialogResult.OK)
            {
                var updatedHotkey = hotkeyDialog.Hotkey;
                _hotkeyApplicationService?.UpdateHotkey(updatedHotkey);
                _hotkeysPanel?.UpdateHotkeyEntry(updatedHotkey);
            }
        }

        private void HotkeysPanel_HotkeyDeleteRequested(object? sender, Hotkey hotkey)
        {
            if (MessageBoxHelper.ConfirmDelete(hotkey.Action.ToString(), "hotkey"))
            {
                _hotkeyApplicationService?.RemoveHotkey(hotkey.Id);
                _hotkeysPanel?.RemoveHotkeyEntry(hotkey.Id);
            }
        }

        private void HotkeysPanel_HotkeyAdded(object? sender, EventArgs e)
        {
            // Optional: Handle any additional logic when a hotkey is added
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
            contextMenu.Items.Add("Exit", null, (s, e) => 
            {
                _isExiting = true;
                System.Windows.Forms.Application.Exit();
            });

            _notifyIcon.ContextMenuStrip = contextMenu;
            _notifyIcon.DoubleClick += (s, e) => { Show(); WindowState = FormWindowState.Normal; };
        }

        private void LoadConfiguration()
        {
            try
            {
                var config = _configurationService.GetConfiguration();
                
                // Load configuration into panels
                _authenticationPanel.LoadConfiguration();
                _settingsPanel.LoadConfiguration();
                _hotkeysPanel.LoadHotkeys(config.Hotkeys);

                // Apply current theme
                ApplyTheme();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load configuration");
                MessageBoxHelper.ShowWarning("Failed to load configuration. Using defaults.", "Configuration Error");
            }
        }

        private async void SaveConfigButton_Click(object? sender, EventArgs e)
        {
            try
            {
                var config = _authenticationPanel.GetConfiguration();
                _settingsPanel.ApplySettingsToConfiguration(config);
                config.Hotkeys = _hotkeysPanel.GetHotkeysFromUI();

                _configurationService.SaveConfiguration(config);

                MessageBoxHelper.ShowSuccess("Configuration saved successfully!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save configuration");
                MessageBoxHelper.ShowException(ex, "saving configuration");
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

            // Use ThemeHelper to apply theme consistently
            ThemeHelper.ApplyThemeToControlHierarchy(this);
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
            if (e.CloseReason == CloseReason.UserClosing && !_isExiting)
            {
                e.Cancel = true;
                Hide();
                return;
            }

            if (_hotkeyApplicationService != null)
            {
                _hotkeyApplicationService.HotkeyTriggered -= OnHotkeyTriggered;
                _hotkeyApplicationService.Dispose();
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
            downloadButton.Click += (s, e) => 
            {
                MessageBoxHelper.OpenUrl(release.Url, ex => _logger.LogError(ex, "Failed to open release URL"));
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
            closeButton.Click += (s, e) => 
            {
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

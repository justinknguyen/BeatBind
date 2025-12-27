using System.ComponentModel;
using BeatBind.Application.Services;
using BeatBind.Core.Entities;
using BeatBind.Core.Interfaces;
using BeatBind.Infrastructure.Helpers;
using BeatBind.Presentation.Components;
using BeatBind.Presentation.Helpers;
using BeatBind.Presentation.Panels;
using BeatBind.Presentation.Themes;
using MaterialSkin;
using MaterialSkin.Controls;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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
        private bool _startMinimized;

        // UI Controls
        private MaterialTabControl _mainTabControl = null!;
        private MaterialButton _saveConfigButton = null!;

        // Panel controls
        private AuthenticationPanel _authenticationPanel = null!;
        private HotkeysPanel _hotkeysPanel = null!;
        private SettingsPanel _settingsPanel = null!;

        /// <summary>
        /// Parameterless constructor for WinForms designer support.
        /// </summary>
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

        /// <summary>
        /// Initializes a new instance of the MainForm with dependency injection.
        /// </summary>
        /// <param name="musicControlService">Service for music control operations</param>
        /// <param name="authenticationService">Service for authentication operations</param>
        /// <param name="mediator">Mediator for command/query handling</param>
        /// <param name="configurationService">Service for configuration management</param>
        /// <param name="githubReleaseService">Service for checking GitHub releases</param>
        /// <param name="logger">Logger instance</param>
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
            ApplyStartupSettings();
        }

        /// <summary>
        /// Called when the form is first shown. Applies theme and checks for updates.
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            // Force a theme refresh after the form is fully loaded
            ApplyTheme();
            // Check for updates
            _ = CheckForUpdatesAsync();
        }

        /// <summary>
        /// Sets the hotkey application service and subscribes to hotkey events.
        /// </summary>
        /// <param name="hotkeyApplicationService">The hotkey application service instance</param>
        public void SetHotkeyApplicationService(HotkeyApplicationService hotkeyApplicationService)
        {
            _hotkeyApplicationService = hotkeyApplicationService;

            // Subscribe to hotkey triggered events
            _hotkeyApplicationService.HotkeyTriggered += OnHotkeyTriggered;

            // Initialize hotkeys from configuration once the service is set
            _hotkeyApplicationService.InitializeHotkeys();
        }

        /// <summary>
        /// Handles hotkey triggered events and updates the UI.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="hotkey">The hotkey that was triggered</param>
        private void OnHotkeyTriggered(object? sender, Hotkey hotkey)
        {
            _hotkeysPanel?.UpdateLastHotkeyLabel($"{hotkey.Action}");
        }

        private const int WM_POWERBROADCAST = 0x218;
        private const int PBT_APMSUSPEND = 0x4;
        private const int PBT_APMRESUMEAUTOMATIC = 0x12;

        /// <summary>
        /// Overrides WndProc to handle power mode changes (sleep/resume) directly from the message loop.
        /// This ensures the hook is removed/added on the correct thread and at the right time.
        /// </summary>
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_POWERBROADCAST)
            {
                switch (m.WParam.ToInt32())
                {
                    case PBT_APMSUSPEND:
                        _logger.LogInformation("System suspending (WM_POWERBROADCAST), pausing hotkey service");
                        _hotkeyApplicationService?.Pause();
                        break;
                    case PBT_APMRESUMEAUTOMATIC:
                        _logger.LogInformation("System resuming (WM_POWERBROADCAST), resuming hotkey service");
                        _hotkeyApplicationService?.Resume();
                        break;
                }
            }
            base.WndProc(ref m);
        }

        /// <summary>
        /// Initializes all form components and creates the UI layout.
        /// </summary>
        private void InitializeComponent()
        {
            SuspendLayout();

            // Form settings
            Text = "BeatBind - Spotify Global Hotkeys";
            Size = new Size(700, 800);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(650, 700);

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

            _hotkeysPanel.ConfigurationChanged += OnConfigurationChanged;
            _authenticationPanel.ConfigurationChanged += OnConfigurationChanged;
            _settingsPanel.ConfigurationChanged += OnConfigurationChanged;

            // Create tabs
            var hotkeysTab = new TabPage("⌨️ Hotkeys")
            {
                BackColor = Theme.CardBackground,
                ForeColor = Color.White
            };
            hotkeysTab.Controls.Add(_hotkeysPanel);

            var authTab = new TabPage("🔐 Authentication")
            {
                BackColor = Theme.CardBackground,
                ForeColor = Color.White
            };
            authTab.Controls.Add(_authenticationPanel);

            var settingsTab = new TabPage("⚙️ Settings")
            {
                BackColor = Theme.CardBackground,
                ForeColor = Color.White
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
                Cursor = Cursors.Hand,
                Enabled = false
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

        /// <summary>
        /// Handles hotkey edit requests from the hotkeys panel.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="hotkey">The hotkey to edit</param>
        private void HotkeysPanel_HotkeyEditRequested(object? sender, Hotkey hotkey)
        {
            var hotkeyDialog = new HotkeyEditorDialog(hotkey);
            if (hotkeyDialog.ShowDialog() == DialogResult.OK)
            {
                var updatedHotkey = hotkeyDialog.Hotkey;
                _hotkeysPanel?.UpdateHotkeyEntry(updatedHotkey);
            }
        }

        /// <summary>
        /// Handles hotkey delete requests from the hotkeys panel.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="hotkey">The hotkey to delete</param>
        private void HotkeysPanel_HotkeyDeleteRequested(object? sender, Hotkey hotkey)
        {
            if (MessageBoxHelper.ConfirmDelete(hotkey.Action.ToString(), "hotkey"))
            {
                _hotkeysPanel?.RemoveHotkeyEntry(hotkey.Id);
            }
        }

        /// <summary>
        /// Handles hotkey added events from the hotkeys panel.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void HotkeysPanel_HotkeyAdded(object? sender, EventArgs e)
        {
            // Optional: Handle any additional logic when a hotkey is added
        }

        /// <summary>
        /// Configures the system tray notification icon and context menu.
        /// </summary>
        private void SetupNotifyIcon()
        {
            // Load icon from embedded resources or use default
            Icon? appIcon = null;
            try
            {
                // Try to load the embedded icon resource
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                // Find the resource name that ends with icon.ico to handle namespace variations
                var resourceName = assembly.GetManifestResourceNames()
                    .FirstOrDefault(n => n.EndsWith("icon.ico"));

                if (!string.IsNullOrEmpty(resourceName))
                {
                    using (var iconStream = assembly.GetManifestResourceStream(resourceName))
                    {
                        if (iconStream != null)
                        {
                            appIcon = new Icon(iconStream);
                            _logger.LogInformation("Successfully loaded application icon from embedded resources: {ResourceName}", resourceName);
                        }
                    }
                }

                if (appIcon == null)
                {
                    _logger.LogWarning("Icon resource not found in assembly. Available resources: {Resources}", string.Join(", ", assembly.GetManifestResourceNames()));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load application icon from resources");
            }

            // If we couldn't load from resources, use the system default
            if (appIcon == null)
            {
                _logger.LogInformation("Using system default application icon");
                appIcon = SystemIcons.Application;
            }

            // Set the form icon
            Icon = appIcon;

            _notifyIcon = new NotifyIcon
            {
                Icon = appIcon,
                Text = "BeatBind - Spotify Global Hotkeys",
                Visible = true
            };

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Show", null, (s, e) =>
            {
                Show();
                WindowState = FormWindowState.Normal;
                Activate();
                BringToFront();
            });
            contextMenu.Items.Add("Exit", null, (s, e) =>
            {
                _isExiting = true;
                System.Windows.Forms.Application.Exit();
            });

            _notifyIcon.ContextMenuStrip = contextMenu;
            _notifyIcon.DoubleClick += (s, e) =>
            {
                Show();
                WindowState = FormWindowState.Normal;
                Activate();
                BringToFront();
            };

            _logger.LogInformation("System tray icon initialized successfully");
        }

        /// <summary>
        /// Loads application configuration and initializes all panels with saved settings.
        /// </summary>
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

        /// <summary>
        /// Applies startup settings including starting minimized to tray.
        /// </summary>
        private void ApplyStartupSettings()
        {
            try
            {
                var config = _configurationService.GetConfiguration();

                // If StartMinimized is enabled, start the app minimized to system tray
                if (config.StartMinimized)
                {
                    _logger.LogInformation("Starting minimized to system tray");
                    _startMinimized = true;
                    // Check for updates since OnShown won't be called immediately
                    _ = CheckForUpdatesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply startup settings");
            }
        }

        /// <summary>
        /// Handles save configuration button click event. Saves all settings and reloads hotkeys.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private async void SaveConfigButton_Click(object? sender, EventArgs e)
        {
            try
            {
                var config = _authenticationPanel.GetConfiguration();
                _settingsPanel.ApplySettingsToConfiguration(config);
                config.Hotkeys = _hotkeysPanel.GetHotkeysFromUI();

                _configurationService.SaveConfiguration(config);

                // Apply Windows startup setting
                StartupHelper.SetStartupWithWindows(config.StartWithWindows, _logger);

                // Reload hotkeys to ensure they're properly registered
                _hotkeyApplicationService?.ReloadHotkeys();

                // Update original values in panels to match saved config
                _authenticationPanel.LoadConfiguration();
                _settingsPanel.LoadConfiguration();
                _hotkeysPanel.LoadHotkeys(config.Hotkeys);

                _saveConfigButton.Enabled = false;
                MessageBoxHelper.ShowSuccess("Configuration saved successfully!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save configuration");
                MessageBoxHelper.ShowException(ex, "saving configuration");
            }
        }

        /// <summary>
        /// Handles configuration changes from any panel.
        /// </summary>
        private void OnConfigurationChanged(object? sender, EventArgs e)
        {
            _saveConfigButton.Enabled = _hotkeysPanel.HasUnsavedChanges() ||
                                      _authenticationPanel.HasUnsavedChanges() ||
                                      _settingsPanel.HasUnsavedChanges();
        }

        /// <summary>
        /// Applies the application theme to all UI elements.
        /// </summary>
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

        /// <summary>
        /// Controls form visibility and manages system tray icon visibility.
        /// </summary>
        /// <param name="value">Whether the form should be visible</param>
        protected override void SetVisibleCore(bool value)
        {
            if (_startMinimized && value)
            {
                value = false;
                _startMinimized = false;
            }

            base.SetVisibleCore(value);
            if (!value && _notifyIcon != null)
            {
                _notifyIcon.Visible = true;
            }
        }

        /// <summary>
        /// Handles form closing event. Minimizes to tray unless explicitly exiting.
        /// </summary>
        /// <param name="e">Form closing event arguments</param>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            var config = _configurationService.GetConfiguration();
            if (e.CloseReason == CloseReason.UserClosing && !_isExiting && config.MinimizeToTray)
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

        /// <summary>
        /// Checks for application updates from GitHub releases.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
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

        /// <summary>
        /// Displays an update notification banner at the top of the form.
        /// </summary>
        /// <param name="release">The GitHub release information</param>
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

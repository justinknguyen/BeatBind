using BeatBind.Application.Services;
using BeatBind.Core.Entities;
using BeatBind.Core.Interfaces;
using BeatBind.Presentation.Helpers;
using BeatBind.Presentation.Themes;
using MaterialSkin.Controls;
using Microsoft.Extensions.Logging;

namespace BeatBind.Presentation.Panels;

public partial class SettingsPanel : BasePanelControl
{
    private readonly IConfigurationService _configurationService;
    private readonly IStartupService _startupService;
    private readonly MusicControlApplicationService _musicControlService;

    private MaterialCheckbox _startupCheckBox = null!;
    private MaterialCheckbox _minimizeCheckBox = null!;
    private MaterialCheckbox _minimizeToTrayCheckBox = null!;
    private MaterialCheckbox _rewindCheckBox = null!;
    private NumericUpDown _volumeStepsNumeric = null!;
    private NumericUpDown _seekMillisecondsNumeric = null!;
    private ComboBox _deviceComboBox = null!;
    private TextBox _deviceIdTextBox = null!;
    private MaterialButton _refreshDevicesButton = null!;
    private bool _isLoading;

    // Guards the two-way sync between the device drop-down and the id text box so
    // updating one does not look like the user editing the other.
    private bool _syncingDeviceSelection;
    private string _favoriteDeviceName = string.Empty;

    private bool _originalStartup;
    private bool _originalMinimize;
    private bool _originalMinimizeToTray;
    private bool _originalRewind;
    private int _originalVolumeSteps;
    private int _originalSeekMilliseconds;
    private string _originalFavoriteDeviceId = string.Empty;
    private string _originalFavoriteDeviceName = string.Empty;

    public event EventHandler? ConfigurationChanged;

    /// <summary>
    /// Initializes a new instance of the SettingsPanel with dependency injection.
    /// </summary>
    /// <param name="configurationService">Service for configuration management</param>
    /// <param name="startupService">Service for startup management</param>
    /// <param name="musicControlService">Service used to list the account's Spotify devices</param>
    /// <param name="logger">Logger instance</param>
    public SettingsPanel(
        IConfigurationService configurationService,
        IStartupService startupService,
        MusicControlApplicationService musicControlService,
        ILogger<SettingsPanel> logger)
        : base(logger)
    {
        _configurationService = configurationService;
        _startupService = startupService;
        _musicControlService = musicControlService;
    }

    /// <summary>
    /// Parameterless constructor for WinForms designer support.
    /// </summary>
    public SettingsPanel() : base()
    {
        _configurationService = null!;
        _startupService = null!;
        _musicControlService = null!;
    }

    /// <summary>
    /// Initializes the UI layout and controls for the settings panel.
    /// </summary>
    protected override void InitializeUI()
    {
        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Theme.CardBackground
        };

        // About needs room for its header plus four lines including the project URL;
        // the settings card scrolls, so it tolerates the smaller share
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 73f)); // Application Settings
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 27f)); // About

        // Use CardFactory for consistent card creation
        var appSettingsCard = CardFactory.CreateCompactCard("Application Settings", CreateAppSettingsContent());
        mainLayout.Controls.Add(appSettingsCard, 0, 0);

        var aboutCard = CardFactory.CreateCompactCard("About", CreateAboutContent());
        mainLayout.Controls.Add(aboutCard, 0, 1);

        Controls.Add(mainLayout);
    }

    /// <summary>
    /// Creates the application settings section with general and audio control options.
    /// </summary>
    /// <returns>A control containing application settings controls</returns>
    private Control CreateAppSettingsContent()
    {
        // Deliberately not AutoSize: this panel is the scroll viewport, and AutoSize
        // would make it grow to fit its content instead of scrolling, which is what
        // clipped the rows below the device picker. The inner layout auto-sizes instead.
        var panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowOnly,
            ColumnCount = 2,
            RowCount = 8,
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
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Playback device header
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Playback device controls
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Folder link

        // General Settings - Use ControlFactory
        var generalLabel = ControlFactory.CreateHeaderLabel("General Settings");
        layout.Controls.Add(generalLabel, 0, 0);
        layout.SetColumnSpan(generalLabel, 2);

        var checkboxPanel1 = ControlFactory.CreateFlowPanel();
        _startupCheckBox = ControlFactory.CreateMaterialCheckbox("Start with Windows");
        _minimizeCheckBox = ControlFactory.CreateMaterialCheckbox("Start Minimized");
        _minimizeToTrayCheckBox = ControlFactory.CreateMaterialCheckbox("Minimize to Tray");
        checkboxPanel1.Controls.Add(_startupCheckBox);
        checkboxPanel1.Controls.Add(_minimizeCheckBox);
        checkboxPanel1.Controls.Add(_minimizeToTrayCheckBox);
        layout.Controls.Add(checkboxPanel1, 0, 1);
        layout.SetColumnSpan(checkboxPanel1, 2);

        // Audio Control Settings - Use ControlFactory
        var audioLabel = ControlFactory.CreateHeaderLabel("Audio Control Settings");
        audioLabel.Margin = new Padding(0, 15, 0, 0);
        layout.Controls.Add(audioLabel, 0, 2);
        layout.SetColumnSpan(audioLabel, 2);

        _rewindCheckBox = ControlFactory.CreateMaterialCheckbox("Previous Track: restart if playback exceeds 5 seconds", isChecked: true);
        _rewindCheckBox.Margin = new Padding(0, 0, 0, 8);
        layout.Controls.Add(_rewindCheckBox, 0, 3);
        layout.SetColumnSpan(_rewindCheckBox, 2);

        // Volume and Seek controls
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

        // Use ControlFactory for labels and numeric controls
        var volumeLabel = new Label
        {
            Text = "Volume Steps:",
            Font = new Font("Segoe UI", 8f),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = true,
            Margin = new Padding(0, 3, 5, 3),
            ForeColor = Theme.PrimaryText
        };

        _volumeStepsNumeric = ControlFactory.CreateNumericUpDown(1, 50, 10, width: 60);
        _volumeStepsNumeric.Margin = new Padding(0, 3, 15, 3);

        var seekLabel = new Label
        {
            Text = "Seek (ms):",
            Font = new Font("Segoe UI", 8f),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = true,
            Margin = new Padding(0, 3, 5, 3),
            ForeColor = Theme.PrimaryText
        };

        _seekMillisecondsNumeric = ControlFactory.CreateNumericUpDown(1000, 60000, 10000, increment: 1000);

        controlsPanel.Controls.Add(volumeLabel, 0, 0);
        controlsPanel.Controls.Add(_volumeStepsNumeric, 1, 0);
        controlsPanel.Controls.Add(seekLabel, 2, 0);
        controlsPanel.Controls.Add(_seekMillisecondsNumeric, 3, 0);

        layout.Controls.Add(controlsPanel, 0, 4);
        layout.SetColumnSpan(controlsPanel, 2);

        // Playback Device Settings
        var deviceLabel = ControlFactory.CreateHeaderLabel("Playback Device");
        deviceLabel.Margin = new Padding(0, 10, 0, 0);
        layout.Controls.Add(deviceLabel, 0, 5);
        layout.SetColumnSpan(deviceLabel, 2);

        var devicePanel = CreateDeviceContent();
        layout.Controls.Add(devicePanel, 0, 6);
        layout.SetColumnSpan(devicePanel, 2);

        // Folder link for config and log files
        var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BeatBind");
        var folderLink = new LinkLabel
        {
            Text = "View config and log files folder",
            Font = new Font("Segoe UI", 9f),
            LinkColor = Color.LightBlue,
            ActiveLinkColor = Color.DeepSkyBlue,
            VisitedLinkColor = Color.CornflowerBlue,
            AutoSize = true,
            Margin = new Padding(0, 12, 0, 5),
            TextAlign = ContentAlignment.MiddleLeft
        };
        folderLink.Links.Clear();
        folderLink.Links.Add(0, folderLink.Text.Length, appDataPath);
        folderLink.LinkClicked += (s, e) =>
        {
            if (e.Link?.LinkData is string path)
            {
                try
                {
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = path,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    LogError(ex, "Failed to open folder");
                    MessageBoxHelper.ShowError($"Failed to open folder:\n{ex.Message}");
                }
            }
        };
        layout.Controls.Add(folderLink, 0, 7);
        layout.SetColumnSpan(folderLink, 2);

        // Subscribe to changes
        _startupCheckBox.CheckedChanged += (s, e) => { if (!_isLoading) { ConfigurationChanged?.Invoke(this, EventArgs.Empty); } };
        _minimizeCheckBox.CheckedChanged += (s, e) => { if (!_isLoading) { ConfigurationChanged?.Invoke(this, EventArgs.Empty); } };
        _minimizeToTrayCheckBox.CheckedChanged += (s, e) => { if (!_isLoading) { ConfigurationChanged?.Invoke(this, EventArgs.Empty); } };
        _rewindCheckBox.CheckedChanged += (s, e) => { if (!_isLoading) { ConfigurationChanged?.Invoke(this, EventArgs.Empty); } };
        _volumeStepsNumeric.ValueChanged += (s, e) => { if (!_isLoading) { ConfigurationChanged?.Invoke(this, EventArgs.Empty); } };
        _seekMillisecondsNumeric.ValueChanged += (s, e) => { if (!_isLoading) { ConfigurationChanged?.Invoke(this, EventArgs.Empty); } };

        panel.Controls.Add(layout);
        return panel;
    }

    /// <summary>
    /// Creates the favorite-device picker: a drop-down of the account's Spotify
    /// devices, a refresh button, and a manual Device ID box for a device that never
    /// shows up in the list.
    /// </summary>
    /// <returns>A control containing the device selection controls</returns>
    private Control CreateDeviceContent()
    {
        var devicePanel = new TableLayoutPanel
        {
            ColumnCount = 3,
            RowCount = 2,
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowOnly
        };
        devicePanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        devicePanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        devicePanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var favoriteLabel = new Label
        {
            Text = "Favorite Device:",
            Font = new Font("Segoe UI", 8f),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = true,
            Margin = new Padding(0, 7, 5, 2),
            ForeColor = Theme.PrimaryText
        };

        _deviceComboBox = ControlFactory.CreateComboBox(width: 220);
        _deviceComboBox.AccessibleName = "Favorite device";
        _deviceComboBox.AccessibleDescription = "Spotify device that playback is sent to by the Play/Pause On Favorite Device hotkey";

        _refreshDevicesButton = ControlFactory.CreateMaterialButton("Refresh Devices", width: 140, height: 32);
        _refreshDevicesButton.Margin = new Padding(0, 3, 0, 3);
        _refreshDevicesButton.AccessibleName = "Refresh devices";
        _refreshDevicesButton.AccessibleDescription = "Ask Spotify for the devices currently available on this account";

        var deviceIdLabel = new Label
        {
            Text = "Device ID:",
            Font = new Font("Segoe UI", 8f),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = true,
            Margin = new Padding(0, 5, 5, 2),
            ForeColor = Theme.PrimaryText
        };

        _deviceIdTextBox = new TextBox
        {
            Font = new Font("Segoe UI", 8f),
            Width = 220,
            Margin = new Padding(0, 3, 15, 3),
            BackColor = Theme.InputBackground,
            ForeColor = Theme.PrimaryText,
            BorderStyle = BorderStyle.FixedSingle,
            AccessibleName = "Device ID",
            AccessibleDescription = "Spotify device identifier, for a device that does not appear in the list"
        };

        devicePanel.Controls.Add(favoriteLabel, 0, 0);
        devicePanel.Controls.Add(_deviceComboBox, 1, 0);
        devicePanel.Controls.Add(_refreshDevicesButton, 2, 0);
        devicePanel.Controls.Add(deviceIdLabel, 0, 1);
        devicePanel.Controls.Add(_deviceIdTextBox, 1, 1);

        _deviceComboBox.SelectedIndexChanged += DeviceComboBox_SelectedIndexChanged;
        _deviceIdTextBox.TextChanged += DeviceIdTextBox_TextChanged;
        _refreshDevicesButton.Click += RefreshDevicesButton_Click;

        PopulateDeviceOptions(Array.Empty<Device>());
        return devicePanel;
    }

    /// <summary>
    /// Fetches the account's currently visible Spotify devices and rebuilds the
    /// drop-down. Only ever runs on demand, since it needs authentication and costs
    /// an API round trip.
    /// </summary>
    private async void RefreshDevicesButton_Click(object? sender, EventArgs e)
    {
        _refreshDevicesButton.Enabled = false;
        try
        {
            var devices = await _musicControlService.GetAvailableDevicesAsync();
            PopulateDeviceOptions(devices);

            if (devices.Count == 0)
            {
                // GetAvailableDevicesAsync swallows every failure into an empty list, so
                // this covers "not authenticated" and network errors as well as "no devices"
                MessageBoxHelper.ShowWarning(
                    "No Spotify devices were returned.\n\nCheck that you are authenticated on the Authentication tab, " +
                    "then open Spotify on the device and play something before refreshing again. " +
                    "You can also paste a Device ID directly if the device never appears.");
            }
        }
        catch (Exception ex)
        {
            LogError(ex, "Failed to refresh devices");
            MessageBoxHelper.ShowError($"Failed to get devices:\n{ex.Message}");
        }
        finally
        {
            _refreshDevicesButton.Enabled = true;
        }
    }

    /// <summary>
    /// Rebuilds the device drop-down, keeping the saved device selectable even when
    /// it is offline and therefore missing from the list Spotify returned.
    /// </summary>
    /// <param name="devices">The devices Spotify currently reports</param>
    private void PopulateDeviceOptions(IReadOnlyCollection<Device> devices)
    {
        var selectedId = NormalizeText(_deviceIdTextBox.Text);
        var nameBeforeRefresh = _favoriteDeviceName;
        var wasLoading = _isLoading;
        _isLoading = true;
        try
        {
            _deviceComboBox.Items.Clear();
            _deviceComboBox.Items.Add(new DeviceOption(string.Empty, string.Empty, "(none - let Spotify decide)"));

            var known = false;
            foreach (var device in devices)
            {
                // Spotify documents device.id as nullable; such a device cannot be targeted
                if (string.IsNullOrWhiteSpace(device.Id))
                {
                    continue;
                }

                var label = string.IsNullOrWhiteSpace(device.Type)
                    ? device.Name
                    : $"{device.Name} ({device.Type})";
                _deviceComboBox.Items.Add(new DeviceOption(device.Id, device.Name, label));
                known |= string.Equals(device.Id, selectedId, StringComparison.Ordinal);
            }

            if (selectedId.Length > 0 && !known)
            {
                var savedLabel = _favoriteDeviceName.Length > 0 ? _favoriteDeviceName : selectedId;
                _deviceComboBox.Items.Add(new DeviceOption(selectedId, _favoriteDeviceName, $"{savedLabel} (saved)"));
            }

            SelectDeviceById(selectedId);

            // Adopt the name Spotify currently reports, so a device chosen by pasting
            // an id can still be re-resolved by name if that id later rotates
            if (_deviceComboBox.SelectedItem is DeviceOption selected && selected.Id.Length > 0)
            {
                _favoriteDeviceName = selected.Name;
            }
        }
        finally
        {
            _isLoading = wasLoading;
        }

        // Adopting a name Spotify reports is a real change to what would be saved, so it
        // has to enable the save button; nothing else in this method raises the event
        if (!_isLoading && _favoriteDeviceName != nameBeforeRefresh)
        {
            ConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Selects the drop-down entry for the given device id, falling back to the
    /// "none" entry when the id is blank or unknown.
    /// </summary>
    private void SelectDeviceById(string deviceId)
    {
        _syncingDeviceSelection = true;
        try
        {
            // Drop any entry left over from a previous keystroke before deciding again
            for (int i = _deviceComboBox.Items.Count - 1; i >= 0; i--)
            {
                if (_deviceComboBox.Items[i] is DeviceOption stale && stale.IsManual)
                {
                    _deviceComboBox.Items.RemoveAt(i);
                }
            }

            for (int i = 0; i < _deviceComboBox.Items.Count; i++)
            {
                if (_deviceComboBox.Items[i] is DeviceOption option
                    && string.Equals(option.Id, deviceId, StringComparison.Ordinal))
                {
                    _deviceComboBox.SelectedIndex = i;
                    return;
                }
            }

            if (deviceId.Length > 0)
            {
                // A hand-entered id Spotify has not listed: show it rather than "(none)",
                // which would contradict what the user just typed
                _deviceComboBox.Items.Add(new DeviceOption(deviceId, _favoriteDeviceName, $"{deviceId} (entered)", isManual: true));
                _deviceComboBox.SelectedIndex = _deviceComboBox.Items.Count - 1;
                return;
            }

            _deviceComboBox.SelectedIndex = _deviceComboBox.Items.Count > 0 ? 0 : -1;
        }
        finally
        {
            _syncingDeviceSelection = false;
        }
    }

    /// <summary>
    /// Copies the picked device into the id text box, which is what gets saved.
    /// </summary>
    private void DeviceComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_syncingDeviceSelection || _deviceComboBox.SelectedItem is not DeviceOption option)
        {
            return;
        }

        _favoriteDeviceName = option.Name;
        _syncingDeviceSelection = true;
        try
        {
            _deviceIdTextBox.Text = option.Id;
        }
        finally
        {
            _syncingDeviceSelection = false;
        }

        // TextChanged only fires when the text actually differs, so picking an entry whose
        // id is already in the box would otherwise change the name with the button disabled
        if (!_isLoading)
        {
            ConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Keeps the drop-down in step with a hand-edited device id.
    /// </summary>
    private void DeviceIdTextBox_TextChanged(object? sender, EventArgs e)
    {
        if (!_syncingDeviceSelection)
        {
            // A hand-typed id carries no trustworthy display name
            _favoriteDeviceName = string.Empty;
            SelectDeviceById(NormalizeText(_deviceIdTextBox.Text));
        }

        if (!_isLoading)
        {
            ConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Trims a possibly null configuration or control value to a non-null string.
    /// </summary>
    private static string NormalizeText(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Creates the about section with application information and links.
    /// </summary>
    /// <returns>A control containing about information</returns>
    private Control CreateAboutContent()
    {
        var panel = new Panel { Dock = DockStyle.Fill };

        var aboutLabel = new LinkLabel
        {
            Text = $"BeatBind v{MainForm.CURRENT_VERSION}\nGlobal Hotkeys for Spotify\n\nhttps://github.com/justinknguyen/BeatBind",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9f),
            LinkColor = Color.LightBlue,
            ActiveLinkColor = Color.DeepSkyBlue,
            VisitedLinkColor = Color.CornflowerBlue,
            ForeColor = Theme.SecondaryText,
            TextAlign = ContentAlignment.TopLeft
        };

        int linkStart = aboutLabel.Text.IndexOf("https://");
        int linkLength = aboutLabel.Text.Length - linkStart;
        aboutLabel.Links.Add(linkStart, linkLength, "https://github.com/justinknguyen/BeatBind");

        aboutLabel.LinkClicked += (s, e) =>
        {
            if (e.Link?.LinkData is string url)
            {
                MessageBoxHelper.OpenUrl(url, ex => LogError(ex, "Failed to open link"));
            }
        };

        panel.Controls.Add(aboutLabel);
        return panel;
    }

    /// <summary>
    /// Loads saved configuration values into the UI controls.
    /// </summary>
    public void LoadConfiguration()
    {
        _isLoading = true;
        try
        {
            var config = _configurationService.GetConfiguration();

            // Sync the StartWithWindows checkbox with actual registry state
            var isInStartup = _startupService.IsInStartup();
            _startupCheckBox.Checked = isInStartup || config.StartWithWindows;

            _minimizeCheckBox.Checked = config.StartMinimized;
            _minimizeToTrayCheckBox.Checked = config.MinimizeToTray;
            _rewindCheckBox.Checked = config.PreviousTrackRewindToStart;
            _volumeStepsNumeric.Value = config.VolumeSteps;
            _seekMillisecondsNumeric.Value = config.SeekMilliseconds;

            _favoriteDeviceName = NormalizeText(config.FavoriteDeviceName);
            _syncingDeviceSelection = true;
            try
            {
                _deviceIdTextBox.Text = NormalizeText(config.FavoriteDeviceId);
            }
            finally
            {
                _syncingDeviceSelection = false;
            }

            // Rebuild the drop-down so the saved device is shown without calling the
            // API; the user refreshes explicitly to discover live devices
            PopulateDeviceOptions(Array.Empty<Device>());

            // Save original values
            _originalStartup = _startupCheckBox.Checked;
            _originalMinimize = config.StartMinimized;
            _originalMinimizeToTray = config.MinimizeToTray;
            _originalRewind = config.PreviousTrackRewindToStart;
            _originalVolumeSteps = config.VolumeSteps;
            _originalSeekMilliseconds = config.SeekMilliseconds;
            _originalFavoriteDeviceId = NormalizeText(_deviceIdTextBox.Text);
            _originalFavoriteDeviceName = _favoriteDeviceName;
        }
        catch (Exception ex)
        {
            LogError(ex, "Failed to load configuration");
        }
        finally
        {
            _isLoading = false;
        }
    }

    /// <summary>
    /// Checks if there are any unsaved changes in the panel.
    /// </summary>
    /// <returns>True if there are unsaved changes, false otherwise</returns>
    public bool HasUnsavedChanges()
    {
        return _startupCheckBox.Checked != _originalStartup ||
               _minimizeCheckBox.Checked != _originalMinimize ||
               _minimizeToTrayCheckBox.Checked != _originalMinimizeToTray ||
               _rewindCheckBox.Checked != _originalRewind ||
               _volumeStepsNumeric.Value != _originalVolumeSteps ||
               _seekMillisecondsNumeric.Value != _originalSeekMilliseconds ||
               NormalizeText(_deviceIdTextBox.Text) != _originalFavoriteDeviceId ||
               _favoriteDeviceName != _originalFavoriteDeviceName;
    }

    /// <summary>
    /// Applies the current UI settings to the provided configuration object.
    /// </summary>
    /// <param name="config">The configuration object to update</param>
    public void ApplySettingsToConfiguration(ApplicationConfiguration config)
    {
        config.StartWithWindows = _startupCheckBox.Checked;
        config.StartMinimized = _minimizeCheckBox.Checked;
        config.MinimizeToTray = _minimizeToTrayCheckBox.Checked;
        config.PreviousTrackRewindToStart = _rewindCheckBox.Checked;
        config.VolumeSteps = (int)_volumeStepsNumeric.Value;
        config.SeekMilliseconds = (int)_seekMillisecondsNumeric.Value;
        config.FavoriteDeviceId = NormalizeText(_deviceIdTextBox.Text);
        config.FavoriteDeviceName = _favoriteDeviceName;
    }

    /// <summary>
    /// A device drop-down entry. A named type rather than an anonymous object so the
    /// refresh and read-back paths can share it.
    /// </summary>
    private sealed class DeviceOption
    {
        public DeviceOption(string id, string name, string display, bool isManual = false)
        {
            Id = id;
            Name = name;
            Display = display;
            IsManual = isManual;
        }

        public string Id { get; }

        public string Name { get; }

        /// <summary>True for a placeholder built from a hand-entered id, not from Spotify.</summary>
        public bool IsManual { get; }

        private string Display { get; }

        public override string ToString()
        {
            return Display;
        }
    }
}

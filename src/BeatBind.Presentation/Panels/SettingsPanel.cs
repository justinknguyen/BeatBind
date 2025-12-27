using BeatBind.Core.Entities;
using BeatBind.Core.Interfaces;
using BeatBind.Infrastructure.Helpers;
using BeatBind.Presentation.Helpers;
using BeatBind.Presentation.Themes;
using MaterialSkin.Controls;
using Microsoft.Extensions.Logging;

namespace BeatBind.Presentation.Panels;

public partial class SettingsPanel : BasePanelControl
{
    private readonly IConfigurationService _configurationService;

    private MaterialCheckbox _startupCheckBox = null!;
    private MaterialCheckbox _minimizeCheckBox = null!;
    private MaterialCheckbox _minimizeToTrayCheckBox = null!;
    private MaterialCheckbox _rewindCheckBox = null!;
    private NumericUpDown _volumeStepsNumeric = null!;
    private NumericUpDown _seekMillisecondsNumeric = null!;
    private bool _isLoading;

    private bool _originalStartup;
    private bool _originalMinimize;
    private bool _originalMinimizeToTray;
    private bool _originalRewind;
    private int _originalVolumeSteps;
    private int _originalSeekMilliseconds;

    public event EventHandler? ConfigurationChanged;

    /// <summary>
    /// Initializes a new instance of the SettingsPanel with dependency injection.
    /// </summary>
    /// <param name="configurationService">Service for configuration management</param>
    /// <param name="logger">Logger instance</param>
    public SettingsPanel(IConfigurationService configurationService, ILogger<SettingsPanel> logger)
        : base(logger)
    {
        _configurationService = configurationService;
    }

    /// <summary>
    /// Parameterless constructor for WinForms designer support.
    /// </summary>
    public SettingsPanel() : base()
    {
        _configurationService = null!;
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

        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 60f)); // Application Settings
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 40f)); // About

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

        // General Settings - Use ControlFactory
        var generalLabel = ControlFactory.CreateHeaderLabel("General Settings");
        layout.Controls.Add(generalLabel, 0, 0);
        layout.SetColumnSpan(generalLabel, 2);

        var checkboxPanel1 = ControlFactory.CreateFlowPanel();
        _startupCheckBox = ControlFactory.CreateMaterialCheckbox("Start with Windows");
        _minimizeCheckBox = ControlFactory.CreateMaterialCheckbox("Start minimized");
        _minimizeToTrayCheckBox = ControlFactory.CreateMaterialCheckbox("Minimize to Tray");
        checkboxPanel1.Controls.Add(_startupCheckBox);
        checkboxPanel1.Controls.Add(_minimizeCheckBox);
        checkboxPanel1.Controls.Add(_minimizeToTrayCheckBox);
        layout.Controls.Add(checkboxPanel1, 0, 1);
        layout.SetColumnSpan(checkboxPanel1, 2);

        // Audio Control Settings - Use ControlFactory
        var audioLabel = ControlFactory.CreateHeaderLabel("Audio Control Settings");
        layout.Controls.Add(audioLabel, 0, 2);
        layout.SetColumnSpan(audioLabel, 2);

        _rewindCheckBox = ControlFactory.CreateMaterialCheckbox("Previous Track: rewind to start", isChecked: true);
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
    /// Creates the about section with application information and links.
    /// </summary>
    /// <returns>A control containing about information</returns>
    private Control CreateAboutContent()
    {
        var panel = new Panel { Dock = DockStyle.Fill };

        var aboutLabel = new LinkLabel
        {
            Text = "BeatBind v2.0.0\nGlobal hotkeys for Spotify\n\nhttps://github.com/justinknguyen/BeatBind",
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
            var isInStartup = StartupHelper.IsInStartup(Logger);
            _startupCheckBox.Checked = isInStartup || config.StartWithWindows;

            _minimizeCheckBox.Checked = config.StartMinimized;
            _minimizeToTrayCheckBox.Checked = config.MinimizeToTray;
            _rewindCheckBox.Checked = config.PreviousTrackRewindToStart;
            _volumeStepsNumeric.Value = config.VolumeSteps;
            _seekMillisecondsNumeric.Value = config.SeekMilliseconds;

            // Save original values
            _originalStartup = _startupCheckBox.Checked;
            _originalMinimize = config.StartMinimized;
            _originalMinimizeToTray = config.MinimizeToTray;
            _originalRewind = config.PreviousTrackRewindToStart;
            _originalVolumeSteps = config.VolumeSteps;
            _originalSeekMilliseconds = config.SeekMilliseconds;
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
               _seekMillisecondsNumeric.Value != _originalSeekMilliseconds;
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
    }
}

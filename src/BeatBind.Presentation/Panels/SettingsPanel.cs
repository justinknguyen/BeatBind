using BeatBind.Core.Entities;
using BeatBind.Core.Interfaces;
using BeatBind.Presentation.Themes;
using MaterialSkin.Controls;
using Microsoft.Extensions.Logging;

namespace BeatBind.Presentation.Panels;

public partial class SettingsPanel : UserControl
{
    private readonly IConfigurationService _configurationService;
    private readonly ILogger<SettingsPanel> _logger;
    
    private MaterialCheckbox _startupCheckBox = null!;
    private MaterialCheckbox _minimizeCheckBox = null!;
    private MaterialCheckbox _rewindCheckBox = null!;
    private NumericUpDown _volumeStepsNumeric = null!;
    private NumericUpDown _seekMillisecondsNumeric = null!;

    public SettingsPanel(IConfigurationService configurationService, ILogger<SettingsPanel> logger)
    {
        _configurationService = configurationService;
        _logger = logger;
        
        InitializeComponent();
        InitializeUI();
    }

    // Parameterless constructor for WinForms designer support
    public SettingsPanel()
    {
        _configurationService = null!;
        _logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<SettingsPanel>.Instance;
        
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        SuspendLayout();
        
        Dock = DockStyle.Fill;
        BackColor = Theme.CardBackground;
        Padding = new Padding(15);
        
        ResumeLayout(false);
    }

    private void InitializeUI()
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

        // Application Settings Card
        var appSettingsCard = CreateCompactCard("Application Settings", CreateAppSettingsContent());
        mainLayout.Controls.Add(appSettingsCard, 0, 0);

        // About Card
        var aboutCard = CreateCompactCard("About", CreateAboutContent());
        mainLayout.Controls.Add(aboutCard, 0, 1);

        Controls.Add(mainLayout);
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

        headerPanel.SendToBack();
        contentPanel.BringToFront();

        return card;
    }

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
            Margin = new Padding(0, 0, 20, 0),
            TabStop = false
        };

        _minimizeCheckBox = new MaterialCheckbox
        {
            Text = "Minimize to tray",
            AutoSize = true,
            Depth = 0,
            Margin = new Padding(0, 0, 20, 0),
            TabStop = false
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
            Margin = new Padding(0, 0, 0, 8),
            TabStop = false
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

        int linkStart = aboutLabel.Text.IndexOf("https://");
        int linkLength = aboutLabel.Text.Length - linkStart;
        aboutLabel.Links.Add(linkStart, linkLength, "https://github.com/justinknguyen/BeatBind");

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

    public void LoadConfiguration()
    {
        try
        {
            var config = _configurationService.GetConfiguration();
            _startupCheckBox.Checked = config.StartMinimized;
            _minimizeCheckBox.Checked = config.MinimizeToTray;
            _rewindCheckBox.Checked = config.PreviousTrackRewindToStart;
            _volumeStepsNumeric.Value = config.VolumeSteps;
            _seekMillisecondsNumeric.Value = config.SeekMilliseconds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load configuration");
        }
    }

    public void ApplySettingsToConfiguration(ApplicationConfiguration config)
    {
        config.StartMinimized = _startupCheckBox.Checked;
        config.MinimizeToTray = _minimizeCheckBox.Checked;
        config.PreviousTrackRewindToStart = _rewindCheckBox.Checked;
        config.VolumeSteps = (int)_volumeStepsNumeric.Value;
        config.SeekMilliseconds = (int)_seekMillisecondsNumeric.Value;
    }
}

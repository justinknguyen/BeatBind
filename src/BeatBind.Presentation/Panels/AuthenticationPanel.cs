using BeatBind.Application.Services;
using BeatBind.Core.Entities;
using BeatBind.Core.Interfaces;
using BeatBind.Presentation.Themes;
using MaterialSkin.Controls;
using Microsoft.Extensions.Logging;

namespace BeatBind.Presentation.Panels;

public partial class AuthenticationPanel : UserControl
{
    private readonly AuthenticationApplicationService _authenticationService;
    private readonly IConfigurationService _configurationService;
    private readonly ILogger<AuthenticationPanel> _logger;
    
    private MaterialTextBox _clientIdTextBox = null!;
    private MaterialTextBox _clientSecretTextBox = null!;
    private MaterialButton _authenticateButton = null!;
    private MaterialLabel _statusLabel = null!;
    private bool _isAuthenticated;

    public event EventHandler? AuthenticationStatusChanged;
    
    public bool IsAuthenticated => _isAuthenticated;

    public AuthenticationPanel(AuthenticationApplicationService authenticationService, IConfigurationService configurationService, ILogger<AuthenticationPanel> logger)
    {
        _authenticationService = authenticationService;
        _configurationService = configurationService;
        _logger = logger;
        
        InitializeComponent();
        InitializeUI();
    }

    // Parameterless constructor for WinForms designer support
    public AuthenticationPanel()
    {
        _authenticationService = null!;
        _configurationService = null!;
        _logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<AuthenticationPanel>.Instance;
        
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

        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 60f)); // Credentials
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 40f)); // Authentication & Status

        // Spotify Credentials Card
        var credentialsCard = CreateCompactCard("Spotify API Credentials", CreateCredentialsContent());
        mainLayout.Controls.Add(credentialsCard, 0, 0);

        // Combined Authentication & Status Card
        var authStatusCard = CreateCompactCard("Authentication & Status", CreateAuthStatusContent());
        mainLayout.Controls.Add(authStatusCard, 0, 1);

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

    private Control CreateCredentialsContent()
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
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

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

    private Control CreateAuthStatusContent()
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

    private async void AuthenticateButton_Click(object? sender, EventArgs e)
    {
        _authenticateButton.Enabled = false;
        _authenticateButton.Text = "Authenticating...";

        try
        {
            // Save credentials first if provided
            if (!string.IsNullOrEmpty(_clientIdTextBox.Text) && !string.IsNullOrEmpty(_clientSecretTextBox.Text))
            {
                await _authenticationService.UpdateClientCredentialsAsync(_clientIdTextBox.Text, _clientSecretTextBox.Text);
            }

            var result = await _authenticationService.AuthenticateUserAsync();
            
            _isAuthenticated = result.IsSuccess;
            UpdateAuthenticationStatus();

            var message = result.IsSuccess
                ? "Authentication successful!"
                : $"Authentication failed. {result.Error}";
            
            var icon = result.IsSuccess ? MessageBoxIcon.Information : MessageBoxIcon.Error;
            var title = result.IsSuccess ? "Success" : "Error";
            
            MessageBox.Show(message, title, MessageBoxButtons.OK, icon);
            
            AuthenticationStatusChanged?.Invoke(this, EventArgs.Empty);
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

    public void LoadConfiguration()
    {
        try
        {
            var config = _configurationService.GetConfiguration();
            _clientIdTextBox.Text = config.ClientId;
            _clientSecretTextBox.Text = config.ClientSecret;
            
            UpdateAuthenticationStatus();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load configuration");
        }
    }

    public ApplicationConfiguration GetConfiguration()
    {
        var config = _configurationService.GetConfiguration();
        config.ClientId = _clientIdTextBox.Text;
        config.ClientSecret = _clientSecretTextBox.Text;
        return config;
    }

    public void UpdateAuthenticationStatus()
    {
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
            var config = _configurationService.GetConfiguration();
            
            if (!string.IsNullOrEmpty(config.AccessToken) && !string.IsNullOrEmpty(config.RefreshToken))
            {
                if (config.TokenExpiresAt > DateTime.UtcNow.AddMinutes(5))
                {
                    _logger.LogInformation("Found valid stored authentication");
                    return true;
                }
                else if (!string.IsNullOrEmpty(config.RefreshToken))
                {
                    _logger.LogInformation("Found stored authentication with refresh token available");
                    return true;
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
}

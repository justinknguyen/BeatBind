using BeatBind.Application.Services;
using BeatBind.Core.Entities;
using BeatBind.Core.Interfaces;
using BeatBind.Presentation.Helpers;
using BeatBind.Presentation.Themes;
using MaterialSkin.Controls;
using Microsoft.Extensions.Logging;

namespace BeatBind.Presentation.Panels;

public partial class AuthenticationPanel : BasePanelControl
{
    private readonly AuthenticationApplicationService _authenticationService;
    private readonly IConfigurationService _configurationService;

    private MaterialTextBox _clientIdTextBox = null!;
    private MaterialTextBox _clientSecretTextBox = null!;
    private MaterialTextBox _redirectPortTextBox = null!;
    private MaterialButton _authenticateButton = null!;
    private MaterialLabel _statusLabel = null!;
    private bool _isAuthenticated;
    private bool _isLoading;

    private string _originalClientId = string.Empty;
    private string _originalClientSecret = string.Empty;
    private string _originalRedirectPort = string.Empty;

    public event EventHandler? AuthenticationStatusChanged;
    public event EventHandler? ConfigurationChanged;

    public bool IsAuthenticated => _isAuthenticated;

    /// <summary>
    /// Initializes a new instance of the AuthenticationPanel with dependency injection.
    /// </summary>
    /// <param name="authenticationService">Service for authentication operations</param>
    /// <param name="configurationService">Service for configuration management</param>
    /// <param name="logger">Logger instance</param>
    public AuthenticationPanel(AuthenticationApplicationService authenticationService, IConfigurationService configurationService, ILogger<AuthenticationPanel> logger)
        : base(logger)
    {
        _authenticationService = authenticationService;
        _configurationService = configurationService;
    }

    /// <summary>
    /// Parameterless constructor for WinForms designer support.
    /// </summary>
    public AuthenticationPanel() : base()
    {
        _authenticationService = null!;
        _configurationService = null!;
    }

    /// <summary>
    /// Initializes the UI layout and controls for the authentication panel.
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

        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 60f)); // Credentials
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 40f)); // Authentication & Status

        // Use CardFactory for consistent card creation
        var credentialsCard = CardFactory.CreateCompactCard("Spotify API Credentials", CreateCredentialsContent());
        mainLayout.Controls.Add(credentialsCard, 0, 0);

        var authStatusCard = CardFactory.CreateCompactCard("Authentication & Status", CreateAuthStatusContent());
        mainLayout.Controls.Add(authStatusCard, 0, 1);

        Controls.Add(mainLayout);
    }

    /// <summary>
    /// Creates the credentials input section with client ID, secret, and redirect port fields.
    /// </summary>
    /// <returns>A control containing the credentials input fields</returns>
    private Control CreateCredentialsContent()
    {
        var panel = new Panel { Dock = DockStyle.Fill };
        var layout = ControlFactory.CreateSingleColumnLayout(6);

        // Use ControlFactory for consistent label and control creation
        var clientIdLabel = ControlFactory.CreateLabel("Client ID", bold: true);
        _clientIdTextBox = ControlFactory.CreateMaterialTextBox("Enter your Spotify Client ID");

        var clientSecretLabel = ControlFactory.CreateLabel("Client Secret", bold: true);
        _clientSecretTextBox = ControlFactory.CreateMaterialTextBox("Enter your Spotify Client Secret");

        var redirectPortLabel = ControlFactory.CreateLabel("Redirect Port", bold: true);
        _redirectPortTextBox = ControlFactory.CreateMaterialTextBox("http://127.0.0.1:{port}/callback");
        _redirectPortTextBox.Margin = new Padding(0, 0, 0, 0);

        layout.Controls.Add(clientIdLabel, 0, 0);
        layout.Controls.Add(_clientIdTextBox, 0, 1);
        layout.Controls.Add(clientSecretLabel, 0, 2);
        layout.Controls.Add(_clientSecretTextBox, 0, 3);
        layout.Controls.Add(redirectPortLabel, 0, 4);
        layout.Controls.Add(_redirectPortTextBox, 0, 5);

        // Subscribe to changes
        _clientIdTextBox.TextChanged += (s, e) => { if (!_isLoading) { ConfigurationChanged?.Invoke(this, EventArgs.Empty); } };
        _clientSecretTextBox.TextChanged += (s, e) => { if (!_isLoading) { ConfigurationChanged?.Invoke(this, EventArgs.Empty); } };
        _redirectPortTextBox.TextChanged += (s, e) => { if (!_isLoading) { ConfigurationChanged?.Invoke(this, EventArgs.Empty); } };

        panel.Controls.Add(layout);
        return panel;
    }

    /// <summary>
    /// Creates the authentication status and action button section.
    /// </summary>
    /// <returns>A control containing the authentication button and status label</returns>
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

        // Use ControlFactory for consistent control creation
        _statusLabel = ControlFactory.CreateMaterialLabel("Not authenticated", highEmphasis: true, fontSize: 11f);
        _statusLabel.TextAlign = ContentAlignment.MiddleCenter;
        _statusLabel.Dock = DockStyle.Top;

        statusContainer.Controls.Add(_statusLabel);

        _authenticateButton = ControlFactory.CreateMaterialButton("AUTHENTICATE WITH SPOTIFY", 0, 45);
        _authenticateButton.Dock = DockStyle.Top;
        _authenticateButton.Margin = new Padding(0, 10, 0, 10);
        _authenticateButton.Click += AuthenticateButton_Click;

        panel.Controls.Add(_authenticateButton);
        panel.Controls.Add(statusContainer);

        return panel;
    }

    /// <summary>
    /// Handles the authenticate button click event. Saves credentials and initiates OAuth flow.
    /// </summary>
    /// <param name="sender">Event sender</param>
    /// <param name="e">Event arguments</param>
    private async void AuthenticateButton_Click(object? sender, EventArgs e)
    {
        _authenticateButton.Enabled = false;
        _authenticateButton.Text = "Authenticating...";

        try
        {
            // Save credentials first if provided
            if (!string.IsNullOrEmpty(_clientIdTextBox.Text) && !string.IsNullOrEmpty(_clientSecretTextBox.Text))
            {
                _authenticationService.UpdateClientCredentials(_clientIdTextBox.Text, _clientSecretTextBox.Text);
            }

            var result = await _authenticationService.AuthenticateUserAsync();

            void HandleResult()
            {
                _isAuthenticated = result.IsSuccess;
                UpdateAuthenticationStatus();

                // Use MessageBoxHelper for consistent messaging
                MessageBoxHelper.ShowResult(
                    result.IsSuccess,
                    "Authentication successful!",
                    $"Authentication failed. {result.Error}"
                );

                AuthenticationStatusChanged?.Invoke(this, EventArgs.Empty);
            }

            if (InvokeRequired)
            {
                Invoke(new Action(HandleResult));
            }
            else
            {
                HandleResult();
            }
        }
        catch (Exception ex)
        {
            LogError(ex, "Authentication error");
            MessageBoxHelper.ShowException(ex, "authenticating");
        }
        finally
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => _authenticateButton.Enabled = true));
            }
            else
            {
                _authenticateButton.Enabled = true;
            }
            // Button text is set by UpdateAuthenticationStatus() based on auth state
        }
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
            _clientIdTextBox.Text = config.ClientId;
            _clientSecretTextBox.Text = config.ClientSecret;
            _redirectPortTextBox.Text = config.RedirectPort.ToString();

            // Save original values
            _originalClientId = config.ClientId ?? string.Empty;
            _originalClientSecret = config.ClientSecret ?? string.Empty;
            _originalRedirectPort = config.RedirectPort.ToString();

            UpdateAuthenticationStatus();
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
        return _clientIdTextBox.Text != _originalClientId ||
               _clientSecretTextBox.Text != _originalClientSecret ||
               _redirectPortTextBox.Text != _originalRedirectPort;
    }

    /// <summary>
    /// Retrieves the current configuration with values from the UI controls.
    /// </summary>
    /// <returns>The updated application configuration</returns>
    public ApplicationConfiguration GetConfiguration()
    {
        var config = _configurationService.GetConfiguration();
        config.ClientId = _clientIdTextBox.Text;
        config.ClientSecret = _clientSecretTextBox.Text;

        if (int.TryParse(_redirectPortTextBox.Text, out int port))
        {
            config.RedirectPort = port;
            config.RedirectUri = $"http://127.0.0.1:{port}/callback";
        }

        return config;
    }

    /// <summary>
    /// Updates the authentication status display based on stored credentials.
    /// </summary>
    public void UpdateAuthenticationStatus()
    {
        if (InvokeRequired)
        {
            Invoke(new Action(UpdateAuthenticationStatus));
            return;
        }

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

    /// <summary>
    /// Checks if valid authentication tokens are stored in the configuration.
    /// </summary>
    /// <returns>True if valid authentication exists, false otherwise</returns>
    private bool CheckStoredAuthentication()
    {
        try
        {
            var config = _configurationService.GetConfiguration();

            if (!string.IsNullOrEmpty(config.AccessToken) && !string.IsNullOrEmpty(config.RefreshToken))
            {
                if (config.TokenExpiresAt > DateTime.UtcNow.AddMinutes(5))
                {
                    LogInfo("Found valid stored authentication");
                    return true;
                }
                else if (!string.IsNullOrEmpty(config.RefreshToken))
                {
                    LogInfo("Found stored authentication with refresh token available");
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            LogError(ex, "Error checking stored authentication");
            return false;
        }
    }
}

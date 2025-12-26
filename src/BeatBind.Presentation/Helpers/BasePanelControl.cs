using System.ComponentModel;
using BeatBind.Presentation.Themes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BeatBind.Presentation.Helpers;

/// <summary>
/// Base class for all panel controls in the Presentation layer.
/// Provides common initialization logic and design-time support.
/// Inherit from this class when creating new panels to eliminate boilerplate code.
/// </summary>
public abstract class BasePanelControl : UserControl
{
    protected ILogger Logger { get; }

    /// <summary>
    /// Constructor for runtime use with dependency injection.
    /// </summary>
    /// <param name="logger">Logger instance for this panel</param>
    protected BasePanelControl(ILogger logger)
    {
        Logger = logger;
        InitializeComponent();

        // Only call InitializeUI if not in design mode
        if (LicenseManager.UsageMode != LicenseUsageMode.Designtime)
        {
            InitializeUI();
        }
    }

    /// <summary>
    /// Parameterless constructor for WinForms designer support.
    /// Automatically provides a NullLogger instance.
    /// </summary>
    protected BasePanelControl()
    {
        Logger = NullLogger.Instance;
        InitializeComponent();
    }

    /// <summary>
    /// Initializes the component with standard panel settings.
    /// Override this method if you need custom initialization behavior.
    /// </summary>
    protected virtual void InitializeComponent()
    {
        SuspendLayout();

        Dock = DockStyle.Fill;
        BackColor = Theme.CardBackground;
        Padding = new Padding(15);

        ResumeLayout(false);
    }

    /// <summary>
    /// Initializes the UI controls and layout.
    /// Override this method to create your panel's UI structure.
    /// </summary>
    protected abstract void InitializeUI();

    /// <summary>
    /// Helper method to safely log information.
    /// </summary>
    protected void LogInfo(string message) => Logger?.LogInformation(message);

    /// <summary>
    /// Helper method to safely log warnings.
    /// </summary>
    protected void LogWarning(string message) => Logger?.LogWarning(message);

    /// <summary>
    /// Helper method to safely log errors.
    /// </summary>
    protected void LogError(Exception ex, string message) => Logger?.LogError(ex, message);
}

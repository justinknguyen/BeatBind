namespace BeatBind.Presentation.Helpers;

/// <summary>
/// Provides standardized MessageBox wrappers for consistent user messaging.
/// Use these methods instead of calling MessageBox.Show() directly.
/// </summary>
public static class MessageBoxHelper
{
    /// <summary>
    /// Shows a success message to the user.
    /// </summary>
    /// <param name="message">The success message to display</param>
    /// <param name="title">Dialog title (default: "Success")</param>
    public static void ShowSuccess(string message, string title = "Success")
    {
        MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    /// <summary>
    /// Shows an error message to the user.
    /// </summary>
    /// <param name="message">The error message to display</param>
    /// <param name="title">Dialog title (default: "Error")</param>
    public static void ShowError(string message, string title = "Error")
    {
        MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    /// <summary>
    /// Shows a warning message to the user.
    /// </summary>
    /// <param name="message">The warning message to display</param>
    /// <param name="title">Dialog title (default: "Warning")</param>
    public static void ShowWarning(string message, string title = "Warning")
    {
        MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    /// <summary>
    /// Shows an informational message to the user.
    /// </summary>
    /// <param name="message">The information message to display</param>
    /// <param name="title">Dialog title (default: "Information")</param>
    public static void ShowInfo(string message, string title = "Information")
    {
        MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    /// <summary>
    /// Asks the user for confirmation with Yes/No buttons.
    /// </summary>
    /// <param name="message">The confirmation question</param>
    /// <param name="title">Dialog title (default: "Confirm")</param>
    /// <returns>True if user clicked Yes, false otherwise</returns>
    public static bool Confirm(string message, string title = "Confirm")
    {
        var result = MessageBox.Show(message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        return result == DialogResult.Yes;
    }

    /// <summary>
    /// Asks the user for confirmation to delete an item.
    /// </summary>
    /// <param name="itemName">Name of the item to delete</param>
    /// <param name="itemType">Type of item (e.g., "hotkey", "setting")</param>
    /// <returns>True if user confirmed deletion, false otherwise</returns>
    public static bool ConfirmDelete(string itemName, string itemType = "item")
    {
        return Confirm(
            $"Are you sure you want to delete the {itemType} '{itemName}'?",
            "Confirm Delete"
        );
    }

    /// <summary>
    /// Shows an error dialog for exceptions with consistent formatting.
    /// </summary>
    /// <param name="exception">The exception that occurred</param>
    /// <param name="context">Context description (e.g., "saving configuration")</param>
    public static void ShowException(Exception exception, string context)
    {
        ShowError($"An error occurred while {context}:\n\n{exception.Message}");
    }

    /// <summary>
    /// Shows a result-based message - success or error depending on the outcome.
    /// </summary>
    /// <param name="isSuccess">Whether the operation succeeded</param>
    /// <param name="successMessage">Message to show on success</param>
    /// <param name="errorMessage">Message to show on failure</param>
    public static void ShowResult(bool isSuccess, string successMessage, string errorMessage)
    {
        if (isSuccess)
        {
            ShowSuccess(successMessage);
        }
        else
        {
            ShowError(errorMessage);
        }
    }

    /// <summary>
    /// Opens a URL in the default browser with error handling.
    /// </summary>
    /// <param name="url">The URL to open</param>
    /// <param name="errorCallback">Optional callback to execute on error</param>
    /// <returns>True if URL was opened successfully, false otherwise</returns>
    public static bool OpenUrl(string url, Action<Exception>? errorCallback = null)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
            return true;
        }
        catch (Exception ex)
        {
            errorCallback?.Invoke(ex);
            ShowError(
                "Failed to open the URL. Please try copying and pasting it into your browser manually.",
                "Browser Error"
            );
            return false;
        }
    }
}

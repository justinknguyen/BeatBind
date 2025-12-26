using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace BeatBind.Infrastructure.Helpers
{
    /// <summary>
    /// Helper class for managing Windows startup registry entries.
    /// </summary>
    public static class StartupHelper
    {
        private const string RUN_LOCATION = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string APP_NAME = "BeatBind";

        /// <summary>
        /// Adds or removes the application from Windows startup based on the provided flag.
        /// </summary>
        /// <param name="startWithWindows">True to add to startup, false to remove</param>
        /// <param name="logger">Optional logger for error reporting</param>
        public static void SetStartupWithWindows(bool startWithWindows, ILogger? logger = null)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RUN_LOCATION, true);
                if (key == null)
                {
                    logger?.LogError("Failed to open registry key: {RegistryPath}", RUN_LOCATION);
                    return;
                }

                if (startWithWindows)
                {
                    // Get the path to the current executable
                    var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                    if (string.IsNullOrEmpty(exePath))
                    {
                        logger?.LogError("Failed to get current executable path");
                        return;
                    }

                    // Add to startup
                    key.SetValue(APP_NAME, $"\"{exePath}\"");
                    logger?.LogInformation("Added BeatBind to Windows startup: {ExePath}", exePath);
                }
                else
                {
                    // Remove from startup
                    if (key.GetValue(APP_NAME) != null)
                    {
                        key.DeleteValue(APP_NAME);
                        logger?.LogInformation("Removed BeatBind from Windows startup");
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                logger?.LogError(ex, "Unauthorized access to registry. Run as administrator if needed.");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to update Windows startup settings");
            }
        }

        /// <summary>
        /// Checks if the application is currently set to start with Windows.
        /// </summary>
        /// <param name="logger">Optional logger for error reporting</param>
        /// <returns>True if the application is in Windows startup, false otherwise</returns>
        public static bool IsInStartup(ILogger? logger = null)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RUN_LOCATION, false);
                if (key == null)
                {
                    return false;
                }

                var value = key.GetValue(APP_NAME) as string;
                return !string.IsNullOrEmpty(value);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to check Windows startup status");
                return false;
            }
        }
    }
}

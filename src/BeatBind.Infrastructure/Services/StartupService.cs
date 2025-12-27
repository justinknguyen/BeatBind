using BeatBind.Core.Interfaces;
using BeatBind.Infrastructure.Helpers;
using Microsoft.Extensions.Logging;

namespace BeatBind.Infrastructure.Services
{
    public class StartupService : IStartupService
    {
        private readonly ILogger<StartupService> _logger;
        private readonly IRegistryWrapper _registryWrapper;
        private const string APP_NAME = "BeatBind";

        public StartupService(ILogger<StartupService> logger, IRegistryWrapper registryWrapper)
        {
            _logger = logger;
            _registryWrapper = registryWrapper;
        }

        public void SetStartupWithWindows(bool startWithWindows)
        {
            try
            {
                if (startWithWindows)
                {
                    var exePath = _registryWrapper.GetCurrentProcessPath();
                    if (string.IsNullOrEmpty(exePath))
                    {
                        _logger.LogError("Failed to get current executable path");
                        return;
                    }

                    _registryWrapper.SetStartupRegistryValue(APP_NAME, exePath);
                    _logger.LogInformation("Added BeatBind to Windows startup: {ExePath}", exePath);
                }
                else
                {
                    _registryWrapper.RemoveStartupRegistryValue(APP_NAME);
                    _logger.LogInformation("Removed BeatBind from Windows startup");
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Unauthorized access to registry. Run as administrator if needed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update Windows startup settings");
            }
        }

        public bool IsInStartup()
        {
            try
            {
                return _registryWrapper.HasStartupRegistryValue(APP_NAME);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check Windows startup status");
                return false;
            }
        }
    }
}

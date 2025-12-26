using BeatBind.Core.Common;
using BeatBind.Core.Entities;
using BeatBind.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace BeatBind.Application.Services
{
    public class ConfigurationApplicationService
    {
        private readonly IConfigurationService _configurationService;
        private readonly ILogger<ConfigurationApplicationService> _logger;

        public ConfigurationApplicationService(
            IConfigurationService configurationService,
            ILogger<ConfigurationApplicationService> logger)
        {
            _configurationService = configurationService;
            _logger = logger;
        }

        public Result SaveConfiguration(ApplicationConfiguration configuration)
        {
            try
            {
                _configurationService.SaveConfiguration(configuration);
                _logger.LogInformation("Configuration saved successfully");
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save configuration");
                return Result.Failure("Failed to save configuration");
            }
        }

        public Result UpdateClientCredentials(string clientId, string clientSecret)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(clientId))
                {
                    return Result.Failure("Client ID cannot be empty");
                }

                if (string.IsNullOrWhiteSpace(clientSecret))
                {
                    return Result.Failure("Client Secret cannot be empty");
                }

                _configurationService.UpdateClientCredentials(clientId, clientSecret);
                _logger.LogInformation("Client credentials updated successfully");
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update client credentials");
                return Result.Failure("Failed to update client credentials");
            }
        }

        public ApplicationConfiguration GetConfiguration()
        {
            return _configurationService.GetConfiguration();
        }

        public IEnumerable<Hotkey> GetHotkeys()
        {
            return _configurationService.GetHotkeys();
        }
    }
}

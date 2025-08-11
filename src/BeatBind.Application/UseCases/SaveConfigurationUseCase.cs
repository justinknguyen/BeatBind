using BeatBind.Domain.Entities;
using BeatBind.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace BeatBind.Application.UseCases
{
    public class SaveConfigurationUseCase
    {
        private readonly IConfigurationService _configurationService;
        private readonly ILogger<SaveConfigurationUseCase> _logger;

        public SaveConfigurationUseCase(
            IConfigurationService configurationService,
            ILogger<SaveConfigurationUseCase> logger)
        {
            _configurationService = configurationService;
            _logger = logger;
        }

        public void Execute(string clientId, string clientSecret)
        {
            try
            {
                _logger.LogInformation("Saving client credentials...");
                _configurationService.UpdateClientCredentials(clientId, clientSecret);
                _logger.LogInformation("Client credentials saved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save client credentials");
                throw;
            }
        }

        public void Execute(ApplicationConfiguration configuration)
        {
            try
            {
                _logger.LogInformation("Saving application configuration...");
                _configurationService.SaveConfiguration(configuration);
                _logger.LogInformation("Application configuration saved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save application configuration");
                throw;
            }
        }
    }
}

using System.Diagnostics;
using System.Windows.Forms;
using BeatBind.Application.Services;
using BeatBind.Application.UseCases;
using BeatBind.Domain.Interfaces;
using BeatBind.Infrastructure.Configuration;
using BeatBind.Infrastructure.Hotkeys;
using BeatBind.Infrastructure.Spotify;
using BeatBind.Presentation.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BeatBind
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Prevent multiple instances
            using var mutex = new Mutex(true, "BeatBindGlobalMutex", out var createdNew);
            if (!createdNew)
            {
                MessageBox.Show("BeatBind is already running! Check the system tray.", "Already Running", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                System.Windows.Forms.Application.EnableVisualStyles();
                System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

                var host = CreateHostBuilder().Build();
                
                using (host)
                {
                    var mainForm = host.Services.GetRequiredService<MainForm>();
                    System.Windows.Forms.Application.Run(mainForm);
                }
            }
            catch (Exception ex)
            {
                var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger("BeatBind");
                logger.LogCritical(ex, "Fatal error occurred");
                
                MessageBox.Show($"A fatal error occurred: {ex.Message}", "Fatal Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static IHostBuilder CreateHostBuilder()
        {
            return Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Infrastructure services
                    services.AddSingleton<IConfigurationService, JsonConfigurationService>();
                    services.AddTransient<IAuthenticationService, SpotifyAuthenticationService>();
                    services.AddTransient<ISpotifyService, SpotifyService>();
                    services.AddHttpClient<ISpotifyService, SpotifyService>();
                    services.AddHttpClient<IAuthenticationService, SpotifyAuthenticationService>();

                    // Application services
                    services.AddTransient<MusicControlService>();
                    services.AddTransient<AuthenticateUserUseCase>();
                    services.AddTransient<SaveConfigurationUseCase>();

                    // Presentation services
                    services.AddSingleton<MainForm>(serviceProvider =>
                    {
                        var form = new MainForm(
                            serviceProvider.GetRequiredService<MusicControlService>(),
                            serviceProvider.GetRequiredService<HotkeyManagementService>(),
                            serviceProvider.GetRequiredService<AuthenticateUserUseCase>(),
                            serviceProvider.GetRequiredService<SaveConfigurationUseCase>(),
                            serviceProvider.GetRequiredService<IConfigurationService>(),
                            serviceProvider.GetRequiredService<ILogger<MainForm>>()
                        );

                        // Register hotkey service using the main form
                        var hotkeyService = new WindowsHotkeyService(form, 
                            serviceProvider.GetRequiredService<ILogger<WindowsHotkeyService>>());
                        
                        var hotkeyManagement = new HotkeyManagementService(
                            hotkeyService,
                            serviceProvider.GetRequiredService<IConfigurationService>(),
                            serviceProvider.GetRequiredService<MusicControlService>(),
                            serviceProvider.GetRequiredService<ILogger<HotkeyManagementService>>()
                        );

                        // Update the form's hotkey management service
                        SetHotkeyManagementService(form, hotkeyManagement);

                        return form;
                    });

                    services.AddTransient<HotkeyManagementService>();
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Information);
                });
        }

        private static void SetHotkeyManagementService(MainForm form, HotkeyManagementService service)
        {
            // This is a simplified approach - in a production app, you'd want to handle this more elegantly
            // For now, we'll assume the form can work with the service passed in the constructor
        }
    }
}

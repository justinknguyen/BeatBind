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

                    // Register MainForm first without HotkeyManagementService
                    services.AddSingleton<MainForm>();

                    // Register IHotkeyService that depends on MainForm
                    services.AddSingleton<IHotkeyService, WindowsHotkeyService>(serviceProvider =>
                    {
                        var mainForm = serviceProvider.GetRequiredService<MainForm>();
                        var logger = serviceProvider.GetRequiredService<ILogger<WindowsHotkeyService>>();
                        return new WindowsHotkeyService(mainForm, logger);
                    });

                    // Register HotkeyManagementService
                    services.AddSingleton<HotkeyManagementService>();

                    // Configure MainForm with HotkeyManagementService after all services are registered
                    services.AddSingleton<IHostedService, MainFormInitializerService>();
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Information);
                });
        }
    }

    public class MainFormInitializerService : IHostedService
    {
        private readonly MainForm _mainForm;
        private readonly HotkeyManagementService _hotkeyManagementService;

        public MainFormInitializerService(MainForm mainForm, HotkeyManagementService hotkeyManagementService)
        {
            _mainForm = mainForm;
            _hotkeyManagementService = hotkeyManagementService;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _mainForm.SetHotkeyManagementService(_hotkeyManagementService);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}

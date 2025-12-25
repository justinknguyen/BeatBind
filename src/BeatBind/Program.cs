using BeatBind.Application.Behaviors;
using BeatBind.Application.Services;
using BeatBind.Domain.Interfaces;
using BeatBind.Hosting;
using BeatBind.Infrastructure.Configuration;
using BeatBind.Infrastructure.Hotkeys;
using BeatBind.Infrastructure.Spotify;
using BeatBind.Presentation.Forms;
using FluentValidation;
using MediatR;
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
                
                // Start the host to initialize services (like MainFormInitializerService)
                host.Start();

                using (host)
                {
                    var mainForm = host.Services.GetRequiredService<MainForm>();
                    System.Windows.Forms.Application.Run(mainForm);
                }
                
                // Stop the host when application exits
                host.StopAsync().GetAwaiter().GetResult();
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
                    ConfigureInfrastructure(services);
                    ConfigureApplication(services);
                    ConfigurePresentation(services);
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Information);
                });
        }

        private static void ConfigureInfrastructure(IServiceCollection services)
        {
            services.AddSingleton<IConfigurationService, JsonConfigurationService>();
            services.AddTransient<IAuthenticationService, SpotifyAuthenticationService>();
            services.AddTransient<ISpotifyService, SpotifyService>();
            services.AddHttpClient<ISpotifyService, SpotifyService>();
            services.AddHttpClient<IAuthenticationService, SpotifyAuthenticationService>();
        }

        private static void ConfigureApplication(IServiceCollection services)
        {
            services.AddTransient<MusicControlService>();
            
            // MediatR
            services.AddMediatR(cfg => 
                cfg.RegisterServicesFromAssembly(typeof(Application.Abstractions.Messaging.ICommand).Assembly));
            
            // Validators
            services.AddValidatorsFromAssembly(typeof(Application.Abstractions.Messaging.ICommand).Assembly);

            // Pipeline Behaviors
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        }

        private static void ConfigurePresentation(IServiceCollection services)
        {
            services.AddSingleton<MainForm>();

            services.AddSingleton<IHotkeyService, WindowsHotkeyService>(sp =>
            {
                var mainForm = sp.GetRequiredService<MainForm>();
                var logger = sp.GetRequiredService<ILogger<WindowsHotkeyService>>();
                return new WindowsHotkeyService(mainForm, logger);
            });

            services.AddSingleton<HotkeyManagementService>();
            services.AddSingleton<IHostedService, MainFormInitializerService>();
        }
    }
}

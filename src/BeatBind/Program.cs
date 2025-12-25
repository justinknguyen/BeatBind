using BeatBind.Application.Behaviors;
using BeatBind.Application.Services;
using BeatBind.Core.Interfaces;
using BeatBind.Infrastructure.Services;
using BeatBind.Presentation.Forms;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BeatBind
{
    internal sealed class Startup : IHostedService
    {
        private readonly MainForm _mainForm;
        private readonly HotkeyManagementService _hotkeyManagementService;

        public Startup(MainForm mainForm, HotkeyManagementService hotkeyManagementService)
        {
            _mainForm = mainForm;
            _hotkeyManagementService = hotkeyManagementService;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _mainForm.SetHotkeyManagementService(_hotkeyManagementService);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

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
                
                // Start the host to initialize services (like Startup)
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
#if DEBUG
                    logging.SetMinimumLevel(LogLevel.Information);
#else
                    logging.SetMinimumLevel(LogLevel.Warning);
#endif
                });
        }

        private static void ConfigureInfrastructure(IServiceCollection services)
        {
            services.AddSingleton<IConfigurationService, JsonConfigurationService>();
            services.AddHttpClient<ISpotifyService, SpotifyService>();
            services.AddHttpClient<IAuthenticationService, SpotifyAuthenticationService>();
            services.AddHttpClient<IGithubReleaseService, GithubReleaseService>();
        }

        private static void ConfigureApplication(IServiceCollection services)
        {
            services.AddTransient<MusicControlService>();
            
            // MediatR
            services.AddMediatR(cfg => 
                cfg.RegisterServicesFromAssembly(typeof(Application.Abstractions.ICommand).Assembly));
            
            // Validators
            services.AddValidatorsFromAssembly(typeof(Application.Abstractions.ICommand).Assembly);

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
            services.AddSingleton<IHostedService, Startup>();
        }
    }
}

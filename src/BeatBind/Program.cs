using BeatBind.Application.Behaviors;
using BeatBind.Application.Services;
using BeatBind.Core.Interfaces;
using BeatBind.Infrastructure.Helpers;
using BeatBind.Infrastructure.Services;
using BeatBind.Presentation;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace BeatBind
{
    internal sealed class Startup : IHostedService
    {
        private readonly MainForm _mainForm;
        private readonly HotkeyApplicationService _hotkeyApplicationService;

        /// <summary>
        /// Initializes a new instance of the Startup service.
        /// </summary>
        /// <param name="mainForm">The main application form</param>
        /// <param name="hotkeyApplicationService">The hotkey application service</param>
        public Startup(MainForm mainForm, HotkeyApplicationService hotkeyApplicationService)
        {
            _mainForm = mainForm;
            _hotkeyApplicationService = hotkeyApplicationService;
        }

        /// <summary>
        /// Starts the hosted service and initializes the hotkey application service.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A completed task</returns>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _mainForm.SetHotkeyApplicationService(_hotkeyApplicationService);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Stops the hosted service.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A completed task</returns>
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
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

                // Set the application to stay running even when all forms are hidden
                System.Windows.Forms.Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

                var host = CreateHostBuilder().Build();

                // Start the host to initialize services (like Startup)
                host.Start();

                using (host)
                {
                    var mainForm = host.Services.GetRequiredService<MainForm>();

                    // Use ApplicationContext to keep the app running even when form is hidden
                    var context = new ApplicationContext(mainForm);
                    System.Windows.Forms.Application.Run(context);
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

        /// <summary>
        /// Creates and configures the application host builder.
        /// </summary>
        /// <returns>A configured host builder</returns>
        private static IHostBuilder CreateHostBuilder()
        {
            return Host.CreateDefaultBuilder()
                .UseSerilog((context, services, configuration) =>
                {
                    var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BeatBind");
                    Directory.CreateDirectory(appDataPath);
                    var logPath = Path.Combine(appDataPath, "log-.txt");

                    configuration
                        .ReadFrom.Configuration(context.Configuration)
                        .ReadFrom.Services(services)
                        .Enrich.FromLogContext()
                        .WriteTo.Console()
                        .WriteTo.File(logPath,
                            rollingInterval: RollingInterval.Day,
                            retainedFileTimeLimit: TimeSpan.FromDays(2));

                    configuration.MinimumLevel.Information();
                })
                .ConfigureServices((context, services) =>
                {
                    ConfigureInfrastructure(services);
                    ConfigureApplication(services);
                    ConfigurePresentation(services);
                });
        }

        /// <summary>
        /// Configures infrastructure layer services.
        /// </summary>
        /// <param name="services">The service collection</param>
        private static void ConfigureInfrastructure(IServiceCollection services)
        {
            services.AddSingleton<IConfigurationService, ConfigurationService>();

            // SpotifyService and AuthenticationService carry authentication state, so
            // they must be singletons — transient copies (the AddHttpClient default)
            // would each hold and refresh their own tokens independently.
            services.AddSingleton<IAuthenticationService>(sp => new AuthenticationService(
                sp.GetRequiredService<ILogger<AuthenticationService>>(),
                sp.GetRequiredService<IConfigurationService>(),
                CreateLongLivedHttpClient()));
            services.AddSingleton<ISpotifyService>(sp => new SpotifyService(
                CreateLongLivedHttpClient(),
                sp.GetRequiredService<ILogger<SpotifyService>>(),
                sp.GetRequiredService<IAuthenticationService>()));

            services.AddHttpClient<IGithubReleaseService, GithubReleaseService>();
            services.AddSingleton<IRegistryWrapper, RegistryWrapper>();
            services.AddSingleton<IStartupService, StartupService>();
        }

        /// <summary>
        /// Creates an HttpClient suitable for singleton services: pooled connections
        /// are recycled periodically so long-lived clients still pick up DNS changes.
        /// </summary>
        /// <returns>A configured HttpClient</returns>
        private static HttpClient CreateLongLivedHttpClient()
        {
            return new HttpClient(new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(15),
                // Keep the connection to the API warm between hotkey presses so a
                // press after an idle stretch doesn't pay a new TCP + TLS handshake.
                // The pings require HTTP/2, which SpotifyService requests per call.
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(10),
                KeepAlivePingDelay = TimeSpan.FromSeconds(30),
                KeepAlivePingTimeout = TimeSpan.FromSeconds(10),
                KeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always
            });
        }

        /// <summary>
        /// Configures application layer services including MediatR and validation.
        /// </summary>
        /// <param name="services">The service collection</param>
        private static void ConfigureApplication(IServiceCollection services)
        {
            // Application Services — singletons so state like the playback cache and
            // last pre-mute volume is shared by every consumer
            services.AddSingleton<AuthenticationApplicationService>();
            services.AddSingleton<ConfigurationApplicationService>();
            services.AddSingleton<MusicControlApplicationService>();
            services.AddSingleton<HotkeyApplicationService>();

            // MediatR
            services.AddMediatR(cfg =>
                cfg.RegisterServicesFromAssembly(typeof(Application.Abstractions.ICommand).Assembly));

            // Validators
            services.AddValidatorsFromAssembly(typeof(Application.Abstractions.ICommand).Assembly);

            // Pipeline Behaviors
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        }

        /// <summary>
        /// Configures presentation layer services including forms and hotkey service.
        /// </summary>
        /// <param name="services">The service collection</param>
        private static void ConfigurePresentation(IServiceCollection services)
        {
            services.AddSingleton<MainForm>();

            services.AddSingleton<IHotkeyService, HotkeyService>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<HotkeyService>>();
                return new HotkeyService(logger);
            });

            services.AddSingleton<IHostedService, Startup>();
        }
    }
}

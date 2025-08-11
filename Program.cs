using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;

namespace BeatBind
{
    internal static class Program
    {
        private static readonly ILogger Logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger("BeatBind");

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Prevent multiple instances
            if (IsAlreadyRunning())
            {
                Logger.LogWarning("BeatBind is already running. Exiting.");
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                var backend = new SpotifyBackend();
                var frontend = new MainForm(backend);

                Application.Run(frontend);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while starting the application");
                MessageBox.Show($"Error starting BeatBind: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static bool IsAlreadyRunning()
        {
            var currentProcess = Process.GetCurrentProcess();
            var processes = Process.GetProcessesByName(currentProcess.ProcessName);
            return processes.Length > 1;
        }
    }
}

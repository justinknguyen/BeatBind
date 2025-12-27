using System.Diagnostics;
using Microsoft.Win32;

namespace BeatBind.Infrastructure.Helpers
{
    public interface IRegistryWrapper
    {
        void SetStartupRegistryValue(string appName, string path);
        void RemoveStartupRegistryValue(string appName);
        bool HasStartupRegistryValue(string appName);
        string? GetCurrentProcessPath();
    }

    public class RegistryWrapper : IRegistryWrapper
    {
        private const string RUN_LOCATION = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        public void SetStartupRegistryValue(string appName, string path)
        {
            using var key = Registry.CurrentUser.OpenSubKey(RUN_LOCATION, true);
            key?.SetValue(appName, $"\"{path}\"");
        }

        public void RemoveStartupRegistryValue(string appName)
        {
            using var key = Registry.CurrentUser.OpenSubKey(RUN_LOCATION, true);
            if (key?.GetValue(appName) != null)
            {
                key.DeleteValue(appName);
            }
        }

        public bool HasStartupRegistryValue(string appName)
        {
            using var key = Registry.CurrentUser.OpenSubKey(RUN_LOCATION, false);
            if (key == null)
            {
                return false;
            }

            var value = key.GetValue(appName) as string;
            return !string.IsNullOrEmpty(value);
        }

        public string? GetCurrentProcessPath()
        {
            return Process.GetCurrentProcess().MainModule?.FileName;
        }
    }
}

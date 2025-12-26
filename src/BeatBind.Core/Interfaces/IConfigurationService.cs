using BeatBind.Core.Entities;

namespace BeatBind.Core.Interfaces
{
    public interface IConfigurationService
    {
        ApplicationConfiguration GetConfiguration();
        void SaveConfiguration(ApplicationConfiguration configuration);
        void UpdateClientCredentials(string clientId, string clientSecret);
        void AddHotkey(Hotkey hotkey);
        void RemoveHotkey(int hotkeyId);
        void UpdateHotkey(Hotkey hotkey);
        List<Hotkey> GetHotkeys();
    }
}

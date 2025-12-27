using BeatBind.Core.Entities;

namespace BeatBind.Core.Interfaces
{
    public interface IHotkeyService
    {
        bool RegisterHotkey(Hotkey hotkey, Action action);
        bool UnregisterHotkey(int hotkeyId);
        void UnregisterAllHotkeys();
        bool IsHotkeyRegistered(int hotkeyId);
        void Pause();
        void Resume();
        event EventHandler<Hotkey>? HotkeyPressed;
    }
}

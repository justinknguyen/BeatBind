using BeatBind.Domain.Entities;

namespace BeatBind.Domain.Interfaces
{
    public interface IHotkeyService
    {
        bool RegisterHotkey(Hotkey hotkey, Action action);
        bool UnregisterHotkey(int hotkeyId);
        void UnregisterAllHotkeys();
        bool IsHotkeyRegistered(int hotkeyId);
        event EventHandler<Hotkey>? HotkeyPressed;
    }
}

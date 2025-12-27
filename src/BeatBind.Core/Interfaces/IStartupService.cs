namespace BeatBind.Core.Interfaces
{
    public interface IStartupService
    {
        void SetStartupWithWindows(bool startWithWindows);
        bool IsInStartup();
    }
}

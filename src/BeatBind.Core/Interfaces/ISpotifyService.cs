using BeatBind.Core.Entities;

namespace BeatBind.Core.Interfaces
{
    public interface ISpotifyService
    {
        Task<bool> AuthenticateAsync();
        Task<bool> RefreshTokenAsync();
        bool IsAuthenticated { get; }
        
        Task<PlaybackState?> GetCurrentPlaybackAsync();
        Task<bool> PlayAsync();
        Task<bool> PauseAsync();
        Task<bool> NextTrackAsync();
        Task<bool> PreviousTrackAsync();
        Task<bool> SetVolumeAsync(int volume);
        Task<bool> ToggleShuffleAsync();
        Task<bool> ToggleRepeatAsync();
        Task<bool> SaveCurrentTrackAsync();
        Task<bool> RemoveCurrentTrackAsync();
        Task<bool> SeekToPositionAsync(int positionMs);
    }
}

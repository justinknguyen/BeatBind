using BeatBind.Core.Entities;

namespace BeatBind.Core.Interfaces
{
    public interface IGithubReleaseService
    {
        Task<GithubRelease?> GetLatestReleaseAsync();
        bool IsNewerVersion(string currentVersion, string latestVersion);
    }
}

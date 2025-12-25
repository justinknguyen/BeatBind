using System.Net.Http.Json;
using System.Text.Json.Serialization;
using BeatBind.Core.Entities;
using BeatBind.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace BeatBind.Infrastructure.Services
{
    public class GithubReleaseService : IGithubReleaseService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GithubReleaseService> _logger;
        private const string GITHUB_API_URL = "https://api.github.com/repos/justinknguyen/BeatBind/releases/latest";

        public GithubReleaseService(HttpClient httpClient, ILogger<GithubReleaseService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "BeatBind");
        }

        public async Task<GithubRelease?> GetLatestReleaseAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<GithubReleaseResponse>(GITHUB_API_URL);
                
                if (response == null)
                    return null;

                return new GithubRelease
                {
                    Version = response.TagName?.TrimStart('v') ?? string.Empty,
                    Url = response.HtmlUrl ?? string.Empty,
                    Name = response.Name ?? string.Empty,
                    PublishedAt = response.PublishedAt,
                    IsPrerelease = response.Prerelease
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch latest GitHub release");
                return null;
            }
        }

        public bool IsNewerVersion(string currentVersion, string latestVersion)
        {
            try
            {
                // Remove 'v' prefix if present
                currentVersion = currentVersion.TrimStart('v');
                latestVersion = latestVersion.TrimStart('v');

                var current = Version.Parse(currentVersion);
                var latest = Version.Parse(latestVersion);

                return latest > current;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to compare versions: {CurrentVersion} vs {LatestVersion}", 
                    currentVersion, latestVersion);
                return false;
            }
        }

        private class GithubReleaseResponse
        {
            [JsonPropertyName("tag_name")]
            public string? TagName { get; set; }

            [JsonPropertyName("html_url")]
            public string? HtmlUrl { get; set; }

            [JsonPropertyName("name")]
            public string? Name { get; set; }

            [JsonPropertyName("published_at")]
            public DateTime PublishedAt { get; set; }

            [JsonPropertyName("prerelease")]
            public bool Prerelease { get; set; }
        }
    }
}

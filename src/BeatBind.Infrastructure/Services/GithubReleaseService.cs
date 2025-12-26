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

        /// <summary>
        /// Initializes a new instance of the GithubReleaseService class.
        /// </summary>
        /// <param name="httpClient">The HTTP client for making API requests.</param>
        /// <param name="logger">The logger instance.</param>
        public GithubReleaseService(HttpClient httpClient, ILogger<GithubReleaseService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "BeatBind");
        }

        /// <summary>
        /// Retrieves information about the latest release from the GitHub repository.
        /// </summary>
        /// <returns>A GithubRelease object if successful; otherwise, null.</returns>
        public async Task<GithubRelease?> GetLatestReleaseAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<GithubReleaseResponse>(GITHUB_API_URL);

                if (response == null)
                {
                    return null;
                }

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

        /// <summary>
        /// Compares two semantic version strings to determine if the latest version is newer.
        /// </summary>
        /// <param name="currentVersion">The current version string.</param>
        /// <param name="latestVersion">The latest version string.</param>
        /// <returns>True if the latest version is newer than the current version; otherwise, false.</returns>
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

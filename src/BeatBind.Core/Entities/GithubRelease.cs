namespace BeatBind.Core.Entities
{
    public class GithubRelease
    {
        public string Version { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTime PublishedAt { get; set; }
        public bool IsPrerelease { get; set; }
    }
}

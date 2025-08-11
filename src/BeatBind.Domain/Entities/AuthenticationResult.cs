namespace BeatBind.Domain.Entities
{
    public class AuthenticationResult
    {
        public bool Success { get; set; }
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
        public string Error { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
}

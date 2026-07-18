using BeatBind.Core.Entities;

namespace BeatBind.Core.Interfaces
{
    public interface IAuthenticationService
    {
        /// <summary>
        /// Raised whenever new authentication tokens are saved, so long-lived
        /// consumers (e.g. the Spotify service) can adopt them immediately.
        /// </summary>
        event EventHandler<AuthenticationResult>? AuthenticationSaved;

        Task<AuthenticationResult> AuthenticateAsync();
        Task<AuthenticationResult> RefreshTokenAsync(string refreshToken);
        bool IsTokenValid(AuthenticationResult authResult);
        AuthenticationResult? GetStoredAuthentication();
        void SaveAuthentication(AuthenticationResult authResult);
    }
}

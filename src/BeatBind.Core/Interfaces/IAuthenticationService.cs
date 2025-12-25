using BeatBind.Core.Entities;

namespace BeatBind.Core.Interfaces
{
    public interface IAuthenticationService
    {
        Task<AuthenticationResult> AuthenticateAsync();
        Task<AuthenticationResult> RefreshTokenAsync(string refreshToken);
        bool IsTokenValid(AuthenticationResult authResult);
        AuthenticationResult? GetStoredAuthentication();
        void SaveAuthentication(AuthenticationResult authResult);
    }
}

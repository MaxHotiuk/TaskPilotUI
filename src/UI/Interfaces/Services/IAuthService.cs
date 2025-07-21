using UI.Models.Auth;
using UI.Models.User;

namespace UI.Interfaces.Services;

public interface IAuthService
{
    Task<bool> IsAuthenticatedAsync();
    Task<UserDto?> GetCurrentUserAsync();
    Task<UserDto?> GetCurrentUserAsync(bool forceRefresh = false);
    UserDto? GetCachedUser();
    Task<UserDto?> RefreshCurrentUserAsync();
    Task InitializeAsync();
    Task<string> GetLoginUrlAsync();
    Task<bool> HandleCallbackAsync(string code, string state);
    Task LogoutAsync();
    Task<string?> GetAccessTokenAsync();
    AuthState AuthState { get; }
    event Action? OnAuthStateChanged;
}

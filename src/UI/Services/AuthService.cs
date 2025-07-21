using Microsoft.JSInterop;
using System.Text.Json;
using UI.Models.Auth;
using UI.Models.User;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Refit;
using UI.Interfaces.Services;
using UI.Interfaces.Api;

namespace UI.Services;

public class AuthService : IAuthService
{
    private readonly ITaskPilotAuthApi _taskPilotAuthApi;
    private readonly IMicrosoftGraphApi _microsoftGraphApi;
    private readonly IAzureAdTokenApi _azureAdTokenApi;
    private readonly IJSRuntime _jsRuntime;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private AuthState _authState = new();
    private System.Timers.Timer? _tokenRefreshTimer;
    private DateTime? _tokenExpiry;
    private DateTime? _tokenIssuedAt;

    public AuthState AuthState => _authState;
    public event Action? OnAuthStateChanged;

    public AuthService(
        ITaskPilotAuthApi taskPilotAuthApi,
        IMicrosoftGraphApi microsoftGraphApi,
        IAzureAdTokenApi azureAdTokenApi,
        IJSRuntime jsRuntime, 
        IConfiguration configuration, 
        ILogger<AuthService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _taskPilotAuthApi = taskPilotAuthApi;
        _microsoftGraphApi = microsoftGraphApi;
        _azureAdTokenApi = azureAdTokenApi;
        _jsRuntime = jsRuntime;
        _configuration = configuration;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        InitializeTokenRefreshTimer();
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        if (_authState.IsAuthenticated && _authState.User != null)
            return true;

        var token = await GetStoredTokenAsync();
        if (string.IsNullOrEmpty(token))
            return false;

        if (_authState.User == null)
        {
            var user = await GetCurrentUserAsync();
            if (user != null)
            {
                _authState.IsAuthenticated = true;
                _authState.User = user;
                _authState.AccessToken = token;
                OnAuthStateChanged?.Invoke();
                return true;
            }
            return false;
        }

        _authState.IsAuthenticated = true;
        _authState.AccessToken = token;
        return true;
    }

    public async Task<UserDto?> GetCurrentUserAsync()
    {
        return await GetCurrentUserAsync(false);
    }

    public async Task<UserDto?> GetCurrentUserAsync(bool forceRefresh = false)
    {
        if (!forceRefresh && _authState.User != null)
            return _authState.User;

        var token = await GetStoredTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogDebug("No token found in storage");
            return null;
        }

        try
        {
            _logger.LogDebug("Making API call to get current user");
            
            var user = await _taskPilotAuthApi.GetCurrentAsync($"Bearer {token}");
                
            if (user != null)
            {
                _logger.LogInformation("Successfully authenticated user: {Email}", user.Email);
                _authState.User = user;
                _authState.IsAuthenticated = true;
                _authState.AccessToken = token;
                OnAuthStateChanged?.Invoke();
                return user;
            }
        }
        catch (ApiException ex)
        {
            _logger.LogWarning("API call failed with status: {StatusCode}", ex.StatusCode);
            _logger.LogDebug("Response: {ResponseContent}", ex.Content);
            
            _logger.LogInformation("Attempting to create user from Microsoft Graph");
            var graphUser = await CreateUserFromTokenAsync(token);
            if (graphUser != null)
            {
                _logger.LogInformation("Successfully created user from Microsoft Graph: {Email}", graphUser.Email);
                _authState.User = graphUser;
                _authState.IsAuthenticated = true;
                _authState.AccessToken = token;
                OnAuthStateChanged?.Invoke();
                return graphUser;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
        }

        return null;
    }

    public UserDto? GetCachedUser()
    {
        return _authState.User;
    }

    public async Task<UserDto?> RefreshCurrentUserAsync()
    {
        return await GetCurrentUserAsync(true);
    }

    public async Task InitializeAsync()
    {
        var token = await GetStoredTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            _authState.AccessToken = token;
            SetTokenTimes(token);
            StartTokenRefreshTimer(token);
        }
    }

    public async Task<string> GetLoginUrlAsync()
    {
        var config = GetAuthConfig();
        var state = Guid.NewGuid().ToString();
        
        var codeVerifier = GenerateCodeVerifier();
        var codeChallenge = GenerateCodeChallenge(codeVerifier);
        
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "auth_state", state);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "code_verifier", codeVerifier);

        var authUrl = $"https://login.microsoftonline.com/{config.TenantId}/oauth2/v2.0/authorize" +
                     $"?client_id={config.ClientId}" +
                     $"&response_type=code" +
                     $"&redirect_uri={Uri.EscapeDataString(config.RedirectUri)}" +
                     $"&response_mode=query" +
                     $"&scope={Uri.EscapeDataString(config.Scope)}" +
                     $"&state={state}" +
                     $"&code_challenge={codeChallenge}" +
                     $"&code_challenge_method=S256";

        return authUrl;
    }

    public async Task<bool> HandleCallbackAsync(string code, string state)
    {
        try
        {
            _logger.LogDebug("Starting HandleCallbackAsync with state: {State}", state);

            var storedState = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "auth_state");
            _logger.LogDebug("Stored state: {StoredState}", storedState);

            if (storedState != state)
            {
                _logger.LogWarning("Invalid state parameter. Expected: {StoredState}, Received: {State}", storedState, state);
                return false;
            }

            var config = GetAuthConfig();
            _logger.LogDebug("Auth config - ClientId: {ClientId}, TenantId: {TenantId}, RedirectUri: {RedirectUri}",
                config.ClientId, config.TenantId, config.RedirectUri);

            var codeVerifier = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "code_verifier");
            if (string.IsNullOrEmpty(codeVerifier))
            {
                _logger.LogWarning("No code verifier found in storage");
                return false;
            }

            var tokenRequest = new Dictionary<string, string>
            {
                {"grant_type", "authorization_code"},
                {"client_id", config.ClientId},
                {"code", code},
                {"redirect_uri", config.RedirectUri},
                {"code_verifier", codeVerifier}
            };

            // Create a new HttpClient for the Azure AD token endpoint
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri($"https://login.microsoftonline.com/{config.TenantId}");

            var tokenApi = RestService.For<IAzureAdTokenApi>(httpClient);
            var tokenResponse = await tokenApi.GetTokenAsync(tokenRequest);

            _logger.LogDebug("Token response received");

            if (tokenResponse.TryGetProperty("access_token", out var accessTokenElement))
            {
                var accessToken = accessTokenElement.GetString();
                _logger.LogDebug("Successfully received access token (length: {TokenLength})", accessToken?.Length ?? 0);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "access_token", accessToken);
                SetTokenTimes(accessToken!);
                if (tokenResponse.TryGetProperty("refresh_token", out var newRefreshTokenElement))
                {
                    var newRefreshToken = newRefreshTokenElement.GetString();
                    if (!string.IsNullOrEmpty(newRefreshToken))
                    {
                        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "refresh_token", newRefreshToken);
                        StartTokenRefreshTimer(accessToken!);
                    }
                }
                
                _logger.LogDebug("Attempting to get current user");
                var user = await GetCurrentUserAsync();
                if (user != null)
                {
                    _logger.LogInformation("Successfully authenticated user: {Email}", user.Email);
                    await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "auth_state");
                    await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "code_verifier");
                    return true;
                }
                else
                {
                    _logger.LogWarning("Failed to get current user from API, using mock user for testing");
                    user = CreateMockUser();
                    _authState.User = user;
                    _authState.IsAuthenticated = true;
                    _authState.AccessToken = accessToken;
                    OnAuthStateChanged?.Invoke();
                    await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "auth_state");
                    await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "code_verifier");
                    return true;
                }
            }
            else
            {
                _logger.LogWarning("No access_token found in token response");
            }
        }
        catch (ApiException ex)
        {
            _logger.LogWarning("Token request failed: {StatusCode} - {Content}", ex.StatusCode, ex.Content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling authentication callback");
        }

        return false;
    }

    public async Task LogoutAsync()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "access_token");
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "auth_state");
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "code_verifier");
        _authState.IsAuthenticated = false;
        _authState.User = null;
        _authState.AccessToken = null;
        StopTokenRefreshTimer();
        OnAuthStateChanged?.Invoke();
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        return await GetStoredTokenAsync();
    }

    private async Task<string?> GetStoredTokenAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "access_token");
        }
        catch
        {
            return null;
        }
    }

    private AuthConfiguration GetAuthConfig()
    {
        var baseUrl = _configuration["App:BaseUrl"] ?? "https://localhost:5001";
        
        return new AuthConfiguration
        {
            ClientId = _configuration["AzureAd:ClientId"] ?? "",
            TenantId = _configuration["AzureAd:TenantId"] ?? "",
            RedirectUri = _configuration["AzureAd:RedirectUri"] ?? $"{baseUrl}/login",
            Scope = _configuration["AzureAd:Scope"] ?? "api://0c097a29-bcfe-48d9-a6a0-8ff50d67384b/TaskPilot_API.all",
            ApiBaseUrl = GetApiBaseUrl()
        };
    }

    private string GetApiBaseUrl()
    {
        return _configuration["Api:BaseUrl"] ?? "https://your-api-domain.com";
    }

    private async Task<UserDto?> CreateUserFromTokenAsync(string accessToken)
    {
        try
        {
            var graphData = await _microsoftGraphApi.GetMeAsync($"Bearer {accessToken}");
            
            _logger.LogDebug("Microsoft Graph response received successfully");

            var user = new UserDto
            {
                Id = Guid.NewGuid().ToString(),
                EntraId = graphData.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "",
                Username = graphData.TryGetProperty("displayName", out var name) ? name.GetString() ?? "" : "",
                Email = graphData.TryGetProperty("mail", out var mail) ? mail.GetString() ?? "" : 
                       (graphData.TryGetProperty("userPrincipalName", out var upn) ? upn.GetString() ?? "" : ""),
                Role = "User",
                CreatedAt = DateTime.UtcNow.ToString("O"),
                UpdatedAt = DateTime.UtcNow.ToString("O")
            };

            _logger.LogInformation("Created user from Microsoft Graph: {Email}", user.Email);
            return user;
        }
        catch (ApiException ex)
        {
            _logger.LogWarning("Microsoft Graph API call failed: {StatusCode} - {Content}", ex.StatusCode, ex.Content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user from Microsoft Graph");
        }

        return null;
    }

    private UserDto CreateMockUser()
    {
        return new UserDto
        {
            Id = Guid.NewGuid().ToString(),
            EntraId = "test-entra-id",
            Username = "Test User",
            Email = "test@example.com",
            Role = "User",
            CreatedAt = DateTime.UtcNow.ToString("O"),
            UpdatedAt = DateTime.UtcNow.ToString("O")
        };
    }

    private string GenerateCodeVerifier()
    {
        var bytes = new byte[32];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private string GenerateCodeChallenge(string codeVerifier)
    {
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            var challengeBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(codeVerifier));
            return Convert.ToBase64String(challengeBytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
    }

    private void InitializeTokenRefreshTimer()
    {
        _tokenRefreshTimer = null;
    }

    private void StartTokenRefreshTimer(string token)
    {
        SetTokenTimes(token);
        if (_tokenIssuedAt == null || _tokenExpiry == null)
            return;
        var ttl = _tokenExpiry.Value - _tokenIssuedAt.Value;
        var halfTtl = ttl.TotalSeconds / 2;
        var refreshTime = _tokenIssuedAt.Value.AddSeconds(halfTtl);
        var interval = (refreshTime - DateTime.UtcNow).TotalMilliseconds;
        if (interval <= 0)
        {
            _ = RefreshTokenAsync();
            return;
        }
        StopTokenRefreshTimer();
        _tokenRefreshTimer = new System.Timers.Timer(interval);
        _tokenRefreshTimer.Elapsed += async (s, e) =>
        {
            _tokenRefreshTimer?.Stop();
            await RefreshTokenAsync();
        };
        _tokenRefreshTimer.AutoReset = false;
        _tokenRefreshTimer.Start();
    }

    private void StopTokenRefreshTimer()
    {
        if (_tokenRefreshTimer != null)
        {
            _tokenRefreshTimer.Stop();
            _tokenRefreshTimer.Dispose();
            _tokenRefreshTimer = null;
        }
    }

    private void SetTokenTimes(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length < 2) return;
            var payload = parts[1];
            var padLength = 4 - (payload.Length % 4);
            if (padLength < 4) payload += new string('=', padLength);
            var bytes = Convert.FromBase64String(payload.Replace('-', '+').Replace('_', '/'));
            var json = System.Text.Encoding.UTF8.GetString(bytes);
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("iat", out var iat))
                _tokenIssuedAt = DateTimeOffset.FromUnixTimeSeconds(iat.GetInt64()).UtcDateTime;
            if (root.TryGetProperty("exp", out var exp))
                _tokenExpiry = DateTimeOffset.FromUnixTimeSeconds(exp.GetInt64()).UtcDateTime;
        }
        catch { _tokenIssuedAt = null; _tokenExpiry = null; }
    }

    private async Task RefreshTokenAsync()
    {
        _logger.LogInformation("Refreshing access token after half TTL expired");
        
        try
        {
            var refreshToken = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "refresh_token");
            if (string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogWarning("No refresh_token found in localStorage. User may need to re-authenticate.");
                
                StopTokenRefreshTimer();
                return;
            }

            var config = GetAuthConfig();
            var tokenRequest = new Dictionary<string, string>
            {
                {"grant_type", "refresh_token"},
                {"client_id", config.ClientId},
                {"refresh_token", refreshToken},
                {"redirect_uri", config.RedirectUri},
                {"scope", config.Scope}
            };

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri($"https://login.microsoftonline.com/{config.TenantId}");
            var tokenApi = RestService.For<IAzureAdTokenApi>(httpClient);
            var tokenResponse = await tokenApi.GetTokenAsync(tokenRequest);

            if (tokenResponse.TryGetProperty("access_token", out var accessTokenElement))
            {
                var accessToken = accessTokenElement.GetString();
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "access_token", accessToken);
                _authState.AccessToken = accessToken;
                SetTokenTimes(accessToken!);
                StartTokenRefreshTimer(accessToken!);
                _logger.LogInformation("Access token refreshed successfully.");
                
                OnAuthStateChanged?.Invoke();
            }
            else
            {
                _logger.LogWarning("Failed to refresh access token: no access_token in response.");
                
                StopTokenRefreshTimer();
            }

            if (tokenResponse.TryGetProperty("refresh_token", out var newRefreshTokenElement))
            {
                var newRefreshToken = newRefreshTokenElement.GetString();
                if (!string.IsNullOrEmpty(newRefreshToken))
                {
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "refresh_token", newRefreshToken);
                    
                }
            }
        }
        catch (ApiException ex)
        {
            _logger.LogWarning("Token refresh failed: {StatusCode} - {Content}", ex.StatusCode, ex.Content);
            
            StopTokenRefreshTimer();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing access token");
            
            StopTokenRefreshTimer();
        }
    }
}

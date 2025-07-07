using Microsoft.JSInterop;
using System.Net.Http.Headers;
using System.Text.Json;
using UI.Models.Auth;
using UI.Models.User;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace UI.Services;

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

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    private AuthState _authState = new();

    public AuthState AuthState => _authState;
    public event Action? OnAuthStateChanged;

    public AuthService(HttpClient httpClient, IJSRuntime jsRuntime, IConfiguration configuration, ILogger<AuthService> logger)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
        _configuration = configuration;
        _logger = logger;
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
            var apiUrl = $"{GetApiBaseUrl()}/api/users/me";
            _logger.LogDebug("Making API call to: {ApiUrl}", apiUrl);
            
            var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            _logger.LogDebug("API response status: {StatusCode}", response.StatusCode);
            _logger.LogTrace("API response content: {ResponseContent}", responseContent);
            
            if (response.IsSuccessStatusCode)
            {
                var user = JsonSerializer.Deserialize<UserDto>(responseContent, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                });
                
                if (user != null)
                {
                    _logger.LogInformation("Successfully authenticated user: {Email}", user.Email);
                    _authState.User = user;
                    _authState.IsAuthenticated = true;
                    _authState.AccessToken = token;
                    OnAuthStateChanged?.Invoke();
                }
                
                return user;
            }
            else
            {
                _logger.LogWarning("API call failed with status: {StatusCode}", response.StatusCode);
                _logger.LogDebug("Response: {ResponseContent}", responseContent);
                
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

            var tokenUrl = $"https://login.microsoftonline.com/{config.TenantId}/oauth2/v2.0/token";
            _logger.LogDebug("Token URL: {TokenUrl}", tokenUrl);

            var tokenResponse = await _httpClient.PostAsync(tokenUrl, new FormUrlEncodedContent(tokenRequest));

            var responseContent = await tokenResponse.Content.ReadAsStringAsync();
            _logger.LogDebug("Token response status: {StatusCode}", tokenResponse.StatusCode);
            _logger.LogTrace("Token response content: {ResponseContent}", responseContent);

            if (tokenResponse.IsSuccessStatusCode)
            {
                var tokenData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                
                if (tokenData.TryGetProperty("access_token", out var accessTokenElement))
                {
                    var accessToken = accessTokenElement.GetString();
                    _logger.LogDebug("Successfully received access token (length: {TokenLength})", accessToken?.Length ?? 0);
                    
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "access_token", accessToken);
                    
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
            else
            {
                _logger.LogWarning("Token request failed: {StatusCode} - {ResponseContent}", tokenResponse.StatusCode, responseContent);
            }
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
            var request = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            _logger.LogDebug("Microsoft Graph response status: {StatusCode}", response.StatusCode);
            _logger.LogTrace("Microsoft Graph response: {ResponseContent}", responseContent);

            if (response.IsSuccessStatusCode)
            {
                var graphData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                
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
}

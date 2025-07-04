using Microsoft.JSInterop;
using System.Net.Http.Headers;
using System.Text.Json;
using UI.Models;
using Microsoft.Extensions.Configuration;

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
    private AuthState _authState = new();

    public AuthState AuthState => _authState;
    public event Action? OnAuthStateChanged;

    public AuthService(HttpClient httpClient, IJSRuntime jsRuntime, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
        _configuration = configuration;
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        // Return cached state if already authenticated
        if (_authState.IsAuthenticated && _authState.User != null)
            return true;

        var token = await GetStoredTokenAsync();
        if (string.IsNullOrEmpty(token))
            return false;

        // Only make API call if we don't have cached user data
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

        // We have token and cached user, mark as authenticated
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
        // Return cached user if available and not forced to refresh
        if (!forceRefresh && _authState.User != null)
            return _authState.User;

        var token = await GetStoredTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("No token found in storage");
            return null;
        }

        try
        {
            var apiUrl = $"{GetApiBaseUrl()}/api/users/me";
            Console.WriteLine($"Making API call to: {apiUrl}");
            
            var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            Console.WriteLine($"API response status: {response.StatusCode}");
            Console.WriteLine($"API response content: {responseContent}");
            
            if (response.IsSuccessStatusCode)
            {
                var user = JsonSerializer.Deserialize<UserDto>(responseContent, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                });
                
                if (user != null)
                {
                    Console.WriteLine($"Successfully parsed user: {user.Email}");
                    _authState.User = user;
                    _authState.IsAuthenticated = true;
                    _authState.AccessToken = token;
                    OnAuthStateChanged?.Invoke();
                }
                
                return user;
            }
            else
            {
                Console.WriteLine($"API call failed with status: {response.StatusCode}");
                Console.WriteLine($"Response: {responseContent}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting current user: {ex}");
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
        // Initialize auth state on app startup by checking if we have a valid token
        // This avoids multiple API calls by setting up the initial state properly
        var token = await GetStoredTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            _authState.AccessToken = token;
            // Note: We don't call GetCurrentUserAsync here to avoid API call on startup
            // The user will be fetched on first IsAuthenticatedAsync call when needed
        }
    }

    public async Task<string> GetLoginUrlAsync()
    {
        var config = GetAuthConfig();
        var state = Guid.NewGuid().ToString();
        
        // Generate PKCE parameters
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
            Console.WriteLine($"Starting HandleCallbackAsync with state: {state}");
            
            var storedState = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "auth_state");
            Console.WriteLine($"Stored state: {storedState}");
            
            if (storedState != state)
            {
                Console.WriteLine("Invalid state parameter");
                return false;
            }

            var config = GetAuthConfig();
            Console.WriteLine($"Auth config - ClientId: {config.ClientId}, TenantId: {config.TenantId}, RedirectUri: {config.RedirectUri}");
            
            // Get the code verifier for PKCE
            var codeVerifier = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "code_verifier");
            if (string.IsNullOrEmpty(codeVerifier))
            {
                Console.WriteLine("No code verifier found");
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

            // Note: For public clients (SPA), client_secret is not required
            // If this is a confidential client, you'll need to add:
            // {"client_secret", config.ClientSecret}

            var tokenUrl = $"https://login.microsoftonline.com/{config.TenantId}/oauth2/v2.0/token";
            Console.WriteLine($"Token URL: {tokenUrl}");

            var tokenResponse = await _httpClient.PostAsync(tokenUrl, new FormUrlEncodedContent(tokenRequest));

            var responseContent = await tokenResponse.Content.ReadAsStringAsync();
            Console.WriteLine($"Token response status: {tokenResponse.StatusCode}");
            Console.WriteLine($"Token response content: {responseContent}");

            if (tokenResponse.IsSuccessStatusCode)
            {
                var tokenData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                
                if (tokenData.TryGetProperty("access_token", out var accessTokenElement))
                {
                    var accessToken = accessTokenElement.GetString();
                    Console.WriteLine($"Got access token: {accessToken?.Substring(0, Math.Min(20, accessToken?.Length ?? 0))}...");
                    
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "access_token", accessToken);
                    
                    // Try to get user info and register if needed
                    Console.WriteLine("Attempting to get current user");
                    var user = await GetCurrentUserAsync();
                    if (user != null)
                    {
                        Console.WriteLine($"Got user: {user.Email}");
                        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "auth_state");
                        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "code_verifier");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine("Failed to get current user from API, using mock user for testing");
                        // Since API is not available, create a mock user for testing
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
                    Console.WriteLine("No access_token in response");
                }
            }
            else
            {
                Console.WriteLine($"Token request failed: {tokenResponse.StatusCode} - {responseContent}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling callback: {ex}");
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
        // Get the current URL base for dynamic redirect URI
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
            // If API is not available, we can still get user info from Microsoft Graph
            var request = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            Console.WriteLine($"Microsoft Graph response status: {response.StatusCode}");
            Console.WriteLine($"Microsoft Graph response: {responseContent}");

            if (response.IsSuccessStatusCode)
            {
                var graphData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                
                var user = new UserDto
                {
                    Id = Guid.NewGuid().ToString(), // Temporary ID
                    EntraId = graphData.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "",
                    Username = graphData.TryGetProperty("displayName", out var name) ? name.GetString() ?? "" : "",
                    Email = graphData.TryGetProperty("mail", out var mail) ? mail.GetString() ?? "" : 
                           (graphData.TryGetProperty("userPrincipalName", out var upn) ? upn.GetString() ?? "" : ""),
                    Role = "User",
                    CreatedAt = DateTime.UtcNow.ToString("O"),
                    UpdatedAt = DateTime.UtcNow.ToString("O")
                };

                Console.WriteLine($"Created user from Graph: {user.Email}");
                return user;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting user from Microsoft Graph: {ex}");
        }

        return null;
    }

    // Temporary method for testing - creates a mock user when API is not available
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

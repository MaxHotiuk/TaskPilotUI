namespace UI.Models;

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string EntraId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
}

public class AuthState
{
    public bool IsAuthenticated { get; set; }
    public UserDto? User { get; set; }
    public string? AccessToken { get; set; }
}

public class AuthConfiguration
{
    public string ClientId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public string ApiBaseUrl { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
}

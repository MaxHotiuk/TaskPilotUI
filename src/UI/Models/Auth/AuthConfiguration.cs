namespace UI.Models.Auth;

public class AuthConfiguration
{
    public string ClientId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public string ApiBaseUrl { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
}

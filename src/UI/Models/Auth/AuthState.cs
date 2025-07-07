namespace UI.Models.Auth;

using UI.Models.User;

public class AuthState
{
    public bool IsAuthenticated { get; set; }
    public UserDto? User { get; set; }
    public string? AccessToken { get; set; }
}

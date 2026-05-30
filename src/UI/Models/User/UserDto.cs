namespace UI.Models.User;

using UI.Models.Organization;

public class UserDto
{
    public Guid Id { get; set; }
    public string EntraId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<OrganizationSummaryDto> Organizations { get; set; } = new();
    public bool IsGoogleCalendarConnected { get; set; }
}

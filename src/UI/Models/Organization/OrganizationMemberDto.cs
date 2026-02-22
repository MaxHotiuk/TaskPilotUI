namespace UI.Models.Organization;

public class OrganizationMemberDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsInvited { get; set; }
    public DateTime JoinedAt { get; set; }
}

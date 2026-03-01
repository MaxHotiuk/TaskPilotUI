namespace UI.Models.Invitation;

public class BoardInvitationDto
{
    public Guid Id { get; set; }
    public Guid BoardId { get; set; }
    public string BoardName { get; set; } = string.Empty;
    public Guid InvitedBy { get; set; }
    public string InviterName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

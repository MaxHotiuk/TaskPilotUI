namespace UI.Models.Invitation;

public class PendingInvitationsDto
{
    public List<BoardInvitationDto> BoardInvitations { get; set; } = new();
    public List<OrganizationInvitationDto> OrganizationInvitations { get; set; } = new();
}

using UI.Models.Invitation;

namespace UI.Interfaces.Services;

public interface IInvitationService
{
    Task<PendingInvitationsDto> GetPendingInvitationsAsync();
    Task AcceptBoardInvitationAsync(Guid invitationId);
    Task RejectBoardInvitationAsync(Guid invitationId);
    Task AcceptOrganizationInvitationAsync(Guid invitationId);
    Task RejectOrganizationInvitationAsync(Guid invitationId);
}

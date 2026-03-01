using Refit;
using UI.Models.Invitation;

namespace UI.Interfaces.Api;

public interface IInvitationApi
{
    [Get("/api/invitations/pending")]
    Task<PendingInvitationsDto> GetPendingInvitationsAsync();

    [Post("/api/invitations/boards/{invitationId}/accept")]
    Task AcceptBoardInvitationAsync(Guid invitationId);

    [Post("/api/invitations/boards/{invitationId}/reject")]
    Task RejectBoardInvitationAsync(Guid invitationId);

    [Post("/api/invitations/organizations/{invitationId}/accept")]
    Task AcceptOrganizationInvitationAsync(Guid invitationId);

    [Post("/api/invitations/organizations/{invitationId}/reject")]
    Task RejectOrganizationInvitationAsync(Guid invitationId);
}

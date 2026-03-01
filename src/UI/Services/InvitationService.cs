using UI.Interfaces.Api;
using UI.Interfaces.Services;
using UI.Models.Invitation;

namespace UI.Services;

public class InvitationService : IInvitationService
{
    private readonly IInvitationApi _invitationApi;

    public InvitationService(IInvitationApi invitationApi)
    {
        _invitationApi = invitationApi;
    }

    public async Task<PendingInvitationsDto> GetPendingInvitationsAsync()
    {
        return await _invitationApi.GetPendingInvitationsAsync();
    }

    public async Task AcceptBoardInvitationAsync(Guid invitationId)
    {
        await _invitationApi.AcceptBoardInvitationAsync(invitationId);
    }

    public async Task RejectBoardInvitationAsync(Guid invitationId)
    {
        await _invitationApi.RejectBoardInvitationAsync(invitationId);
    }

    public async Task AcceptOrganizationInvitationAsync(Guid invitationId)
    {
        await _invitationApi.AcceptOrganizationInvitationAsync(invitationId);
    }

    public async Task RejectOrganizationInvitationAsync(Guid invitationId)
    {
        await _invitationApi.RejectOrganizationInvitationAsync(invitationId);
    }
}

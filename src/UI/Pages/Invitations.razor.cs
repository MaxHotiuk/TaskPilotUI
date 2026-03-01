using Microsoft.AspNetCore.Components;
using UI.Interfaces.Services;
using UI.Models.Invitation;

namespace UI.Pages;

public partial class Invitations : ComponentBase
{
    [Inject] private IInvitationService InvitationService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private AntDesign.INotificationService NotificationService { get; set; } = default!;

    private PendingInvitationsDto? _invitations;
    private bool _isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadInvitations();
    }

    private async Task LoadInvitations()
    {
        try
        {
            _isLoading = true;
            _invitations = await InvitationService.GetPendingInvitationsAsync();
        }
        catch (Exception ex)
        {
            await NotificationService.Error(new AntDesign.NotificationConfig
            {
                Message = "Failed to load invitations",
                Description = ex.Message
            });
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    private async Task AcceptBoardInvitation(Guid invitationId)
    {
        try
        {
            await InvitationService.AcceptBoardInvitationAsync(invitationId);
            await NotificationService.Success(new AntDesign.NotificationConfig
            {
                Message = "Invitation accepted",
                Description = "You've successfully joined the board!"
            });
            await LoadInvitations();
        }
        catch (Exception ex)
        {
            await NotificationService.Error(new AntDesign.NotificationConfig
            {
                Message = "Failed to accept invitation",
                Description = ex.Message
            });
        }
    }

    private async Task RejectBoardInvitation(Guid invitationId)
    {
        try
        {
            await InvitationService.RejectBoardInvitationAsync(invitationId);
            await NotificationService.Success(new AntDesign.NotificationConfig
            {
                Message = "Invitation rejected",
                Description = "Board invitation has been declined."
            });
            await LoadInvitations();
        }
        catch (Exception ex)
        {
            await NotificationService.Error(new AntDesign.NotificationConfig
            {
                Message = "Failed to reject invitation",
                Description = ex.Message
            });
        }
    }

    private async Task AcceptOrganizationInvitation(Guid invitationId)
    {
        try
        {
            await InvitationService.AcceptOrganizationInvitationAsync(invitationId);
            await NotificationService.Success(new AntDesign.NotificationConfig
            {
                Message = "Invitation accepted",
                Description = "You've successfully joined the organization!"
            });
            await LoadInvitations();
        }
        catch (Exception ex)
        {
            await NotificationService.Error(new AntDesign.NotificationConfig
            {
                Message = "Failed to accept invitation",
                Description = ex.Message
            });
        }
    }

    private async Task RejectOrganizationInvitation(Guid invitationId)
    {
        try
        {
            await InvitationService.RejectOrganizationInvitationAsync(invitationId);
            await NotificationService.Success(new AntDesign.NotificationConfig
            {
                Message = "Invitation rejected",
                Description = "Organization invitation has been declined."
            });
            await LoadInvitations();
        }
        catch (Exception ex)
        {
            await NotificationService.Error(new AntDesign.NotificationConfig
            {
                Message = "Failed to reject invitation",
                Description = ex.Message
            });
        }
    }

    private int GetTotalInvitationsCount()
    {
        if (_invitations == null) return 0;
        return _invitations.BoardInvitations.Count + _invitations.OrganizationInvitations.Count;
    }
}

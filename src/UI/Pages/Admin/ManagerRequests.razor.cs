using Microsoft.AspNetCore.Components;
using UI.Models.Organization;
using UI.Interfaces.Services;
using UI.Components.Base;

namespace UI.Pages.Admin;

public partial class ManagerRequests : BaseComponentWithLoading
{
    [Inject] private IOrganizationService OrganizationService { get; set; } = default!;
    [Inject] private IAuthService AuthService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    private List<ManagerRequestDto> _requests = new();
    private string? _errorMessage;
    private string? _successMessage;
    private bool _showRejectModal = false;
    private ManagerRequestDto? _selectedRequest;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        var currentUser = AuthService.GetCachedUser();
        if (currentUser?.Role != "Admin")
        {
            NavigationManager.NavigateTo("/");
            return;
        }

        await LoadRequestsAsync();
    }

    private async Task LoadRequestsAsync()
    {
        await ExecuteWithLoadingAsync(async () =>
        {
            try
            {
                var requests = await OrganizationService.GetPendingManagerRequestsAsync();
                _requests = requests.ToList();
            }
            catch (Exception ex)
            {
                _errorMessage = ex.Message;
            }
        });
    }

    protected async Task ExecuteWithLoadingAsync(Func<Task> action)
    {
        if (LoadingService != null)
        {
            await LoadingService.ExecuteWithLoadingAsync(action);
        }
        else
        {
            await action();
        }
    }

    private async Task ApproveRequest(Guid requestId)
    {
        await ExecuteWithLoadingAsync(async () =>
        {
            try
            {
                await OrganizationService.ApproveManagerRequestAsync(requestId);
                _successMessage = UI.Resources.I18n.RequestApprovedSuccessfully;
                await LoadRequestsAsync();
            }
            catch (Exception ex)
            {
                _errorMessage = ex.Message;
            }
        });
    }

    private void ShowRejectModal(ManagerRequestDto request)
    {
        _selectedRequest = request;
        _showRejectModal = true;
    }

    private async Task HandleRequestRejected()
    {
        _successMessage = UI.Resources.I18n.RequestRejectedSuccessfully;
        await LoadRequestsAsync();
    }
}

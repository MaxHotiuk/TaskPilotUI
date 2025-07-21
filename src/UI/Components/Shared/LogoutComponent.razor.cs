using Microsoft.AspNetCore.Components;
using UI.Interfaces.Services;
using AntDesign;

namespace UI.Components.Shared;

public partial class LogoutComponent : ComponentBase
{
    [Inject] private IAuthService AuthService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IMessageService Message { get; set; } = default!;
    [Inject] private IConfirmService Confirm { get; set; } = default!;

    [Parameter] public bool ShowAsDropdownItem { get; set; } = false;
    [Parameter] public bool ShowAsButton { get; set; } = false;
    [Parameter] public bool ShowAsLink { get; set; } = false;
    [Parameter] public bool IconOnly { get; set; } = false;
    [Parameter] public bool DangerButton { get; set; } = true;
    [Parameter] public ButtonSize Size { get; set; } = ButtonSize.Default;
    [Parameter] public bool ShowConfirmation { get; set; } = true;
    [Parameter] public string RedirectUrl { get; set; } = "/login";
    [Parameter] public string? Class { get; set; }
    [Parameter] public EventCallback OnBeforeLogout { get; set; }
    [Parameter] public EventCallback OnAfterLogout { get; set; }

    private bool _isLoading = false;

    private async Task OnLogoutClick()
    {
        if (ShowConfirmation)
        {
            var confirmed = await Confirm.Show(
                "Are you sure you want to sign out?",
                "Sign Out",
                ConfirmButtons.YesNo,
                ConfirmIcon.Question);

            if (confirmed != ConfirmResult.Yes)
                return;
        }

        await PerformLogout();
    }

    private async Task PerformLogout()
    {
        _isLoading = true;
        StateHasChanged();

        try
        {
            if (OnBeforeLogout.HasDelegate)
            {
                await OnBeforeLogout.InvokeAsync();
            }

            await AuthService.LogoutAsync();

            Message.Success("You have been signed out successfully");

            if (OnAfterLogout.HasDelegate)
            {
                await OnAfterLogout.InvokeAsync();
            }

            await Task.Delay(500);

            Navigation.NavigateTo(RedirectUrl, forceLoad: true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during logout: {ex.Message}");
            Message.Error($"Failed to sign out: {ex.Message}");
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    public async Task TriggerLogout()
    {
        await PerformLogout();
    }
}
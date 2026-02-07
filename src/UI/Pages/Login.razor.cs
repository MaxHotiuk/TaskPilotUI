using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using UI.Models.User;
using UI.Interfaces.Services;

namespace UI.Pages
{
    public partial class Login : ComponentBase
    {
        [Inject] private IAuthService AuthService { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

        private bool _isLoading = false;
        private string? _error = null;

        protected override async Task OnInitializedAsync()
        {
            if (AuthService.AuthState.IsAuthenticated && AuthService.GetCachedUser() != null)
            {
                Navigation.NavigateTo("/");
                return;
            }
            
            if (await AuthService.IsAuthenticatedAsync())
            {
                Navigation.NavigateTo("/");
                return;
            }

            var uri = new Uri(Navigation.Uri);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            var code = query["code"];
            var state = query["state"];
            var error = query["error"];

            if (!string.IsNullOrEmpty(error))
            {
                _error = string.Format(UI.Resources.I18n.AuthenticationFailedWithError, error);
                return;
            }

            if (!string.IsNullOrEmpty(code) && !string.IsNullOrEmpty(state))
            {
                await HandleCallback(code, state);
            }
        }

        private async Task HandleLogin()
        {
            try
            {
                _isLoading = true;
                _error = null;
                StateHasChanged();

                var loginUrl = await AuthService.GetLoginUrlAsync();
                await JSRuntime.InvokeVoidAsync("authHelpers.navigateToUrl", loginUrl);
            }
            catch (Exception ex)
            {
                _error = string.Format(UI.Resources.I18n.FailedToInitiateLogin, ex.Message);
            }
            finally
            {
                _isLoading = false;
                StateHasChanged();
            }
        }

        private async Task HandleCallback(string code, string state)
        {
            try
            {
                _isLoading = true;
                StateHasChanged();

                Console.WriteLine($"Handling callback with code: {code?.Substring(0, Math.Min(20, code?.Length ?? 0))}... and state: {state}");

                var success = await AuthService.HandleCallbackAsync(code!, state);
                if (success)
                {
                    Console.WriteLine("Authentication successful, navigating to home");
                    Navigation.NavigateTo("/", true);
                }
                else
                {
                    _error = "Authentication failed. Please check console for details.";
                    Console.WriteLine("Authentication failed in HandleCallbackAsync");
                }
            }
            catch (Exception ex)
            {
                _error = string.Format(UI.Resources.I18n.AuthenticationException, ex.Message);
                Console.WriteLine($"Exception in HandleCallback: {ex}");
            }
            finally
            {
                _isLoading = false;
                StateHasChanged();
            }
        }
    }
}

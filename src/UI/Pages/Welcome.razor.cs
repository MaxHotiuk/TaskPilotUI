using Microsoft.AspNetCore.Components;
using UI.Models.User;
using UI.Interfaces.Services;

namespace UI.Pages
{
    public partial class Welcome : ComponentBase
    {
        [Inject] private IAuthService AuthService { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;

        private bool _isLoading = true;
        private UserDto? _currentUser = null;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                if (await AuthService.IsAuthenticatedAsync())
                {
                    _currentUser = AuthService.GetCachedUser();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading user: {ex.Message}");
            }
            finally
            {
                _isLoading = false;
                StateHasChanged();
            }
        }

        private void NavigateToLogin()
        {
            Navigation.NavigateTo("/login");
        }

        private async Task HandleLogout()
        {
            await AuthService.LogoutAsync();
            Navigation.NavigateTo("/login", true);
        }

        private string GetFormattedDate(string dateString)
        {
            if (DateTime.TryParse(dateString, out var date))
            {
                return date.ToString("MMMM dd, yyyy", System.Globalization.CultureInfo.InvariantCulture);
            }
            return dateString;
        }
    }
}

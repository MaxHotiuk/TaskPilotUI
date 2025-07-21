using Microsoft.AspNetCore.Components;
using UI.Models.User;
using UI.Interfaces.Services;
using UI.Extensions;

namespace UI.Pages
{
    public partial class Welcome : ComponentBase
    {
        [CascadingParameter] public IGlobalLoadingService LoadingService { get; set; } = default!;
        [Inject] private IAuthService AuthService { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;

        private UserDto? _currentUser = null;

        protected bool IsLoading => LoadingService?.IsLoading ?? false;

        protected override async Task OnInitializedAsync()
        {
            await AuthService.ExecuteWithGlobalLoadingAndErrorHandlingAsync(
                LoadingService,
                async service =>
                {
                    if (await service.IsAuthenticatedAsync())
                    {
                        _currentUser = service.GetCachedUser();
                    }
                },
                onError: ex =>
                {
                    Console.WriteLine($"Error loading user: {ex.Message}");
                    return Task.CompletedTask;
                },
                onFinally: () =>
                {
                    StateHasChanged();
                    return Task.CompletedTask;
                });
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

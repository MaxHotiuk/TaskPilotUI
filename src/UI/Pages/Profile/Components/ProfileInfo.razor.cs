using Microsoft.AspNetCore.Components;
using UI.Models.User;
using UI.Interfaces.Services;
using UI.Extensions;

namespace UI.Pages.Profile.Components;

public partial class ProfileInfo : ComponentBase
{
    [Inject] private IAuthService AuthService { get; set; } = default!;
    [Inject] private IGlobalLoadingService LoadingService { get; set; } = default!;
    private UserDto? _currentUser;

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
}

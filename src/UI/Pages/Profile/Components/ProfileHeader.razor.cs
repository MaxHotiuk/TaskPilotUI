using Microsoft.AspNetCore.Components;
using UI.Models.User;
using UI.Interfaces.Services;
using UI.Models.Avatar;
using AntDesign;
using UI.Extensions;

namespace UI.Pages.Profile.Components;

public partial class ProfileHeader : ComponentBase
{
    [Inject] private IAuthService AuthService { get; set; } = default!;
    [Inject] private IAvatarService AvatarService { get; set; } = default!;
    [Inject] private IGlobalLoadingService LoadingService { get; set; } = default!;

    private UserDto? _currentUser;
    private string? _avatarUrl;

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
        if (_currentUser != null)
        {
            var avatar = await AvatarService.GetAvatarOrNullAsync(Guid.Parse(_currentUser.Id));
            _avatarUrl = avatar?.CompressedUrl ?? "/_content/AntDesign/images/user.png";
        }
    }
}

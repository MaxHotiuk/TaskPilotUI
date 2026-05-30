using Microsoft.AspNetCore.Components;
using UI.Models.User;
using UI.Models.Avatar;
using UI.Interfaces.Services;
using UI.Extensions;
using AntDesign;

namespace UI.Pages.Profile.Components;

public partial class ProfileCard : ComponentBase
{
    [Inject] private IAuthService AuthService { get; set; } = default!;
    [Inject] private IGlobalLoadingService LoadingService { get; set; } = default!;
    [Inject] private IAvatarService AvatarService { get; set; } = default!;
    [Inject] private IMessageService Message { get; set; } = default!;
    [Inject] private IGoogleCalendarService GoogleCalendarService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private UserDto? _currentUser;
    private string? _avatarUrl;

    protected override async Task OnInitializedAsync()
    {
        await LoadUserAndAvatar();
    }

    private async Task LoadUserAndAvatar()
    {
        await AuthService.ExecuteWithGlobalLoadingAndErrorHandlingAsync(
            LoadingService,
            async service =>
            {
                if (await service.IsAuthenticatedAsync())
                {
                    _currentUser = service.GetCachedUser();
                    await LoadAvatar();
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

    private async Task LoadAvatar()
    {
        if (_currentUser != null)
        {
            var avatar = await AvatarService.GetAvatarOrNullAsync(_currentUser.Id);
            _avatarUrl = avatar?.CompressedUrl;
        }
    }

    public async Task OnFileSelected(Microsoft.AspNetCore.Components.Forms.InputFileChangeEventArgs e)
    {
        if (_currentUser == null) return;

        var files = e.GetMultipleFiles();
        if (files == null || files.Count == 0)
        {
            return;
        }

        await UploadAvatar(files[0]);
    }

    private async Task UploadAvatar(Microsoft.AspNetCore.Components.Forms.IBrowserFile browserFile)
    {
        try
        {
            using var stream = browserFile.OpenReadStream();
            var avatar = await AvatarService.UploadAsync(_currentUser!.Id, stream, browserFile.Name);
            _avatarUrl = avatar.CompressedUrl;
            Message.Success("Avatar updated successfully");
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Message.Error($"Failed to upload avatar: {ex.Message}");
        }
    }

    private async Task OnDeleteAvatar()
    {
        if (_currentUser == null) return;
        
        try
        {
            await AvatarService.DeleteAsync(_currentUser.Id);
            _avatarUrl = null;
            Message.Success("Avatar removed successfully");
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Message.Error($"Failed to remove avatar: {ex.Message}");
        }
    }

    private string GetFormattedDate(DateTime? date)
    {
        if (date == null)
            return "N/A";

        return date.Value.ToString("MMM dd, yyyy");
    }

    private async Task ConnectGoogleCalendar()
    {
        if (_currentUser == null) return;

        try
        {
            var url = await GoogleCalendarService.GetAuthorizationUrlAsync(_currentUser.Id);
            Navigation.NavigateTo(url, forceLoad: true);
        }
        catch (Exception ex)
        {
            Message.Error($"Failed to connect Google Calendar: {ex.Message}");
        }
    }
}
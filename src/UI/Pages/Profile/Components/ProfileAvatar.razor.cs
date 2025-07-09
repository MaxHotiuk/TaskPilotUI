using Microsoft.AspNetCore.Components;
using UI.Models.User;
using UI.Models.Avatar;
using UI.Interfaces.Services;
using UI.Extensions;

namespace UI.Pages.Profile.Components;

public partial class ProfileAvatar : ComponentBase
{
    [Inject] private IAuthService AuthService { get; set; } = default!;
    [Inject] private IGlobalLoadingService LoadingService { get; set; } = default!;
    [Inject] private IAvatarService AvatarService { get; set; } = default!;
    [Inject] private IMessageService Message { get; set; } = default!;

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
            var avatar = await AvatarService.GetAvatarOrNullAsync(Guid.Parse(_currentUser.Id));
            _avatarUrl = avatar?.CompressedUrl;
        }
    }

    public async Task OnFileSelected(Microsoft.AspNetCore.Components.Forms.InputFileChangeEventArgs e)
    {
        Console.WriteLine("File selected for avatar upload");
        if (_currentUser == null) return;
        Console.WriteLine($"Current user ID: {_currentUser.Id}");
        var files = e.GetMultipleFiles();
        if (files == null || files.Count == 0)
        {
            Console.WriteLine("No files selected or invalid input");
            return;
        }
        await UploadAvatar(files[0]);
    }

    private async Task UploadAvatar(Microsoft.AspNetCore.Components.Forms.IBrowserFile browserFile)
    {
        try
        {
            using var stream = browserFile.OpenReadStream();
            Console.WriteLine($"Uploading avatar: {browserFile.Name}, Size: {browserFile.Size} bytes");
            var avatar = await AvatarService.UploadAsync(Guid.Parse(_currentUser!.Id), stream, browserFile.Name);
            _avatarUrl = avatar.CompressedUrl;
            Message.Success("Avatar uploaded successfully");
            await LoadAvatar();
        }
        catch (Exception ex)
        {
            Message.Error($"Failed to upload avatar: {ex.Message}");
        }
        StateHasChanged();
    }

    private async Task OnDeleteAvatar()
    {
        if (_currentUser == null) return;
        try
        {
            await AvatarService.DeleteAsync(Guid.Parse(_currentUser.Id));
            _avatarUrl = null;
            Message.Success("Avatar deleted");
            await LoadAvatar();
        }
        catch (Exception ex)
        {
            Message.Error($"Failed to delete avatar: {ex.Message}");
        }
    }
}

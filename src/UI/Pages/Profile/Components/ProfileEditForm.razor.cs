using Microsoft.AspNetCore.Components;
using UI.Models.User;
using UI.Interfaces.Services;
using UI.Extensions;
using AntDesign;

namespace UI.Pages.Profile.Components;

public partial class ProfileEditForm : ComponentBase
{
    [Inject] private IAuthService AuthService { get; set; } = default!;
    [Inject] private IGlobalLoadingService LoadingService { get; set; } = default!;
    [Inject] private IMessageService Message { get; set; } = default!;

    private UserDto? _currentUser;
    private UpdateUserDto _formModel = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadUser();
    }

    private async Task LoadUser()
    {
        await AuthService.ExecuteWithGlobalLoadingAndErrorHandlingAsync(
            LoadingService,
            async service =>
            {
                if (await service.IsAuthenticatedAsync())
                {
                    _currentUser = service.GetCachedUser();
                    if (_currentUser != null)
                    {
                        _formModel = new UpdateUserDto
                        {
                            Username = _currentUser.Username,
                            Email = _currentUser.Email,
                            Role = _currentUser.Role
                        };
                    }
                }
            },
            onError: ex =>
            {
                Console.WriteLine($"Error loading user: {ex.Message}");
                Message.Error("Failed to load user information");
                return Task.CompletedTask;
            },
            onFinally: () =>
            {
                StateHasChanged();
                return Task.CompletedTask;
            });
    }

    private void OnReset()
    {
        if (_currentUser != null)
        {
            _formModel = new UpdateUserDto
            {
                Username = _currentUser.Username,
                Email = _currentUser.Email,
                Role = _currentUser.Role
            };
            StateHasChanged();
        }
    }

    private string GetFormattedDate(string? dateString)
    {
        if (string.IsNullOrEmpty(dateString) || !DateTime.TryParse(dateString, out var date))
            return "N/A";
        
        return date.ToString("MMM dd, yyyy");
    }
}
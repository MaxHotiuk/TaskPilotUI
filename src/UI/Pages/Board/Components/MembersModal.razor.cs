using Microsoft.AspNetCore.Components;
using UI.Models.Board;
using UI.Models.Member;
using UI.Interfaces.Services;
using UI.Models.User;

namespace UI.Pages.Board.Components;

public partial class MembersModal : ComponentBase
{
    [Inject] private IUserService UserService { get; set; } = default!;
    
    [Parameter] public bool IsVisible { get; set; }
    [Parameter] public BoardDetailDto? BoardDetail { get; set; }
    [Parameter] public bool CanManageMembers { get; set; }
    [Parameter] public string? CurrentUserId { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }
    [Parameter] public EventCallback OnAddMember { get; set; }
    [Parameter] public EventCallback<(BoardMemberDto member, string role)> OnChangeRole { get; set; }
    [Parameter] public EventCallback<BoardMemberDto> OnRemoveMember { get; set; }

    private Dictionary<string, UserDto> _userCache = new();
    private bool _usersLoaded = false;

    protected override async Task OnParametersSetAsync()
    {
        if (IsVisible && BoardDetail?.Members.Any() == true && !_usersLoaded)
        {
            await LoadUsersAsync();
        }
        
        if (!IsVisible)
        {
            _usersLoaded = false;
        }
    }

    private async Task LoadUsersAsync()
    {
        try
        {
            var userIds = BoardDetail?.Members.Select(m => m.UserId).Where(id => !string.IsNullOrEmpty(id)) ?? Enumerable.Empty<string>();
            if (userIds.Any())
            {
                _userCache = await UserService.GetByIdsAsync(userIds);
                _usersLoaded = true;
                StateHasChanged();
            }
        }
        catch (Exception)
        {
            _usersLoaded = false;
        }
    }

    private string GetMemberName(string userId)
    {
        if (_userCache.TryGetValue(userId, out var user))
        {
            return !string.IsNullOrEmpty(user.Username) ? user.Username 
                 : !string.IsNullOrEmpty(user.Email) ? user.Email 
                 : $"User {userId[..Math.Min(8, userId.Length)]}";
        }
        
        return $"User {userId[..Math.Min(8, userId.Length)]}";
    }

    private string FormatDate(string dateString)
    {
        if (DateTime.TryParse(dateString, out var date))
        {
            return date.ToString("MMM dd, yyyy");
        }
        return dateString;
    }

    public async Task RefreshUsersAsync()
    {
        _usersLoaded = false;
        _userCache.Clear();
        await LoadUsersAsync();
    }

    private string GetMemberEmail(string userId)
    {
        if (_userCache.TryGetValue(userId, out var user))
        {
            return user.Email ?? string.Empty;
        }
        
        return string.Empty;
    }
}

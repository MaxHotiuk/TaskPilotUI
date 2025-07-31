using Microsoft.AspNetCore.Components;
using UI.Models.Board;
using UI.Models.Member;
using UI.Interfaces.Services;
using UI.Models.User;
using UI.Models.Avatar;
using System.Collections.Concurrent;
using System.Threading.Tasks;

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
    [Inject] private IAvatarService AvatarService { get; set; } = default!;
    private ConcurrentDictionary<string, AvatarDto?> _avatarCache = new();
    private ConcurrentDictionary<string, bool> _avatarLoading = new();

    private Dictionary<string, UserDto> _userCache = new();
    private bool _usersLoaded = false;

    protected override async Task OnParametersSetAsync()
    {
        if (IsVisible && BoardDetail?.Members.Any() == true && !_usersLoaded)
        {
            await LoadUsersAsync();
        }
        if (IsVisible && BoardDetail?.Members.Any() == true)
        {
            foreach (var member in BoardDetail.Members)
            {
                _ = LoadAvatarAsync(member.UserId);
            }
        }
        if (!IsVisible)
        {
            _usersLoaded = false;
        }
    }

    private async Task LoadAvatarAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId) || _avatarCache.ContainsKey(userId) || _avatarLoading.ContainsKey(userId))
            return;
        _avatarLoading[userId] = true;
        try
        {
            if (Guid.TryParse(userId, out var guid))
            {
                var avatar = await AvatarService.GetAvatarOrNullAsync(guid);
                _avatarCache[userId] = avatar;
            }
            else
            {
                _avatarCache[userId] = null;
            }
        }
        catch
        {
            _avatarCache[userId] = null;
        }
        finally
        {
            _avatarLoading.TryRemove(userId, out _);
            StateHasChanged();
        }
    }

    private string? GetAvatarUrl(string userId)
    {
        if (_avatarCache.TryGetValue(userId, out var avatar) && avatar != null && !string.IsNullOrEmpty(avatar.CompressedUrl))
            return avatar.CompressedUrl;
        return null;
    }

    private bool IsAvatarLoading(string userId) => _avatarLoading.ContainsKey(userId);

    private string GetMemberInitials(string userId)
    {
        if (_userCache.TryGetValue(userId, out var user))
        {
            var parts = user.Username?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
            if (parts.Length >= 2)
                return $"{parts[0][0]}{parts[1][0]}".ToUpper();
            if (!string.IsNullOrEmpty(user.Username))
                return user.Username[0].ToString().ToUpper();
            if (!string.IsNullOrEmpty(user.Email))
                return user.Email[0].ToString().ToUpper();
        }
        return "U";
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
        if (_userCache.TryGetValue(userId, out var user) && user.Username != null)
        {
            return user.Username;
        }
        else
        {
            var fetchedUser = UserService.GetByIdAsync(userId);
            if (fetchedUser != null)
            {
                return fetchedUser.Result!.Username;
            }
        }
        return "Failed to load username";
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

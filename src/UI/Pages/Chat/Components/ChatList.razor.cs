using Microsoft.AspNetCore.Components;
using UI.Interfaces.Services;
using UI.Models.Avatar;
using UI.Models.Chat;

namespace UI.Pages.Chat.Components;

public partial class ChatList : ComponentBase
{
    [Parameter] public IReadOnlyList<ChatDto> Chats { get; set; } = Array.Empty<ChatDto>();
    [Parameter] public Guid? SelectedChatId { get; set; }
    [Parameter] public Guid CurrentUserId { get; set; }
    [Parameter] public EventCallback<ChatDto> OnSelectChat { get; set; }

    [Inject] private IAvatarService AvatarService { get; set; } = default!;

    private readonly Dictionary<Guid, AvatarDto?> _avatarCache = new();
    private readonly HashSet<Guid> _avatarLoading = new();

    protected override void OnParametersSet()
    {
        foreach (var chat in Chats)
        {
            var otherMemberId = GetOtherMemberId(chat);
            if (otherMemberId.HasValue)
            {
                _ = LoadAvatarAsync(otherMemberId.Value);
            }
        }
    }

    private string GetChatTitle(ChatDto chat)
    {
        if (chat.Type == ChatType.Group)
            return string.IsNullOrWhiteSpace(chat.Name) ? "Group chat" : chat.Name;

        var otherMember = chat.Members.FirstOrDefault(member => member.UserId != CurrentUserId);
        if (otherMember != null && !string.IsNullOrWhiteSpace(otherMember.UserName))
            return otherMember.UserName;

        return "Private chat";
    }

    private string GetChatInitials(ChatDto chat)
    {
        if (chat.Type == ChatType.Group)
            return "G";

        var member = chat.Members.FirstOrDefault(m => m.UserId != CurrentUserId);
        if (member == null || string.IsNullOrWhiteSpace(member.UserName))
            return "P";

        return member.UserName.Trim()[0].ToString().ToUpperInvariant();
    }

    private string? GetChatIcon(ChatDto chat)
    {
        return chat.Type == ChatType.Group ? "team" : null;
    }

    private bool TryGetChatAvatar(ChatDto chat, out string? avatarUrl)
    {
        avatarUrl = null;
        var otherMemberId = GetOtherMemberId(chat);
        if (!otherMemberId.HasValue)
            return false;

        if (_avatarCache.TryGetValue(otherMemberId.Value, out var avatar) && avatar != null && !string.IsNullOrEmpty(avatar.CompressedUrl))
        {
            avatarUrl = avatar.CompressedUrl;
            return true;
        }

        return false;
    }

    private Guid? GetOtherMemberId(ChatDto chat)
    {
        if (chat.Type != ChatType.Private)
            return null;

        var member = chat.Members.FirstOrDefault(m => m.UserId != CurrentUserId);
        return member?.UserId;
    }

    private bool IsChatUnread(ChatDto chat)
    {
        if (chat.LastMessage == null)
            return false;

        if (chat.LastMessage.SenderId == CurrentUserId)
            return false;

        var member = chat.Members.FirstOrDefault(m => m.UserId == CurrentUserId);
        if (member?.LastReadAt == null)
            return true;

        return chat.LastMessage.CreatedAt > member.LastReadAt.Value;
    }

    private string GetLastMessagePreview(ChatMessagePreviewDto message)
    {
        if (string.Equals(message.MessageType, "Call", StringComparison.OrdinalIgnoreCase))
            return "started a call";

        return message.Content;
    }

    private async Task LoadAvatarAsync(Guid userId)
    {
        if (_avatarCache.ContainsKey(userId) || _avatarLoading.Contains(userId))
            return;

        _avatarLoading.Add(userId);
        try
        {
            var avatar = await AvatarService.GetAvatarOrNullAsync(userId);
            _avatarCache[userId] = avatar;
        }
        catch
        {
            _avatarCache[userId] = null;
        }
        finally
        {
            _avatarLoading.Remove(userId);
            await InvokeAsync(StateHasChanged);
        }
    }
}
